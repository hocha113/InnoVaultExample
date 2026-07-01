using InnoVault;
using InnoVault.Actors;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace InnoVaultExample.Content
{
    // Showcase: Guide Orb Actor (Legacy)
    // Demonstrates: SyncVar lifetime; superseded by AcademyBeaconActor for manual spawn
    // Wiki: https://innovault.wiki/cn/persistence/actor/
    // Source: InnoVaultExample/Content/ExampleGuideOrb.cs
    internal class ExampleGuideOrb : Actor
    {
        [SyncVar]
        private int lifetime;

        [SyncVar]
        private Color orbColor;

        public override void OnSpawn(params object[] args) {
            Width = 36;
            Height = 36;
            DrawLayer = ActorDrawLayer.AfterPlayers;

            // Trap · SyncVar authority: mutate only when AcademyAuthority.HasAuthority().
            if (AcademyAuthority.HasAuthority()) {
                lifetime = 300;
                orbColor = Color.Lerp(Color.CornflowerBlue, Color.White, Main.rand.NextFloat(0.35f));
            }
        }

        public override void AI() {
            // Motion runs on every peer for smooth drift; synced fields stay authority-side.
            Velocity *= 0.96f;
            Velocity.Y += MathF.Sin(Main.GameUpdateCount * 0.08f + WhoAmI) * 0.015f;
            Rotation += 0.05f;

            if (AcademyAuthority.HasAuthority()) {
                lifetime--;
                if (lifetime <= 0) {
                    Active = false;
                    NetUpdate = true;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float progress = MathHelper.Clamp(lifetime / 300f, 0f, 1f);
            float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.08f;
            Rectangle rect = new Rectangle(
                (int)(Center.X - Main.screenPosition.X - 14f * pulse),
                (int)(Center.Y - Main.screenPosition.Y - 14f * pulse),
                (int)(28f * pulse),
                (int)(28f * pulse));

            spriteBatch.Draw(pixel, rect, null, orbColor * (0.35f + progress * 0.45f), Rotation, Vector2.Zero, SpriteEffects.None, 0f);
            Utils.DrawBorderString(spriteBatch, "Actor", rect.TopLeft() + new Vector2(-6f, -22f), Color.White * progress, 0.72f);
            return false;
        }
    }
}
