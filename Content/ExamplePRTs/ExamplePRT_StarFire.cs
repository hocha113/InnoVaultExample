using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.ExamplePRTs
{
    // Now let's create a more complex particle, which will be even cooler. 
    // This particle will simulate the effect of scattering sparks.
    // It is generated in StarFireSword, so grab that sword and check out the effect.
    internal class ExamplePRT_StarFire : ExamplePRT
    {
        // Override the Texture property to point to the texture resource for the StarFire particle.
        public override string Texture => "InnoVaultExample/Asset/DiffusionCircle";

        // An array of particle colors representing bright, transition, and dark colors.
        public Color[] particleColors;

        // Variables for particle lifetime, speed, scaling, opacity, and more.
        public int lifeTimer;
        public float horizontalSpeed;
        public float speedMultiplier;
        public int maxLifetime;
        public int minLifetime;
        public float scale;
        private float opacity;
        private float remainingLifeTime;

        // Set particle properties.
        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend; // Set draw mode to "Additive Blend."
            SetLifetime = true; // Set the particle to have a limited lifetime.

            // If colors are not initialized, assign values.
            if (particleColors == null) {
                particleColors = new Color[3];
                particleColors[0] = new Color(262, 150, 45, 255); // Bright color.
                particleColors[1] = new Color(186, 35, 24, 255);  // Transition color.
                particleColors[2] = new Color(122, 24, 36, 255);  // Dark color, the final color.
            }

            // Set the minimum and maximum lifetime for the particle.
            minLifetime = minLifetime == 0 ? 90 : minLifetime;
            maxLifetime = maxLifetime == 0 ? 121 : maxLifetime;

            // Randomly generate the particle's lifetime and initialize related properties.
            remainingLifeTime = lifeTimer = Lifetime = Main.rand.Next(minLifetime, maxLifetime);
            lifeTimer = (int)(lifeTimer * Main.rand.NextFloat(0.6f, 1.1f));  // Randomly adjust the initial timer.
            horizontalSpeed = Main.rand.NextFloat(4f, 9f);  // Horizontal speed.
            speedMultiplier = Main.rand.NextFloat(10f, 31f) / 200f;  // Speed multiplier.
            scale = Main.rand.NextFloat(5f, 11f) / 10f;  // Scaling factor.

            if (ai[1] == 1) {
                Lifetime /= 7;  // If AI state is 1, reduce the lifetime.
            }
            maxLifetime = Lifetime;  // Set the maximum lifetime.
        }

        // Particle behavior logic.
        public override void AI() {
            if (ai[0] > 0) {
                ai[0]--;  // AI timer countdown.
                return;
            }

            // Simulate the effect of air flow around the player on the particle.
            if (Main.LocalPlayer.DistanceSQ(Position) < 120 * 120) {
                if (--ai[2] <= 0) {
                    Velocity += Main.LocalPlayer.velocity;  // Add the player's velocity as influence.
                    ai[2] = 30;  // Update every 30 frames.
                }
            }

            // Calculate the particle's opacity based on time. It starts to fade as it reaches half of its lifetime.
            opacity = MathHelper.Lerp(1f, 0f, (maxLifetime / 2f - remainingLifeTime) / (maxLifetime / 2f));

            // Handle speed based on AI state.
            if (ai[1] == 1) {
                return;  // In state 1, do not modify velocity.
            }
            else if (ai[1] == 2) {
                Velocity *= 0.9f;  // In state 2, the speed gradually slows down.
            }
            else {
                if (lifeTimer == 0) {
                    lifeTimer = Main.rand.Next(50, 100);  // Reset timer.
                    horizontalSpeed = Main.rand.NextFloat(4f, 9f);  // Randomize new horizontal speed.
                    speedMultiplier = Main.rand.NextFloat(10f, 31f) / 200f;  // Randomize speed multiplier.
                }

                // Calculate horizontal oscillation for the particle.
                float sineX = (float)Math.Sin(Main.GlobalTimeWrappedHourly * horizontalSpeed);
                Velocity += new Vector2(Main.windSpeedCurrent * (Main.windPhysicsStrength * 3f) * MathHelper.Lerp(1f, 0.1f, Math.Abs(Velocity.X) / 6f), 0f);  // Adjust velocity based on wind strength.
                Velocity += new Vector2(sineX * speedMultiplier, -Main.rand.NextFloat(1f, 2f) / 100f);  // Horizontal oscillation.
                Velocity = new Vector2(MathHelper.Clamp(Velocity.X, -6f, 6f), MathHelper.Clamp(Velocity.Y, -6f, 6f));  // Clamp velocity range.
            }

            lifeTimer--;
            remainingLifeTime--;
        }

        // Draw the particle.
        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D mainTexture = PRTLoader.PRT_IDToTexture[ID];  // Get the main texture.
            Texture2D starTexture = Mod.Assets.Request<Texture2D>("Asset/StarTexture").Value;  // Get the star effect texture.

            // Calculate the particle's color based on the remaining lifetime.
            Color emberColor = Color.Lerp(particleColors[0], particleColors[2], (float)(maxLifetime - remainingLifeTime) / maxLifetime) * opacity;

            // Set the scaling factor.
            float pixelRatio = 1f / 64f;
            Vector2 drawPos = Position - Main.screenPosition;  // Calculate the drawing position of the particle.

            // Draw the main particle.
            spriteBatch.Draw(mainTexture, drawPos, new Rectangle(0, 0, 64, 64), emberColor,
                             Rotation, mainTexture.Size() / 2, pixelRatio * 3f * scale * Scale, SpriteEffects.None, 0f);

            // Draw the star effect (only if AI state is 0).
            if (ai[1] < 1) {
                spriteBatch.Draw(starTexture, drawPos, null, Color, Rotation, starTexture.Size() / 2, Scale * 0.04f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
