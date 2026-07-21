using InnoVault;
using InnoVault.TileProcessors;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InnoVaultExample.Content.TileEntities
{
    //哨戒炮台：放下后自动扫描附近的敌对 NPC，发射同文件下方自制的友方弹丸。
    //重点看 Update 里的那道权威判断——发射只发生在服务器或单人端，
    //客户端的 Update 与 Draw 只负责灯光和瞄准线这类纯视觉的部分。
    //
    //The sentry turret: place it and it scans for nearby hostile NPCs and fires
    //the little friendly bolt defined further down this file. The point of
    //interest is the authority gate in Update: firing happens only on the server
    //or in single player, while client-side Update and Draw stick to cosmetics
    //like light and the aiming line.
    //https://innovault.wiki/cn/content/tile-processor/

    internal class DemoTurretItem : ModItem
    {
        //借原版一秒计时器的物品贴图（ItemID 583），和下面的物块外观正好配套
        //
        //Borrow the vanilla 1 Second Timer sprite (ItemID 583), which matches the
        //tile look below
        public override string Texture => "Terraria/Images/Item_583";

        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<DemoTurretTile>());
            Item.value = Item.buyPrice(silver: 8);
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.IronBar, 3).AddTile(TileID.Anvils).Register();
        }
    }

    internal class DemoTurretTile : ModTile
    {
        //物块贴图借原版计时器图集，1x1 摆在地上就是个小装置。
        //炮台没有右键交互，所以不开 HasOutlines
        //
        //The tile sheet is the vanilla timer one; a 1x1 gadget sitting on the
        //ground. The turret has no right-click interaction, so HasOutlines stays off
        public override string Texture => "Terraria/Images/Tiles_144";

        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(200, 90, 60), CreateMapEntryName());
            DustType = DustID.Iron;
        }
    }

    internal class DemoTurretTP : TileProcessor
    {
        public override int TargetTileID => ModContent.TileType<DemoTurretTile>();

        private const float ScanRange = 600f;
        private const int FireInterval = 45;
        private int cooldown;

        //炮口位置：图格中心再抬高一点，弹幕和瞄准线都从这里出发
        //
        //The muzzle: a bit above the tile center, where both projectiles and the
        //aiming line originate
        private Vector2 Muzzle => CenterInWorld - new Vector2(0f, 10f);

        public override void SetProperty() {
            //所有玩家都离开 2000 像素后，这个实体会暂停更新。IdleDistance 是 TP 自带的
            //性能阀门，默认 -1 表示不启用；哨戒炮台没人看着就不必工作，正适合打开
            //
            //Once every player is more than 2000 pixels away the entity stops
            //updating. IdleDistance is the TP's built-in performance valve, off by
            //default at -1; a sentry nobody is around to see is a fine thing to pause.
            IdleDistance = 2000;
        }

        public override void Update() {
            //Update 在每个端都跑。灯光是纯视觉，放在权威判断之前，
            //让所有有画面的端都能看到炮口的待机红光
            //
            //Update ticks on every peer. The light is pure cosmetics, so it sits
            //before the authority gate and every peer with a screen sees the idle
            //red glow at the muzzle.
            if (!Main.dedServ) {
                float pulse = 0.6f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.25f;
                Lighting.AddLight(Muzzle, new Vector3(0.4f, 0.12f, 0.08f) * pulse);
            }

            //发射会改变世界状态，只允许权威端做。服务器生成的弹幕会自动同步给所有客户端；
            //客户端要是也生成一份，那就是只有自己看得见、打不中任何东西的幻影弹
            //
            //Firing mutates world state, so only the authority does it. A projectile
            //spawned on the server syncs to every client automatically; one spawned on
            //a client would be a phantom only that player sees and that hits nothing.
            if (!VaultUtils.isServer && !VaultUtils.isSinglePlayer) {
                return;
            }

            if (cooldown > 0) {
                cooldown--;
                return;
            }

            //VaultUtils 里现成的最近目标扫描：内部用 CanBeChasedBy 过滤掉友方与不可攻击者，
            //ignoreTiles 传 false 让炮台不会隔着墙开火
            //
            //VaultUtils ships a closest-target scan: it filters friendlies and
            //untargetables via CanBeChasedBy, and ignoreTiles=false stops the turret
            //from shooting through walls.
            NPC target = Muzzle.FindClosestNPC(ScanRange, ignoreTiles: false);
            if (target == null) {
                return;
            }

            Vector2 velocity = (target.Center - Muzzle).SafeNormalize(-Vector2.UnitY) * 11f;

            //弹幕用同文件下方自制的 DemoTurretBolt。阵营是弹幕类型的一部分：friendly 才与
            //NPC 碰撞、hostile 只与玩家碰撞，而这对 flag 由每个端各自跑 SetDefaults 重建，
            //生成后再翻改只动得了本端那一份。借原版弹幕还有一对现成的地雷——DemonScythe(45)
            //是友方法术，DemonSickle(44) 才是恶魔扔的敌对镰刀，名字几乎一样、阵营正好相反。
            //自带一枚弹幕，语义就钉死了
            //
            //The projectile is our own DemoTurretBolt from further down this file.
            //Faction is part of the projectile type: friendly collides with NPCs,
            //hostile with players, and every peer rebuilds those flags by running
            //SetDefaults itself — flipping them after spawning only touches the local
            //copy. Borrowing vanilla projectiles also has a ready-made landmine:
            //DemonScythe (45) is the friendly spell, DemonSickle (44) the demons'
            //hostile blade, near-identical names with opposite factions. Owning the
            //bolt nails the semantics down.
            //
            //owner 仍传 Main.myPlayer：服务器上它是 255，即"属于世界"的弹幕。
            //friendly 弹幕的命中由它的 owner 端结算，owner 为 255 时那正是服务器自己——
            //城镇 NPC 的武器走的同一条路
            //
            //Still pass Main.myPlayer as owner: on the server that is 255, a projectile
            //owned by the world itself. A friendly projectile's hits are settled by its
            //owner's peer, and with owner 255 that peer is the server — the same route
            //town NPC weapons take.
            Projectile.NewProjectile(new EntitySource_WorldEvent(), Muzzle, velocity,
                ModContent.ProjectileType<DemoTurretBolt>(), 24, 2f, Main.myPlayer);
            cooldown = FireInterval;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //Draw 只在客户端调用。这里再扫一次目标，但只用来画瞄准线：
            //就算某一帧和服务器选中的目标不一致，也不会影响真正的射击判定——
            //这就是"权威端定生死、客户端只管好看"的分工
            //
            //Draw runs on clients only. We scan again here purely for the aiming
            //line: even if a frame disagrees with the server's pick, actual firing is
            //untouched. That is the split: the authority decides, clients decorate.
            NPC target = Muzzle.FindClosestNPC(ScanRange, ignoreTiles: false);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle onePixel = new(0, 0, 1, 1);
            Vector2 muzzleScreen = Muzzle - Main.screenPosition;

            if (target != null) {
                Vector2 toTarget = target.Center - Muzzle;
                spriteBatch.Draw(pixel, muzzleScreen, onePixel, Color.Red * 0.35f,
                    toTarget.ToRotation(), new Vector2(0f, 0.5f), new Vector2(toTarget.Length(), 2f), SpriteEffects.None, 0f);
            }

            float pulse = 0.7f + MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.3f;
            spriteBatch.Draw(pixel, muzzleScreen, onePixel, Color.OrangeRed * pulse,
                0f, new Vector2(0.5f), new Vector2(4f), SpriteEffects.None, 0f);
        }
    }

    //炮台的弹丸。写成自己的类而不是借原版编号，是为了把阵营钉死在 SetDefaults 里：
    //friendly 与 NPC 碰撞、hostile 与玩家碰撞，这对 flag 属于弹幕类型本身，
    //每个端生成弹幕时都会各自重建，想在生成后翻改是同步不出去的
    //
    //The turret's bolt. It is our own class instead of a borrowed vanilla ID so
    //the faction is nailed down inside SetDefaults: friendly collides with NPCs,
    //hostile with players, and the pair belongs to the projectile type itself —
    //every peer rebuilds it when the projectile spawns, so flipping the flags
    //after spawning never makes it across the wire.
    internal class DemoTurretBolt : ModProjectile
    {
        //弹体不画贴图，挂一张原版占位图让资源管线闭嘴，弹道交给尘埃
        //
        //No sprite gets drawn; the vanilla placeholder keeps the asset pipeline
        //satisfied and the dust trail carries the visuals
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;

            //npcProj 标记"这不是玩家打出去的"：城镇 NPC 的武器就带着它。
            //有了它，原版不会把命中归因到 owner 编号对应的玩家身上，
            //也不会给这枚弹丸叠玩家的附魔效果
            //
            //npcProj marks "no player fired this": town NPC weapons carry it.
            //With it set, vanilla neither attributes hits to whatever player the
            //owner index happens to point at, nor stacks player enchantments
            //onto the bolt.
            Projectile.npcProj = true;

            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            if (Main.dedServ) {
                return;
            }
            Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(2f, 2f),
                DustID.Torch, -Projectile.velocity * 0.2f, 100, default, 1.2f);
            d.noGravity = true;
            Lighting.AddLight(Projectile.Center, 0.35f, 0.18f, 0.08f);
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 1.3f);
                d.noGravity = true;
            }
        }

        //贴图是占位符，跳过绘制，和 DemoBossBolt 同一套做法
        //
        //The texture is a placeholder; skip drawing, same trick as DemoBossBolt
        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal sealed class DemoTurretEntry : IDemoEntry
    {
        public string Id => "DemoTurret";
        public string Chapter => "TileEntities";
        public string WikiSlug => "content/tile-processor";
        public string SourcePath => "Content/TileEntities/DemoTurret.cs";
        public int DemoItemType => ModContent.ItemType<DemoTurretItem>();
        public int SortIndex => 1;
    }
}
