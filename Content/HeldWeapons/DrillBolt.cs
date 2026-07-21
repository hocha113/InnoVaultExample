using InnoVault;
using InnoVault.Trails;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.HeldWeapons
{
    //钻头枪射出的弹体，演示 InnoVault 的 Trail 网格拖尾：位置历史来自原版的
    //Projectile.oldPos，网格颜色完全由顶点色渐变提供，不需要任何拖尾贴图，
    //也不需要模组自带编译好的着色器——一个参数已知的 BasicEffect 就够了。
    //
    //The bolt fired by the drill gun, demonstrating InnoVault's Trail mesh:
    //position history comes from vanilla Projectile.oldPos, and the ribbon is
    //colored purely by vertex-color gradients — no trail texture and no
    //bundled compiled shader needed; a BasicEffect with known parameters does
    //the job.
    //
    //https://innovault.wiki/cn/content/trails/
    internal class DrillBolt : ModProjectile
    {
        private const int TrailPoints = 16;

        public override string Texture => "Terraria/Images/Projectile_14";

        //Trail 持有 GPU 顶点/索引缓冲，属于要显式管理生命周期的对象；
        //points 数组构造后长度不能变，Trail 会校验它必须等于 maxPointCount
        //
        //Trail owns GPU vertex/index buffers, so its lifetime is managed
        //explicitly; the points array keeps a fixed length after construction —
        //Trail validates it against maxPointCount.
        private Trail trail;
        private Vector2[] points;

        //共享的绘制用 BasicEffect：所有钻头镖复用一个，卸载时统一释放
        //
        //The shared BasicEffect for drawing: all bolts reuse one instance,
        //released together at unload.
        private static BasicEffect trailEffect;

        public override void SetStaticDefaults() {
            //TrailingMode 2 让原版每帧记录位置与旋转，TrailCacheLength 决定
            //oldPos 的容量——也就是拖尾网格的点数
            //
            //TrailingMode 2 makes vanilla record position and rotation every
            //frame; TrailCacheLength sizes oldPos — which is the ribbon's
            //point count.
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = TrailPoints;
        }

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 75;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.25f, 0.5f, 0.7f);
        }

        //宽度与颜色委托：progress 从 0（尾端）走到 1（尖端），让光带朝尾巴收窄、
        //变暗。加法混合下透明度就是亮度，所以颜色直接乘系数即可
        //
        //Width and color delegates: progress runs 0 (tail) to 1 (tip), so the
        //ribbon narrows and dims toward the tail. Under additive blending
        //brightness is opacity, so multiplying the color is enough.
        private float TrailWidth(float progress) => MathHelper.Lerp(1.5f, 7f, progress) * Projectile.scale;

        private Color TrailColor(Vector2 uv) => new Color(120, 210, 255) * (0.1f + 0.9f * uv.X * uv.X);

        public override bool PreDraw(ref Color lightColor) {
            //绘制钩子只在图形客户端运行，这里不用再守卫 dedServ；真正要守卫的是
            //逻辑侧的生成与音效（见 DrillHeld）
            //
            //Draw hooks only run on graphical clients, so no dedServ guard is
            //needed here; the guards that matter sit on the logic side (see
            //DrillHeld).
            DrawTrail();

            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, new Color(170, 230, 255),
                Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        private void DrawTrail() {
            if (Projectile.oldPos == null || Projectile.oldPos.Length < 2) {
                return;
            }

            //oldPos[0] 是最新采样，而 Trail 约定 TrailPositions[^1] 是尖端，
            //填充时要倒过来；未写入的零向量退回当前位置，避免开场从原点拉线
            //
            //oldPos[0] holds the newest sample while Trail expects
            //TrailPositions[^1] to be the tip, so fill in reverse; zero vectors
            //not yet written fall back to the current position, or the ribbon
            //would streak in from the world origin on spawn.
            int count = Projectile.oldPos.Length;
            points ??= new Vector2[count];
            for (int i = 0; i < count; i++) {
                Vector2 sample = Projectile.oldPos[i];
                if (sample == Vector2.Zero) {
                    sample = Projectile.position;
                }
                points[count - 1 - i] = sample + Projectile.Size * 0.5f;
            }

            //Trail 只构造一次：maxPointCount 定死后每帧重赋同长度的位置数组即可。
            //内部的 GPU 缓冲在主线程异步建立，首帧可能静默跳过绘制，属正常现象
            //
            //Construct the Trail once: with maxPointCount fixed, just reassign
            //a same-length position array per frame. Its GPU buffers are
            //created via a main-thread queue, so the very first frame may
            //silently skip drawing — that's expected.
            trail ??= new Trail(points, TrailWidth, TrailColor);
            trail.TrailPositions = points;

            //DrawTrail 需要一个 Effect。顶点色方案用 BasicEffect 即可：打开
            //VertexColorEnabled、关掉贴图，再喂一个"世界坐标 → 屏幕"的矩阵
            //（GetTransfromMatrix 含 -screenPosition 平移、缩放与正交投影）
            //
            //DrawTrail wants an Effect. For vertex colors a BasicEffect
            //suffices: VertexColorEnabled on, texturing off, fed with a
            //world-to-screen matrix (GetTransfromMatrix bundles the
            //-screenPosition translation, zoom and orthographic projection).
            GraphicsDevice device = Main.instance.GraphicsDevice;
            trailEffect ??= new BasicEffect(device) {
                VertexColorEnabled = true,
                TextureEnabled = false,
            };
            trailEffect.World = VaultUtils.GetTransfromMatrix();

            device.RasterizerState = RasterizerState.CullNone;
            device.BlendState = BlendState.Additive;
            trail.DrawTrail(trailEffect);
            device.BlendState = BlendState.AlphaBlend;
        }

        //Dispose 时机：弹幕消亡（命中、穿透耗尽、超时）时释放本实例的 GPU 缓冲。
        //OnKill 在服务端也会跑，但那里从没进过 PreDraw，trail 保持 null，
        //空传播安全跳过。忘记释放不会立刻出事，堆积起来就是显存泄漏
        //
        //Dispose timing: free this instance's GPU buffers when the projectile
        //dies (hit, out of penetration, timeout). OnKill also runs on the
        //server, but PreDraw never ran there, so trail stays null and the
        //null-conditional skips safely. A missed Dispose won't hurt at once —
        //accumulated, it's a VRAM leak.
        public override void OnKill(int timeLeft) {
            trail?.Dispose();
            trail = null;

            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric,
                    Main.rand.NextVector2Circular(3f, 3f), 100, default, 0.7f);
                dust.noGravity = true;
            }
        }

        //静态的 BasicEffect 在模组卸载时释放；GPU 资源的销毁排回主线程执行
        //
        //The static BasicEffect is released at mod unload; GPU teardown is
        //queued back onto the main thread.
        public override void Unload() {
            BasicEffect fx = trailEffect;
            trailEffect = null;
            if (fx != null) {
                Main.QueueMainThreadAction(fx.Dispose);
            }
        }
    }
}
