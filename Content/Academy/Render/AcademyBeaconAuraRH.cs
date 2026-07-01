using InnoVault.Actors;
using InnoVault.RenderHandles;
using InnoVaultExample.Content.Academy.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace InnoVaultExample.Content.Academy.Render
{
    // Showcase: Beacon Aura RenderHandle (WORLD-301)
    // Demonstrates: DrawAfterPlayers pass, Actor iteration, dedServ guard
    // Wiki: https://innovault.wiki/cn/content/render-handle/
    // Source: InnoVaultExample/Content/Academy/Render/AcademyBeaconAuraRH.cs

    internal class AcademyBeaconAuraRH : RenderHandle
    {
        public override float Weight => 2f;

        public override void DrawAfterPlayers(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (Main.dedServ || ActorLoader.Actors == null) {
                return;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            foreach (Actor actor in ActorLoader.Actors) {
                if (actor is not AcademyBeaconActor { Active: true } beacon) {
                    continue;
                }

                float progress = MathHelper.Clamp(beacon.Lifetime / 420f, 0f, 1f);
                float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + beacon.WhoAmI * 0.4f) * 0.15f;
                Vector2 center = beacon.Center - Main.screenPosition;
                int size = (int)(36f * pulse);
                Rectangle aura = new Rectangle((int)(center.X - size * 0.5f), (int)(center.Y - size * 0.5f), size, size);
                spriteBatch.Draw(pixel, aura, null, Color.DeepSkyBlue * (0.08f + progress * 0.12f));
            }

            spriteBatch.End();
        }
    }
}
