using InnoVault;
using InnoVault.Actors;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace InnoVaultExample.Content.Academy.Actors
{
    // Showcase: Academy Beacon Actor (WORLD-301)
    // Demonstrates: SyncVar authority, server lifetime, client PreDraw with dedServ guard
    // Wiki: https://innovault.wiki/cn/persistence/actor/
    // Wiki: https://innovault.wiki/cn/persistence/sync-var/
    // Source: InnoVaultExample/Content/Academy/Actors/AcademyBeaconActor.cs

    internal class AcademyBeaconActor : Actor
    {
        [SyncVar]
        private int lifetime;

        [SyncVar]
        private Color beaconColor;

        public int Lifetime => lifetime;

        public override void OnSpawn(params object[] args) {
            Width = 40;
            Height = 40;
            DrawLayer = ActorDrawLayer.AfterPlayers;

            // Trap · SyncVar authority: seed synced fields only on HasAuthority (SP is not isServer).
            if (AcademyAuthority.HasAuthority()) {
                lifetime = 420;
                beaconColor = Color.Lerp(Color.Gold, Color.DeepSkyBlue, Main.rand.NextFloat(0.2f, 0.5f));
            }
        }

        public override void AI() {
            if (AcademyAuthority.HasAuthority()) {
                // Trap · SyncVar authority: lifetime decrements only on authority peer.
                lifetime--;
                if (lifetime <= 0) {
                    Active = false;
                    NetUpdate = true;
                }
            }

            Velocity *= 0.97f;
            Velocity.Y += MathF.Sin(Main.GameUpdateCount * 0.07f + WhoAmI) * 0.018f;
            Rotation += 0.035f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (Main.dedServ) {
                return false;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float progress = MathHelper.Clamp(lifetime / 420f, 0f, 1f);
            float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4.5f + WhoAmI) * 0.1f;
            Vector2 center = Center - Main.screenPosition;
            Rectangle core = new Rectangle((int)(center.X - 12f * pulse), (int)(center.Y - 12f * pulse), (int)(24f * pulse), (int)(24f * pulse));

            spriteBatch.Draw(pixel, core, null, beaconColor * (0.4f + progress * 0.5f), Rotation, Vector2.Zero, SpriteEffects.None, 0f);
            Utils.DrawBorderString(spriteBatch, "Beacon", core.TopLeft() + new Vector2(-4f, -20f), Color.White * progress, 0.75f);
            return false;
        }
    }
}
