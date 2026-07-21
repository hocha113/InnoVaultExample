using InnoVault;
using InnoVault.Actors;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Persistence
{
    //漂浮信标：一个既不是 NPC 也不是弹幕的自定义实体。寿命和颜色是 [SyncVar]，
    //由权威端推进、框架自动同步到各客户端；寿命耗尽后消亡。配套的信标杖演示
    //客户端调 NewActor 时"返回 -1、请求被路由到服务器"的生成语义。
    //
    //A floating beacon: a custom entity that is neither NPC nor projectile. Lifetime
    //and color are [SyncVar] fields, advanced on the authority and auto-synced to
    //clients; the beacon dies when its lifetime runs out. The companion rod shows the
    //client-side NewActor semantics: it returns -1 and routes the request to the server.
    //https://innovault.wiki/cn/persistence/actor/

    internal class FloatBeaconActor : Actor
    {
        //光斑贴图交给 VaultLoaden 自动加载：路径以 / 结尾时会拼上成员名。
        //它只在客户端加载，专用服务器上是 null——绘制端有 dedServ 守卫兜着
        //
        //The glow texture is auto-loaded by VaultLoaden: a path ending in / gets the
        //member name appended. It loads on clients only and stays null on a dedicated
        //server - the dedServ guard in the draw hook covers that
        [VaultLoaden("Assets/Effects/")]
        internal static Asset<Texture2D> DiffusionCircle { get; set; }

        internal const int MaxLifetime = 540;

        //[SyncVar] 按"基类先、同层按名字典序"的固定顺序序列化，私有字段也支持。
        //位置、速度、Active 这些基础字段 Actor 已经内置同步，这里只补业务状态
        //
        //[SyncVar] members serialize in a fixed order (base class first, name order
        //within each level); private fields work too. Position, velocity and Active
        //are already built-in synced on Actor, so only business state is added here
        [SyncVar]
        private int lifetime;

        [SyncVar]
        private Color glowColor;

        public override void OnSpawn(params object[] args) {
            Width = 32;
            Height = 32;
            DrawLayer = ActorDrawLayer.AfterPlayers;

            //同步字段只由权威端播种。客户端的生成流程是：先把服务器生成包里的权威
            //SyncVar 状态写进实例，然后才调 OnSpawn——这里不加守卫，客户端就会把
            //刚收到的值又盖掉。另外单人下 isServer 是 false，漏了 isSinglePlayer
            //整个初始化在单人里就不会执行。
            //
            //Synced fields are seeded by the authority only. On clients the spawn flow
            //applies the authoritative SyncVar state from the server packet first and
            //calls OnSpawn afterwards - without this guard the client would overwrite
            //the values it just received. Also, isServer is false in single player;
            //drop isSinglePlayer and none of this runs solo.
            if (VaultUtils.isServer || VaultUtils.isSinglePlayer) {
                lifetime = MaxLifetime;
                glowColor = Color.Lerp(Color.Gold, Color.DeepSkyBlue, Main.rand.NextFloat());
            }
        }

        public override void AI() {
            //寿命与颜色只在权威端改，客户端只消费同步下来的值。字段的常规变化由
            //服务器自动做增量广播；NetUpdate = true 是"这一变化必须立刻全量到达"
            //的手动开关，适合阶段切换这类不能等心跳的状态翻转。
            //
            //Lifetime and color change on the authority only; clients just consume the
            //synced values. Routine changes are delta-broadcast automatically by the
            //server; NetUpdate = true is the manual "this must arrive in full, now"
            //switch, for phase flips that cannot wait for the heartbeat.
            if (VaultUtils.isServer || VaultUtils.isSinglePlayer) {
                lifetime--;

                if (lifetime == 120) {
                    glowColor = Color.OrangeRed;
                    NetUpdate = true;
                }

                if (lifetime <= 0) {
                    //消亡走 KillActor（RequestKill 是它的实例便捷包装）：释放槽位并向
                    //客户端广播销毁包。只把 Active 置 false 是不够的——服务器的广播
                    //循环会直接跳过非激活实体，客户端永远收不到死讯，槽位也不回收。
                    //
                    //Death goes through KillActor (RequestKill is its instance shorthand):
                    //it frees the slot and broadcasts the kill to clients. Merely setting
                    //Active = false is not enough - the server broadcast loop skips
                    //inactive entities, so clients never hear about the death and the
                    //slot is never recycled.
                    RequestKill();
                    return;
                }
            }

            //漂浮动画在所有端都跑：位置本身是 SyncVar，客户端本地积分出的运动
            //会被权威状态平滑纠偏，既流畅又不会漂走
            //
            //The float animation runs on every peer: position itself is a SyncVar, so
            //client-side integration gets smoothly reconciled toward the authoritative
            //state - fluid motion without drifting apart
            Velocity *= 0.96f;
            Velocity.Y -= MathF.Sin(Main.GameUpdateCount * 0.05f + WhoAmI) * 0.012f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            //专用服务器没有图形设备也没有贴图。框架的绘制循环本就不在服务器上跑，
            //这道守卫是习惯性保险
            //
            //A dedicated server has no graphics device and no textures. The framework
            //never runs its draw loop there anyway; the guard is habitual insurance
            if (Main.dedServ) {
                return false;
            }

            Texture2D glow = DiffusionCircle.Value;
            float progress = MathHelper.Clamp(lifetime / (float)MaxLifetime, 0f, 1f);
            float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + WhoAmI) * 0.12f;
            float scale = (0.3f + progress * 0.4f) * pulse;

            spriteBatch.Draw(glow, Center - Main.screenPosition, null, glowColor * (0.35f + progress * 0.55f)
                , 0f, glow.Size() / 2f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glow, Center - Main.screenPosition, null, Color.White * (0.25f + progress * 0.3f)
                , 0f, glow.Size() / 2f, scale * 0.45f, SpriteEffects.None, 0f);

            //返回 false 跳过框架的占位默认绘制，光斑就是这个实体的全部形象
            //
            //Return false to skip the framework's placeholder draw; the glow is the
            //entity's entire look
            return false;
        }
    }

    //信标杖：对着鼠标位置放一个信标。贴图借用原版闪光魔杖。
    //
    //The beacon rod: drops a beacon at the cursor. Sprite borrowed from the vanilla
    //Wand of Sparking.
    internal class BeaconRod : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.WandofSparking}";

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item8;
        }

        //生成只由"操作者本人的那一端"发起：单人下 NewActor 直接分配槽位并返回；
        //多人客户端返回 -1，框架自动把生成请求发给服务器，由服务器分配槽位、
        //再向所有客户端广播生成包——所以拿返回值当句柄在客户端行不通。
        //守卫必不可少：UseItem 在服务器和每个客户端上都会跑（远端也模拟使用动画），
        //不拦的话服务器直接生成一枚、每个客户端又各发一次生成请求，信标会成倍冒出来。
        //
        //Spawning is initiated only by the peer operating the item: solo, NewActor
        //allocates a slot and returns it; on a multiplayer client it returns -1 and
        //the framework routes the request to the server, which assigns the slot and
        //broadcasts the spawn - so the return value is no handle on clients. The
        //guard is essential: UseItem runs on the server and on every client (remote
        //peers simulate the use animation too), so without it the server spawns one
        //directly and each client also files a spawn request - beacons multiply.
        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                ActorLoader.NewActor<FloatBeaconActor>(Main.MouseWorld - new Vector2(16f, 16f));
            }

            UseCounterSave.CountLocalUse(player);
            ProgressPlayer.RecordDemoUse(player, "FloatBeacon");
            return true;
        }
    }

    internal class FloatBeaconEntry : IDemoEntry
    {
        public string Id => "FloatBeacon";
        public string Chapter => "Persistence";
        public string WikiSlug => "persistence/actor";
        public string SourcePath => "Content/Persistence/FloatBeacon.cs";
        public int DemoItemType => ModContent.ItemType<BeaconRod>();
        public int SortIndex => 3;
    }
}
