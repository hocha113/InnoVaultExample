using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InnoVaultExample.Content.Particles
{
    //进阶写法：贴图不放在类旁边而是显式指路径，绘制不走默认管线而是 PreDraw 返回 false
    //自己画——同一张星形贴图叠三层不同缩放和亮度，加法混合把它们糊成一团辉光。
    //
    //The advanced shape of a PRT: the texture lives elsewhere so we point at it
    //explicitly, and instead of the default pipeline PreDraw returns false and
    //draws by hand — the same star texture stacked in three scales and
    //brightnesses, fused into a glow by additive blending.
    //
    //https://innovault.wiki/cn/content/prt/
    internal class StarBurst : BasePRT
    {
        //显式路径要带模组名段，格式和 ModContent.Request 一致
        //
        //An explicit path carries the mod-name segment, same format as
        //ModContent.Request.
        public override string Texture => "InnoVaultExample/Assets/Effects/StarTexture";

        //每颗爆星自己的闪烁相位，免得满屏星星同频呼吸像跳广播体操。
        //粒子实例是逐颗新建的（本类型未启用对象池），字段不用清理；
        //如果哪天重写 CanPool => true，就得在 Reset 里把它归零。
        //
        //A per-instance flicker phase keeps a screen full of stars from
        //breathing in lockstep. Instances are freshly created (no pooling for
        //this type) so the field needs no cleanup; if CanPool were ever
        //overridden to true, Reset would have to zero it.
        private float flickerPhase;

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            Lifetime = Main.rand.Next(36, 60);
            flickerPhase = Main.rand.NextFloat(MathHelper.TwoPi);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI() {
            Velocity *= 0.9f;
            Rotation += 0.03f;

            //Opacity 是基类自带的通用渐变字段：前 20% 寿命撑开，剩下的慢慢咽气
            //
            //Opacity is the base class's general-purpose fade field: pop open in
            //the first 20% of the lifetime, then die down over the rest.
            float t = LifetimeCompletion;
            Opacity = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f;
        }

        //返回 false 表示默认绘制完全不跑，画面上出现什么全由这里说了算。
        //进到这里时 SpriteBatch 已经按 PRTDrawMode 分好桶、用加法混合开好批次，直接 Draw 即可。
        //
        //Returning false skips the default draw entirely — whatever appears on
        //screen is decided here. The SpriteBatch has already been bucketed by
        //PRTDrawMode and begun with additive blending, so just Draw.
        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = TexValue;
            Vector2 drawPos = Position - Main.screenPosition;
            Vector2 origin = tex.Size() / 2f;

            //闪烁系数三层共用，同一帧内层与层不能错拍，不然看起来像描边分家
            //
            //One flicker factor shared by all three layers: within a frame the
            //layers must pulse together or the halo visibly detaches.
            float flicker = 1f + 0.15f * MathF.Sin(flickerPhase + Time * 0.35f);

            //原图 326 像素见方，基准缩放压到 0.1 量级才是正常粒子尺寸
            //
            //The source image is 326px square; a base scale around 0.1 brings it
            //down to sane particle size.
            float baseScale = Scale * 0.12f * flicker;

            //从大到小、从暗到亮：外圈大而暗铺出光晕，中层撑主体，
            //核心一小点纯白，加法混合会把三层叠成中心白热、边缘渐散的辉光
            //
            //Big-and-dim to small-and-bright: the outer layer lays down the
            //halo, the middle carries the body, and a small white core tops it
            //off. Additive blending fuses them into a white-hot center that
            //falls off at the edges.
            spriteBatch.Draw(tex, drawPos, null, Color * (0.25f * Opacity), Rotation, origin, baseScale * 2.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(tex, drawPos, null, Color * (0.7f * Opacity), Rotation, origin, baseScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(tex, drawPos, null, Color.White * (0.9f * Opacity), -Rotation * 0.5f, origin, baseScale * 0.45f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
