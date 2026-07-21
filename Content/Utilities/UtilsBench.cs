using InnoVault;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

//VaultUtils 精选演示台：使用工具演示台后，头顶播放约四秒的可视化——缓动函数驱动
//光点沿贝塞尔曲线飞行、目标查询锁定最近敌人并连线、再配一个现成的旋转光环绘制辅助。
//wiki 的 utilities 章节按主题分页整理了常用方法，更细的签名以源码为准，按需 grep 方法名。
//
//A hand-picked VaultUtils showcase: using the toolbox bench plays a ~4 second
//visual above your head - an eased orb flying along a Bezier curve, the
//nearest-enemy query drawing a lock-on line, and a ready-made rotating halo
//draw helper. The wiki's utilities chapter groups the common helpers by topic;
//for exact signatures, grep the method name in the source.
//https://innovault.wiki/cn/utilities/

namespace InnoVaultExample.Content.Utilities
{
    internal class UtilsBenchSystem : ModSystem
    {
        //Asset<Texture2D> 交给 [VaultLoaden] 自动加载（带 setter 的属性同样支持，
        //还不会像字段那样被编译器怀疑"从未赋值"）；LoadAsset 只在客户端跑，
        //服务器上保持 null，而下面的绘制代码在服务器上根本不会被调用
        //
        //The Asset<Texture2D> members load automatically via [VaultLoaden]
        //(settable properties work just like fields, minus the compiler's
        //"never assigned" suspicion); LoadAsset only runs on clients, so on a
        //server they stay null - and the drawing code below never runs there
        [VaultLoaden("Assets/Effects/StarTexture")]
        private static Asset<Texture2D> StarTex { get; set; }
        [VaultLoaden("Assets/Effects/DiffusionCircle")]
        private static Asset<Texture2D> CircleTex { get; set; }

        private const int Duration = 240;
        private static int timer;
        private static readonly List<Vector2> orbTrail = [];

        //只由本地客户端触发（见 ToolboxBench.UseItem），整段演示不碰任何游戏状态
        //
        //Triggered only by the local client (see ToolboxBench.UseItem); the whole
        //show never touches game state
        internal static void Trigger() {
            timer = Duration;
            orbTrail.Clear();
        }

        public override void PostUpdateEverything() {
            //纯客户端视觉计时器，专用服务器不需要跑
            //
            //A client-only visual timer; a dedicated server has no use for it
            if (Main.dedServ || timer <= 0) {
                return;
            }
            if (--timer == 0) {
                orbTrail.Clear();
            }
        }

        //挑 PostDrawTiles 当画布：tModLoader 自带的世界层钩子，进入时 SpriteBatch
        //未开启，自己 Begin/End 并传 GameViewMatrix 就能按世界坐标作画。
        //该钩子只在客户端运行，无需再守卫 dedServ
        //
        //PostDrawTiles is the canvas: a stock tModLoader world-layer hook where
        //the SpriteBatch arrives closed, so Begin/End with GameViewMatrix lets us
        //draw in world coordinates. The hook is client-only, no dedServ guard needed
        public override void PostDrawTiles() {
            if (timer <= 0) {
                return;
            }
            Player player = Main.LocalPlayer;
            //VaultUtils.Alives：一行搞定 null + active + 死亡三连判断
            //
            //VaultUtils.Alives: null + active + dead checks in a single call
            if (!player.Alives()) {
                return;
            }

            float progress = 1f - timer / (float)Duration;
            //包络用两条缓动拼出来：EaseOutCubic 前 20 tick 冲入，EaseInOutCubic
            //最后 40 tick 收尾，整段演示淡入淡出都不会跳变
            //
            //The envelope is two easings glued together: EaseOutCubic ramps in
            //over the first 20 ticks, EaseInOutCubic fades out over the last 40,
            //so the show never pops in or out
            float envelope = VaultUtils.EaseOutCubic(Math.Min((Duration - timer) / 20f, 1f))
                * VaultUtils.EaseInOutCubic(Math.Min(timer / 40f, 1f));

            SpriteBatch sb = Main.spriteBatch;
            //全部素材都是黑底/白色光效图，用 Additive 叠加最省事：黑色自动消失，
            //白色变成辉光，还不用管纹理的 alpha 通道到底长什么样
            //
            //All the art here is black-background / white glow imagery, so
            //Additive blending is the easy win: black vanishes, white glows, and
            //the texture's alpha channel stops mattering
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState
                , DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Vector2 anchor = player.Top + new Vector2(0f, -34f);
            Texture2D circle = CircleTex.Value;
            Texture2D star = StarTex.Value;

            //DrawRotatingMarginEffect：想给图标加"旋转双层光环"时不必自己写
            //相位循环，这个现成方法一次画完；GetOrig 顺手把纹理中心当原点
            //
            //DrawRotatingMarginEffect: when an icon needs the "two rotating
            //halo rings" treatment, this ready-made helper does it in one call
            //instead of hand-rolling phase loops; GetOrig grabs the texture
            //center as the origin
            VaultUtils.DrawRotatingMarginEffect(sb, circle, (int)(progress * Duration)
                , anchor - Main.screenPosition, null, new Color(90, 170, 255) * (0.55f * envelope)
                , 0f, circle.GetOrig(), 0.5f);
            sb.Draw(circle, anchor - Main.screenPosition, null, Color.White * (0.75f * envelope)
                , 0f, circle.GetOrig(), 0.3f, SpriteEffects.None, 0f);

            //缓动 + 曲线：EaseInOutCubic 把线性进度变成慢-快-慢的节奏，
            //Bezier 用三个控制点把光点甩过头顶——两者组合就是"手写轨迹动画"的
            //最小配方
            //
            //Easing + curve: EaseInOutCubic turns linear progress into a
            //slow-fast-slow rhythm, and Bezier swings the orb over your head
            //through three control points - together they are the minimal recipe
            //for hand-written path animation
            float eased = VaultUtils.EaseInOutCubic(progress);
            Vector2 pathStart = anchor + new Vector2(-96f, 30f);
            Vector2 pathPeak = anchor + new Vector2(0f, -110f);
            Vector2 pathEnd = anchor + new Vector2(96f, 30f);
            Vector2 orbPos = VaultUtils.Bezier(pathStart, pathPeak, pathEnd, eased);

            orbTrail.Add(orbPos);
            if (orbTrail.Count > 18) {
                orbTrail.RemoveAt(0);
            }

            //MultiStepColorLerp：多停靠点渐变一行出结果，光点从青到白再烧成金色
            //
            //MultiStepColorLerp: multi-stop gradients in one line; the orb runs
            //cyan through white into gold
            Color orbColor = VaultUtils.MultiStepColorLerp(eased, Color.Cyan, Color.White, Color.Gold);
            for (int i = 0; i < orbTrail.Count; i++) {
                float k = (i + 1) / (float)orbTrail.Count;
                sb.Draw(star, orbTrail[i] - Main.screenPosition, null, orbColor * (0.3f * k * envelope)
                    , 0f, star.GetOrig(), 0.06f + 0.1f * k, SpriteEffects.None, 0f);
            }
            sb.Draw(star, orbPos - Main.screenPosition, null, orbColor * envelope
                , 0f, star.GetOrig(), 0.24f, SpriteEffects.None, 0f);

            //目标查询：TryFindClosestNPC 一次调用替掉"遍历 Main.npc、筛可追踪、
            //比距离"的整段样板；默认 ignoreTiles 为 true，即隔墙也能锁定。
            //InnoVault 没有现成的"画线"工具，这里用 MagicPixel 拉伸手写一条
            //
            //Targeting: one TryFindClosestNPC call replaces the whole "loop
            //Main.npc, filter chaseable, compare distances" boilerplate; the
            //ignoreTiles default of true means walls do not block the lock-on.
            //InnoVault has no line-drawing helper, so the beam is hand-rolled by
            //stretching MagicPixel
            if (player.Center.TryFindClosestNPC(out NPC target, 900f) && target.Alives()) {
                Vector2 from = player.Center;
                Vector2 delta = target.Center - from;
                Color beamColor = new Color(255, 120, 90) * (0.5f * envelope);
                sb.Draw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, new Rectangle(0, 0, 1, 1)
                    , beamColor, delta.ToRotation(), new Vector2(0f, 0.5f)
                    , new Vector2(delta.Length(), 2f), SpriteEffects.None, 0f);
                float ping = 0.3f + 0.06f * MathF.Sin((float)Main.timeForVisualEffects * 0.25f);
                sb.Draw(circle, target.Center - Main.screenPosition, null, beamColor * 1.4f
                    , 0f, circle.GetOrig(), ping, SpriteEffects.None, 0f);
            }

            sb.End();
        }

        public override void Unload() {
            timer = 0;
            orbTrail.Clear();
        }
    }

    internal class ToolboxBench : ModItem
    {
        public override string Texture => "InnoVaultExample/Assets/Badges/Badge_0";

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = Item.useAnimation = 30;
            Item.UseSound = SoundID.Item6;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 30);
        }

        //远程玩家使用物品时各客户端也会执行 UseItem，靠 myPlayer 只在触发方本地开演
        //
        //UseItem also runs on every client when remote players use the item; the
        //myPlayer check keeps the show local to whoever triggered it
        public override bool? UseItem(Player player) {
            if (!Main.dedServ && player.whoAmI == Main.myPlayer) {
                UtilsBenchSystem.Trigger();
            }
            return true;
        }
    }

    internal class UtilsBenchEntry : IDemoEntry
    {
        public string Id => "UtilsBench";
        public string Chapter => "Utilities";
        public string WikiSlug => "utilities";
        public string SourcePath => "Content/Utilities/UtilsBench.cs";
        public int DemoItemType => ModContent.ItemType<ToolboxBench>();
        public int SortIndex => 10;
    }
}
