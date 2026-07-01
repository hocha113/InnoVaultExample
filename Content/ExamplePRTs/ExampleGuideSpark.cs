using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InnoVaultExample.Content.ExamplePRTs
{
    // 一个短寿命的拖尾粒子，用来演示 PRT 池化和历史位置缓存。
    // A short-lived trail particle showing PRT pooling and position history caches.
    internal class ExampleGuideSpark : BasePRT
    {
        private float fadePower;

        // 高频粒子可以开启对象池；开启后别忘了在 Reset 里清理自己的字段。
        // High-frequency particles can use pooling; remember to clear custom fields in Reset.
        public override bool CanPool => true;

        public override void SetProperty() {
            // SetProperty 每次生成都会调用，包括从池里取出的旧实例。
            // SetProperty runs for every spawn, including reused instances from the pool.
            Lifetime = Main.rand.Next(36, 58);
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            RenderLayer = PRTRenderLayer.AfterPlayers;
            fadePower = Main.rand.NextFloat(0.85f, 1.2f);
            // 缓存数组不能放构造函数里；池化实例被 Reset 后需要重新初始化。
            // Cache arrays should not live in the constructor; pooled instances need them rebuilt after Reset.
            InitializeCaches(8);
        }

        public override void AI() {
            // oldPositions 不是自动更新的，需要粒子自己决定何时记录轨迹。
            // oldPositions is not updated automatically; the particle decides when to record its trail.
            UpdatePositionCache(oldPositions.Length);
            Velocity *= 0.94f;
            Rotation += Velocity.X * 0.03f;
            Color = Color.Lerp(Color, Color.White, 0.04f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            // TexValue 来自 PRTLoader，找不到本地贴图时会落到 InnoVault 的占位纹理。
            // TexValue comes from PRTLoader; if the local texture is missing, InnoVault uses a placeholder.
            Texture2D texture = TexValue;
            float alpha = MathF.Pow(1f - LifetimeCompletion, fadePower);

            for (int i = 0; i < oldPositions.Length; i++) {
                float progress = i / (float)(oldPositions.Length - 1);
                Vector2 drawPos = oldPositions[i] - Main.screenPosition;
                spriteBatch.Draw(texture, drawPos, null, Color.CornflowerBlue * alpha * progress, Rotation, texture.Size() / 2f, Scale * progress, SpriteEffects.None, 0f);
            }

            return false;
        }

        public override void Reset() {
            // base.Reset 会清掉 BasePRT 的字段；子类字段要自己补上。
            // base.Reset clears BasePRT fields; subclass fields are still this class's job.
            base.Reset();
            fadePower = 0f;
        }
    }
}
