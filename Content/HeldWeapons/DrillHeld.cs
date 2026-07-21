using InnoVault;
using InnoVault.GameContent.BaseEntity;
using InnoVault.GameSystem;
using InnoVault.Trails;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.HeldWeapons
{
    //钻头枪的手持体，本章的主角。BaseHeldProj 把鼠标向量和左右键状态封进了
    //密封的网络管线，子类只需要读 ToMouse/DownLeft 这些属性——它们在远程客户端
    //上同样有效。这里用它实现弹匣连射、右键换弹，换弹完成瞬间放一道 ThunderTrail
    //短闪电；额外状态（弹药数、换弹计时）走 NetHeldSend 与 SendBitsByte 同步。
    //
    //The held body of the drill gun and the star of this chapter. BaseHeldProj
    //seals mouse vectors and button states into its network pipeline; a
    //subclass just reads ToMouse/DownLeft and friends, which are equally valid
    //on remote clients. Built on that: magazine autofire, right-click reload,
    //and a ThunderTrail lightning flash the instant a reload completes. Extra
    //state (ammo, reload timer) syncs through NetHeldSend and SendBitsByte.
    //
    //https://innovault.wiki/cn/content/base-held-proj/
    internal class DrillHeld : BaseHeldProj
    {
        internal const int MagazineSize = 8;
        internal const int ReloadTicks = 45;
        private const float MuzzleLength = 24f;
        private const int ThunderLife = 14;
        private const string ThunderTexPath = "InnoVaultExample/Assets/Trails/LightningStrip";

        //枪身沿用米尼鲨贴图，和物品保持一致
        //
        //The gun body reuses the Minishark sprite, matching the item.
        public override string Texture => "Terraria/Images/Item_98";

        //弹药和换弹计时是普通字段而不是 Projectile.ai：ai 数组另有原版同步语义，
        //这里特意让它们走 BaseHeldProj 的 NetHeld 通道来演示自定义载荷
        //
        //Ammo and the reload timer are plain fields, not Projectile.ai — the ai
        //array has its own vanilla sync semantics, and these two deliberately
        //ride BaseHeldProj's NetHeld channel to show custom payloads.
        private int ammo = MagazineSize;
        private int reloadTimer;
        private bool reloading;
        private int fireCooldown;

        //ThunderTrail 是纯 CPU 顶点对象，不持有 GPU 缓冲，所以不像 Trail 那样
        //需要 Dispose，弹幕消失后交给 GC 即可
        //
        //ThunderTrail is a plain CPU-side vertex object holding no GPU buffers,
        //so unlike Trail it needs no Dispose; the GC reclaims it after the
        //projectile is gone.
        private ThunderTrail thunder;
        private int thunderTimer;

        private static Asset<Texture2D> thunderTexture;
        private static BasicEffect thunderApplyEffect;

        //给 DrillGunHoldAnimation 查"这个玩家是否正在换弹"用的快照。框架的
        //MeleeSwingAnimation 也用同样的按 whoAmI 建静态数组的办法存每人状态
        //
        //A snapshot for DrillGunHoldAnimation to ask "is this player
        //reloading". The framework's MeleeSwingAnimation stores per-player
        //state with the same static-array-by-whoAmI trick.
        private static readonly bool[] reloadingSnapshot = new bool[Main.maxPlayers + 1];

        internal static bool IsReloading(Player player)
            => player.ownedProjectileCounts[ModContent.ProjectileType<DrillHeld>()] > 0
            && reloadingSnapshot[player.whoAmI];

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;

            //中途加入的玩家也要能看到别人手里的枪
            //
            //Players who join mid-session should still see this gun in
            //someone's hands.
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        //最早的门控，跑在 Initialize/PreUpdate 之前：玩家切走武器就立刻收枪，
        //返回 false 会把本 tick 的后续逻辑全部拦断。收枪判定只在主人客户端做：
        //Item 底层的 GetItem 在光标上有物品时会退回本地 Main.mouseItem，拿它去
        //检查别人的枪会误杀。主人杀掉后，原版的弹幕消亡消息自会通知其余各端；
        //主人死亡也不用管——密封的 PostAI 会自动 Kill
        //
        //The earliest gate, running before Initialize/PreUpdate: put the gun
        //away the moment the player switches weapons; returning false cuts off
        //everything else this tick. Only the owner's client makes that call —
        //Item resolves through GetItem, which falls back to the local
        //Main.mouseItem while the cursor holds something, so checking someone
        //else's gun against it would kill it by mistake. Once the owner kills
        //the projectile, the vanilla kill message reaches everyone else. Owner
        //death needs no handling either — the sealed PostAI kills it.
        public override bool ExtraPreSet() {
            if (Projectile.IsOwnedByLocalPlayer() && Item.type != ModContent.ItemType<DrillGun>()) {
                Projectile.Kill();
                NetUpdate();
                return false;
            }
            return true;
        }

        //Initialize 在首个 tick 执行一次（ExtraPreSet 之后），适合放一次性的
        //登场表现。别把弹药这类权威计数器初始化写在这里：远程客户端随生成同步
        //就收到了第一份 NetHeld 数据，Initialize 跑在其后，会把刚收到的真值
        //抹掉。默认值交给字段初始化，权威值交给接收端覆盖
        //
        //Initialize runs once on the first tick, after ExtraPreSet — a good
        //home for one-shot entrance flair. Don't initialize authoritative
        //counters like ammo here: a remote client receives its first NetHeld
        //payload with the spawn sync, and Initialize runs after it, wiping the
        //value just received. Field initializers supply the defaults; the
        //receive side supplies the truth.
        public override void Initialize() {
            if (!Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.4f, Pitch = 0.3f }, Projectile.Center);
            }
        }

        //每 tick 的主逻辑。SetHeld 与 SetDirection 不会被基类自动调用，忘了前者
        //枪不算"手持"（原版不认 heldProj），忘了后者玩家不会转身面向鼠标。
        //ToMouse/UnitToMouseV/DownLeft/DownRight 在这里直接读——主人客户端写入
        //并在变化时自动发包，远程客户端读到的就是同步后的值，不需要自己同步鼠标。
        //
        //The per-tick brain. SetHeld and SetDirection are NOT auto-called by
        //the base: skip the former and vanilla won't treat the gun as held
        //(no heldProj), skip the latter and the player won't face the mouse.
        //ToMouse/UnitToMouseV/DownLeft/DownRight are read directly — the owner
        //client writes them and packets fire on change, so remote clients read
        //already-synced values with no manual mouse sync.
        public override bool PreUpdate() {
            SetHeld();
            SetDirection();
            UpdatePose();

            reloadingSnapshot[Owner.whoAmI] = reloading;

            //只要玩家还按着键或换弹没结束就续命；松手后 timeLeft 自然耗尽收枪
            //
            //Keep the projectile alive while a button is held or a reload is
            //running; once released, timeLeft simply runs out.
            if (DownLeft || DownRight || reloading) {
                Projectile.timeLeft = 2;
            }

            KeepUseBodyAlive();

            if (fireCooldown > 0) {
                fireCooldown--;
            }

            if (reloadTimer > 0) {
                reloadTimer--;
                if (reloadTimer == 0) {
                    FinishReload();
                }
            }
            else if (DownLeft && fireCooldown <= 0) {
                if (ammo <= 0) {
                    StartReload();
                }
                else {
                    FireOneShot();
                }
            }
            else if (DownRight && ammo < MagazineSize) {
                StartReload();
            }

            TickThunder();

            //返回 false 跳过原版 AI()——手持体不需要默认的弹幕运动逻辑
            //
            //Return false to skip vanilla AI(); a held body has no use for the
            //default projectile motion.
            return false;
        }

        //枪身姿态直接从 DrillGunHoldAnimation 的公开参数反推：物品是 noUseGraphic，
        //动画写进 itemLocation 的后座在物品贴图上看不见，所以让看得见的枪身复用
        //同一套 HoldDistance/Recoil 公式——参数只调一处，手臂和枪永远一致
        //
        //The gun pose is derived from DrillGunHoldAnimation's public knobs: the
        //item is noUseGraphic, so the recoil the animation writes into
        //itemLocation is invisible on the item sprite. Instead the visible gun
        //body reuses the same HoldDistance/Recoil formula — tune once, and arm
        //and gun always agree.
        private void UpdatePose() {
            Projectile.rotation = ToMouseA;

            float holdDistance = 20f;
            float kick = 0f;
            DrillGunHoldAnimation anim = ItemUseAnimation.GetAnimation<DrillGunHoldAnimation>();
            if (anim != null) {
                holdDistance = anim.HoldDistance;
                //与 AimedHoldAnimation.ApplyUseStyle 里的回退公式保持同一口径
                //
                //Same recovery curve as AimedHoldAnimation.ApplyUseStyle.
                float progress = ItemUseAnimation.GetUseProgress(Owner);
                if (anim.RecoilActive(Item, Owner) && progress < anim.RecoilPhase) {
                    kick = (anim.RecoilPhase - progress) / anim.RecoilPhase * anim.RecoilStrength;
                }
            }

            Projectile.Center = Owner.GetPlayerStabilityCenter() + UnitToMouseV * (holdDistance - kick);
        }

        //把玩家钉在"使用中"状态：itemAnimation 大于 0 原版才会调用 UseStyle 链，
        //DrillGunHoldAnimation 才有机会摆姿态。原版每 tick 减一，我们再托回去
        //
        //Pin the player in the "using an item" state: vanilla only dispatches
        //the UseStyle chain while itemAnimation > 0, which is what gives
        //DrillGunHoldAnimation its chance to pose. Vanilla decrements each
        //tick; we prop it back up.
        private void KeepUseBodyAlive() {
            if (Owner.itemAnimation < 2) {
                Owner.itemAnimation = 2;
            }
            if (Owner.itemAnimationMax < 2) {
                Owner.itemAnimationMax = 2;
            }
            if (Owner.itemTime < 2) {
                Owner.itemTime = 2;
            }
            if (Owner.itemTimeMax < 2) {
                Owner.itemTimeMax = 2;
            }
        }

        private Vector2 MuzzlePos => Projectile.Center + UnitToMouseV * MuzzleLength;

        //开火在所有客户端各跑一遍：DownLeft 是同步属性，远程客户端据此播放同样的
        //枪口特效和声音；但真正的钻头镖只由主人客户端生成，弹幕本身的网络同步会
        //把它带给其他人——两边都生成就是双倍弹幕
        //
        //Firing runs on every client: DownLeft is a synced property, so remote
        //clients replay the same muzzle flash and sound. The actual bolt is
        //spawned only on the owner's client; projectile netcode carries it to
        //everyone else — spawning on both sides would double the bolts.
        private void FireOneShot() {
            int cooldown = Math.Max(2, (int)(Item.useTime / Owner.GetAttackSpeed(Item.DamageType)));
            fireCooldown = cooldown;
            ammo--;

            //把 itemTime/itemAnimation 重置成本次射击的节奏，动画的
            //GetUseProgress 因此每发都从 0 走起，后座曲线逐发重播
            //
            //Reset itemTime/itemAnimation to this shot's cadence so the
            //animation's GetUseProgress restarts at 0 per shot, replaying the
            //recoil curve every time.
            Owner.itemTime = Owner.itemTimeMax = cooldown;
            Owner.itemAnimation = Owner.itemAnimationMax = cooldown;

            if (Projectile.IsOwnedByLocalPlayer()) {
                int damage = Owner.GetWeaponDamage(Item);
                float knockback = Owner.GetWeaponKnockback(Item);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), MuzzlePos, UnitToMouseV * Item.shootSpeed,
                    ModContent.ProjectileType<DrillBolt>(), damage, knockback, Owner.whoAmI);
                NetUpdate();
            }

            //粒子与音效是纯表现，专用服务器上一律不跑
            //
            //Particles and sounds are presentation only; never on a dedicated
            //server.
            if (Main.dedServ) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.5f, Pitch = Main.rand.NextFloat(-0.1f, 0.2f) }, MuzzlePos);
            for (int i = 0; i < 4; i++) {
                Dust dust = Dust.NewDustPerfect(MuzzlePos, DustID.Electric,
                    UnitToMouseV.RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 5f), 100, default, 0.8f);
                dust.noGravity = true;
            }
        }

        private void StartReload() {
            reloading = true;
            reloadTimer = ReloadTicks;

            //换弹期间同样维持"使用中"，动画继续端枪（RecoilActive 已在换弹时关闭）
            //
            //Stay in the "using" state through the reload so the animation
            //keeps the gun shouldered (recoil is gated off while reloading).
            Owner.itemTime = Owner.itemTimeMax = ReloadTicks;
            Owner.itemAnimation = Owner.itemAnimationMax = ReloadTicks;

            if (Projectile.IsOwnedByLocalPlayer()) {
                NetUpdate();
            }
            if (!Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.5f, Pitch = 0.1f }, Projectile.Center);
            }
        }

        //换弹完成。每个客户端都会在自己的 reloadTimer 走到 0 时进入这里：数值
        //上大家会归于同一个满弹匣，而闪电与音效正好借这个时机在各端本地播放
        //
        //Reload complete. Every client enters here when its own reloadTimer
        //hits zero: numerically everyone converges on a full magazine, and the
        //lightning and sound piggyback on that moment to play locally per
        //client.
        private void FinishReload() {
            ammo = MagazineSize;
            reloading = false;

            if (Projectile.IsOwnedByLocalPlayer()) {
                NetUpdate();
            }
            if (Main.dedServ) {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.45f, Pitch = 0.25f }, MuzzlePos);
            SpawnThunder();
        }

        //从枪口沿当前瞄准方向铺一条直线点集，交给 ThunderTrail 折出锯齿。
        //构造参数是宽度/颜色/透明度三个函数；SetRange 决定锯齿的随机横向偏移，
        //SetExpandWidth 再叠一圈圆形抖散。这道闪电不结算伤害，纯视觉
        //
        //Lay a straight line of points from the muzzle along the current aim
        //and let ThunderTrail crumple it into jags. The constructor takes
        //width/color/alpha functions; SetRange controls the random lateral
        //offset and SetExpandWidth adds a circular scatter on top. The bolt
        //deals no damage — visual only.
        private void SpawnThunder() {
            thunderTexture ??= ModContent.Request<Texture2D>(ThunderTexPath);

            const int pointCount = 9;
            const float reach = 170f;
            Vector2 start = MuzzlePos;
            Vector2 dir = UnitToMouseV;
            Vector2[] points = new Vector2[pointCount];
            for (int i = 0; i < pointCount; i++) {
                points[i] = start + dir * (reach * i / (pointCount - 1));
            }

            thunder ??= new ThunderTrail(thunderTexture, ThunderWidth, ThunderColor, ThunderAlpha) {
                CanDraw = true,
                UseNonOrAdd = true,
                PartitionPointCount = 2,
                FlowColor = new Color(200, 240, 255),
            };
            thunder.SetRange((0f, 7f));
            thunder.SetExpandWidth(4f);
            thunder.BasePositions = points;
            thunder.RandomThunder();
            thunderTimer = ThunderLife;
        }

        private void TickThunder() {
            if (Main.dedServ || thunderTimer <= 0) {
                return;
            }
            thunderTimer--;
            //每 3 tick 重掷一次随机形状，闪电看起来才会"跳"
            //
            //Re-roll the shape every 3 ticks so the bolt visibly flickers.
            if (thunderTimer > 0 && thunderTimer % 3 == 0) {
                thunder.RandomThunder();
            }
        }

        private float ThunderWidth(float factor) => 6f;
        private Color ThunderColor(float factor) => new Color(140, 220, 255);
        private float ThunderAlpha(float factor) => thunderTimer / (float)ThunderLife;

        //基类的 SendBitsByte 已经用掉 0/1 号位（左右键），子类必须先调 base 再从
        //2 号位往后写，直接覆盖会把按键状态吃掉。reloading 其实能从 reloadTimer
        //推出来，单独占一位是为了演示这个扩展方式
        //
        //The base SendBitsByte already occupies slots 0/1 (the mouse buttons);
        //a subclass must call base first and write from slot 2 onward, or the
        //button states get clobbered. reloading is technically derivable from
        //reloadTimer — it gets its own bit purely to demonstrate the pattern.
        public override BitsByte SendBitsByte(BitsByte flags) {
            flags = base.SendBitsByte(flags);
            flags[2] = reloading;
            return flags;
        }

        public override void ReceiveBitsByte(BitsByte flags) {
            base.ReceiveBitsByte(flags);
            reloading = flags[2];
        }

        //数值型的额外状态走 NetHeldSend/NetHeldReceive，它们运行在密封的
        //SendExtraAI/ReceiveExtraAI 内部，读写顺序必须一一对应。远程客户端在
        //两次包之间自己往下走计时器，包到达时被这里的权威值纠正
        //
        //Numeric extra state rides NetHeldSend/NetHeldReceive, which run inside
        //the sealed SendExtraAI/ReceiveExtraAI; write and read order must
        //match exactly. Between packets remote clients tick the timer on their
        //own and get corrected by the authoritative values arriving here.
        public override void NetHeldSend(BinaryWriter writer) {
            writer.Write7BitEncodedInt(ammo);
            writer.Write7BitEncodedInt(reloadTimer);
        }

        public override void NetHeldReceive(BinaryReader reader) {
            ammo = reader.Read7BitEncodedInt();
            reloadTimer = reader.Read7BitEncodedInt();
        }

        public override bool PreDraw(ref Color lightColor) {
            //TextureValue 是基类给的主纹理快捷方式；朝左时垂直翻转，否则枪是倒的
            //
            //TextureValue is the base class shortcut to the main texture; flip
            //vertically when aiming left or the gun draws upside down.
            Texture2D tex = TextureValue;
            SpriteEffects flip = Owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor,
                Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, flip, 0);

            if (thunderTimer > 0 && thunder != null) {
                DrawThunder();
            }
            return false;
        }

        //DrawThunder 内部直接调 DrawUserPrimitives，用的是设备上"当前已应用"的
        //着色器——在 PreDraw 里那可能是任何东西。先手动 Apply 一个参数已知的
        //BasicEffect，让闪电的变换矩阵和贴图采样有确定的来源。顶点坐标是屏幕空间
        //（ThunderTrail 内部减了 screenPosition），所以矩阵不带世界平移
        //
        //DrawThunder calls DrawUserPrimitives directly, riding whatever shader
        //is currently applied on the device — which inside PreDraw could be
        //anything. Applying a known BasicEffect first gives the lightning a
        //deterministic transform and texture source. Vertices are screen-space
        //(ThunderTrail subtracts screenPosition internally), hence no world
        //translation in the matrix.
        private void DrawThunder() {
            GraphicsDevice device = Main.instance.GraphicsDevice;
            thunderApplyEffect ??= new BasicEffect(device) {
                VertexColorEnabled = true,
                TextureEnabled = true,
            };
            thunderApplyEffect.World = VaultUtils.GetTransfromMatrix(Vector3.Zero);
            thunderApplyEffect.Texture = thunderTexture.Value;
            device.RasterizerState = RasterizerState.CullNone;
            foreach (EffectPass pass in thunderApplyEffect.CurrentTechnique.Passes) {
                pass.Apply();
            }
            thunder.DrawThunder(device);
        }

        //模组卸载跑在工作线程上，BasicEffect 这种由我们亲手创建的 GPU 资源
        //要排回主线程销毁；贴图归 tML 资产系统管，置空引用即可
        //
        //Mod unload runs on a worker thread, so the BasicEffect — the one GPU
        //resource we created ourselves — must be disposed back on the main
        //thread. The texture belongs to tML's asset system; dropping the
        //reference is enough.
        public override void Unload() {
            BasicEffect fx = thunderApplyEffect;
            thunderApplyEffect = null;
            thunderTexture = null;
            if (fx != null) {
                Main.QueueMainThreadAction(fx.Dispose);
            }
        }
    }
}
