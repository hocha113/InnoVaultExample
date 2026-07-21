using InnoVault;
using InnoVault.RenderHandles;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

//RenderHandle 演示：使用脉冲护符后，EndCaptureDraw 阶段对整个屏幕做约两秒半的
//紫色脉冲——把画面拷进 screenSwap 再放大叠回，附带全屏色调，结束后自动归零成本。
//
//The RenderHandle demo: after using the pulse charm, the EndCaptureDraw phase
//runs a violet pulse over the whole screen for about 2.5 seconds - the frame is
//copied into screenSwap, drawn back slightly zoomed, plus a full-screen tint.
//Once the timer runs out the handle costs nothing again.
//https://innovault.wiki/cn/content/render-handle/

namespace InnoVaultExample.Content.Advanced.RenderHandles
{
    internal class ScreenPulseHandle : RenderHandle
    {
        private const int Duration = 150;
        private static int pulseTimer;

        //RenderHandle 整条管线都是客户端 / GPU 专用，专用服务器上连 RT 都不会创建，
        //所以在这里直接拒绝注册
        //
        //The whole RenderHandle pipeline is client / GPU only; a dedicated server
        //never creates the render targets, so refuse to register there outright
        public override bool CanLoad() => !Main.dedServ;

        //Weight 越大越靠后。内置 PRTRender 是 1、Model3DRenderer 是 5，
        //全屏后处理应该压在它们之上，取 10。ScreenSlot 则不重写，保持默认 0：
        //每个私有槽位都是一整块 screenWidth x screenHeight 的显存，本效果用
        //框架代维护的 screenSwap 中转就够了，不必自己再要一块
        //
        //Higher Weight runs later. Built-in PRTRender is 1 and Model3DRenderer
        //is 5; a full-screen post effect belongs on top of both, so 10.
        //ScreenSlot is left at its default of 0: every private slot costs a
        //full screenWidth x screenHeight render target, and the
        //framework-maintained screenSwap is all this effect needs as scratch
        public override float Weight => 10f;

        //只在触发方本地调用（见 PulseCharm.UseItem）。重复触发就把计时器拉满重来
        //
        //Called only on the triggering client (see PulseCharm.UseItem).
        //Re-triggering simply refills the timer
        internal static void Trigger() => pulseTimer = Duration;

        //UpdateBySystem 是逻辑侧心跳（PostUpdateEverything，非服务端）。计时归零后
        //EndCaptureDraw 第一行就提前返回，效果不常驻，闲置时零渲染开销
        //
        //UpdateBySystem is the logic-side heartbeat (PostUpdateEverything, never
        //on a server). Once the timer hits zero, EndCaptureDraw bails on its
        //first line - the effect never idles with a rendering cost
        public override void UpdateBySystem(int index) {
            if (pulseTimer > 0) {
                pulseTimer--;
            }
        }

        //EndCaptureDraw 的契约：主菜单不调用；filterManager / finalTexture /
        //screenTarget1/2 仅在本阶段有效；SpriteBatch 与 RenderTarget 状态全部自管。
        //真正要改画面时写 Main.screenTarget，screenSwap 只当中间缓冲——
        //绝不能采样当前绑定的 RT，所以拷贝与写回必须像下面这样乒乓两步。
        //
        //The EndCaptureDraw contract: skipped on the main menu; filterManager /
        //finalTexture / screenTarget1/2 are only valid during this phase; both
        //SpriteBatch and render target state are fully self-managed. To actually
        //change the picture, write into Main.screenTarget and treat screenSwap
        //as scratch - a bound render target can never be sampled, hence the
        //two-step ping-pong below.
        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (pulseTimer <= 0) {
                return;
            }

            //包络：前 30% 快速冲起，后 70% 缓缓落下，两头都经过缓动所以没有硬切
            //
            //Envelope: a fast rise over the first 30%, then a slow fall; both
            //ends are eased so there is no visible pop
            float progress = 1f - pulseTimer / (float)Duration;
            float wave = progress < 0.3f ? progress / 0.3f : (1f - progress) / 0.7f;
            float strength = VaultUtils.EaseInOutCubic(MathHelper.Clamp(wave, 0f, 1f));

            //第一步：把当前画面原样拷进 screenSwap，留作采样源
            //
            //Step one: copy the current frame into screenSwap to sample from
            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            //第二步：回到 Main.screenTarget，把快照放大一点点半透明叠回（画面
            //向外"呼吸"的回响感），再罩一层紫色调
            //
            //Step two: back on Main.screenTarget, draw the snapshot slightly
            //zoomed and translucent (the outward "breathing" echo), then lay a
            //violet tint over everything
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Vector2 screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
            float zoom = 1f + 0.045f * strength;
            spriteBatch.Draw(screenSwap, screenCenter, null, Color.White * (0.4f * strength)
                , 0f, screenCenter, zoom, SpriteEffects.None, 0f);

            Rectangle fullScreen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, fullScreen, new Rectangle(0, 0, 1, 1)
                , new Color(140, 90, 255) * (0.2f * strength));

            spriteBatch.End();
            //离开前保持 Main.screenTarget 绑定，这正是进入本阶段时的目标，
            //后续实例与原版 EndCapture 拿到的状态才不会错乱
            //
            //Leave with Main.screenTarget bound - that is what was bound when
            //this phase started, so later instances and the vanilla EndCapture
            //see the state they expect
        }

        public override void Unload() => pulseTimer = 0;
    }

    internal class PulseCharm : ModItem
    {
        public override string Texture => "InnoVaultExample/Assets/Emblem/Crest";

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.sellPrice(silver: 50);
        }

        //屏幕后处理是"你的屏幕"的事：只在触发方本地开效果，不同步、不改游戏状态。
        //远程玩家使用护符时它们各自的客户端也会走到这里，靠 myPlayer 判掉
        //
        //Screen post-processing belongs to "your screen": the effect starts only
        //on the triggering client, nothing is synced and no game state changes.
        //When remote players use the charm, their item use still runs here on
        //every client - the myPlayer check filters those out
        public override bool? UseItem(Player player) {
            if (!Main.dedServ && player.whoAmI == Main.myPlayer) {
                ScreenPulseHandle.Trigger();
            }
            return true;
        }
    }

    internal class ScreenPulseEntry : IDemoEntry
    {
        public string Id => "ScreenPulse";
        public string Chapter => "Advanced";
        public string WikiSlug => "content/render-handle";
        public string SourcePath => "Content/Advanced/RenderHandle/ScreenPulse.cs";
        public int DemoItemType => ModContent.ItemType<PulseCharm>();
        public int SortIndex => 20;
    }
}
