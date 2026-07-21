using InnoVault.GameSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Overrides
{
    //透镜画廊：Player / Proj / Tile / Scene / Menu / Dust 六种覆写各给一个最小钩子，
    //全部只在透镜开着时有表现，关掉透镜世界立刻恢复原样。
    //每个类刻意只做一件事，方便对照 wiki 各子页阅读。
    //
    //The lens gallery: one minimal hook each for the Player / Proj / Tile / Scene /
    //Menu / Dust override types, all of them visible only while the lens is on and
    //gone the moment it is off. Each class deliberately does exactly one thing so it
    //reads side by side with its wiki page.
    //
    //https://innovault.wiki/cn/overrides/

    //PlayerOverride 没有 TargetID：类一加载就注册，进世界时按玩家克隆，Player 指向宿主。
    //ModifyGravity 是它独有的钩子（ModPlayer 没有对应物），拿来当演示最合适。
    //
    //PlayerOverride has no TargetID: it registers on load and is cloned per player,
    //with Player pointing at the host. ModifyGravity is a hook ModPlayer simply lacks,
    //which makes it the right pick for this showcase.
    internal class LensPlayerOverride : PlayerOverride
    {
        public override void ModifyGravity(ref float gravity) {
            //只改本地玩家：Terraria 的玩家位置以所属客户端为准再向外广播，
            //这种手感级的微调不需要同步，远端只会收到结果坐标
            //
            //Local player only: player positions are authoritative on the owning client
            //and broadcast outward, so a feel-level tweak like this needs no sync —
            //remote ends only ever see the resulting coordinates.
            if (Player.whoAmI == Main.myPlayer && LensPlayer.LocalLensOn) {
                gravity *= 0.88f;
            }
        }

        public override void PostUpdate() {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer || !LensPlayer.LocalLensOn) {
                return;
            }
            //justJumped 只在起跳那一帧为真，正好放一圈脚下的云雾
            //
            //justJumped is true only on the takeoff frame — one puff of cloud, no timer.
            if (Player.justJumped) {
                for (int i = 0; i < 10; i++) {
                    Dust dust = Dust.NewDustPerfect(Player.Bottom, DustID.Cloud, Main.rand.NextVector2Circular(2.5f, 1f));
                    dust.noGravity = true;
                }
            }
        }
    }

    //ProjOverride 与 ItemOverride 共用一套 TargetID 语义：>0 单个弹幕 ID，-1 全体弹幕，
    //0（默认）不注册。这里挂在原版木箭上，让它飞行途中掉落微光。
    //
    //ProjOverride shares ItemOverride's TargetID semantics: >0 one projectile id,
    //-1 every projectile, 0 (default) unregistered. This one rides the vanilla wooden
    //arrow and sheds a faint trail in flight.
    internal class LensArrowOverride : ProjOverride
    {
        public override int TargetID => ProjectileID.WoodenArrowFriendly;

        public override bool AI() {
            //弹幕在所有端模拟，粒子只让客户端出。返回 true 保留原版箭矢的重力与转向；
            //返回 false 会连原版 AI 一起拦掉
            //
            //Projectiles simulate on every end; only clients spawn the dust. Returning
            //true keeps the vanilla arrow's gravity and steering; false would block the
            //vanilla AI as well.
            if (!Main.dedServ && LensPlayer.LocalLensOn && Main.rand.NextBool(3)) {
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.YellowTorch, projectile.velocity * 0.1f);
                dust.noGravity = true;
                dust.scale = 0.9f;
            }
            return true;
        }
    }

    //TileOverride 是 GlobalTile 级的轻量钩子层，不是 TileProcessor——那是带存档、
    //网络和多层绘制的物块实体。注意 TargetID 语义在这里拐了个弯：-1（默认）表示
    //不注册，想覆盖全部物块要用 -2，照抄 Item/Proj 的写法会静默失效。
    //
    //TileOverride is a light GlobalTile-level hook layer, not TileProcessor (that one
    //is a tile-attached entity with saving, networking and layered drawing). Mind the
    //TargetID twist here: -1 (the default) means "not registered", and all-tiles is -2 —
    //copying the Item/Proj pattern would silently do nothing.
    internal class LensWorkbenchOverride : TileOverride
    {
        public override int TargetID => TileID.WorkBenches;

        public override void MouseOver(int i, int j) {
            if (!LensPlayer.LocalLensOn) {
                return;
            }
            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<OverrideLens>();
        }

        public override bool? RightClick(int i, int j, Tile tile) {
            if (!LensPlayer.LocalLensOn) {
                return null;
            }
            CombatText.NewText(Main.LocalPlayer.Hitbox, Color.LightSkyBlue, Language.GetTextValue("Mods.InnoVaultExample.Overrides.WorkbenchHint"));
            for (int k = 0; k < 8; k++) {
                Dust dust = Dust.NewDustPerfect(new Vector2(i * 16 + 8, j * 16), DustID.BlueTorch, Main.rand.NextVector2Circular(1.5f, 1.5f));
                dust.noGravity = true;
            }
            //返回非 null 才会接管右键判定结果；工作台本来就没有右键行为，保持 null 不打扰链
            //
            //Only a non-null return takes over the right-click verdict; a workbench has
            //no vanilla right-click anyway, so stay null and leave the chain alone.
            return null;
        }
    }

    //SceneOverride 用的是加载期注册的单例原型，运行时没有"当前玩家"概念，
    //门控只能读静态状态。DecideMusic 跑在原版音乐判定之后，是场景钩子里最直观的一个。
    //
    //SceneOverride runs on the load-time singleton prototype with no notion of a
    //"current player", so the gate has to be static state. DecideMusic runs after the
    //vanilla music pick and is the most immediately noticeable hook of the group.
    internal class LensSceneOverride : SceneOverride
    {
        public override void DecideMusic() {
            if (Main.gameMenu || !LensPlayer.LocalLensOn) {
                return;
            }
            //开着透镜就切到蘑菇地音乐——差异大到不会被当成巧合，关掉立刻还原
            //
            //With the lens on, swap to the glowing mushroom track — different enough
            //that nobody calls it coincidence, and it reverts the moment you toggle off.
            Main.newMusic = MusicID.Mushrooms;
        }
    }

    //MenuOverride 没有 TargetID，所有实例都参与标题界面的绘制与逻辑。
    //主菜单没有玩家实例，所以这里读的是 LensPlayer.MenuBadgeOn 那个静态镜像：
    //开着透镜退回标题，左下角就会出现这行角标。
    //
    //MenuOverride has no TargetID; every instance takes part in title-screen drawing
    //and logic. The main menu has no player instance, so the gate reads the static
    //mirror LensPlayer.MenuBadgeOn: quit to title with the lens on and the badge shows
    //bottom-left.
    internal class LensMenuOverride : MenuOverride
    {
        private int ticks;

        //MenuLogicUpdate 由框架在绘制线程上以固定 60 tick/s 驱动。动画计时放这里
        //而不是画钩子里，免得跟帧率绑死。
        //
        //MenuLogicUpdate is driven by the framework at a fixed 60 ticks per second on
        //the draw thread. Animation timers belong here rather than in the draw hook,
        //so they stay frame-rate independent.
        public override void MenuLogicUpdate() {
            ticks++;
        }

        public override void PostDrawMenu(GameTime gameTime) {
            if (!LensPlayer.MenuBadgeOn) {
                return;
            }
            float pulse = 0.7f + 0.3f * MathF.Sin(ticks * 0.05f);
            string text = Language.GetTextValue("Mods.InnoVaultExample.Overrides.MenuBadge");
            Utils.DrawBorderString(Main.spriteBatch, text, new Vector2(24f, Main.screenHeight - 48f), Color.LightSkyBlue * pulse, 0.85f);
        }
    }

    //GlobalDust 没有 TargetID，所有尘埃生成都会路过 OnSpawn。尘埃是纯客户端粒子，
    //不参与网络同步；专用服务器上 NewDust 一律返回 6000 号哑弹槽位，改了它也没有
    //任何东西会画出来，所以这里省掉 dedServ 判断也无妨。
    //
    //GlobalDust has no TargetID; every dust spawn passes through OnSpawn. Dust is a
    //purely client-side particle with no networking; on a dedicated server NewDust
    //always hands back the dud slot 6000, where nothing we touch would ever be
    //drawn — safe to skip the dedServ check here.
    internal class LensTorchDust : GlobalDust
    {
        public override void OnSpawn(Dust dust) {
            if (!LensPlayer.LocalLensOn || dust.type != DustID.Torch) {
                return;
            }
            //把火把的橙色火焰染成青绿色，路过任何点燃的火把立刻能看出差别
            //
            //Tint the torch's orange flame teal; any lit torch shows the change at once.
            dust.color = new Color(80, 255, 170);
            dust.scale *= 1.15f;
        }
    }
}
