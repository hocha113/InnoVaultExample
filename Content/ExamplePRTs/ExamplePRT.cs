using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InnoVaultExample.Content.ExamplePRTs
{
    // Our first PRT particle, pretty cool right? It is generated in VaultSword, 
    // so grab the sword and check out the effect.
    internal class ExamplePRT : BasePRT
    {
        // The Texture property doesn't need to be overridden, as BasePRT has an automatic loading mechanism.
        // It automatically loads a .png file with the same name in the same directory.
        // This is similar to how ModProjectile works.
        // So, let's prepare a .png file called "ExamplePRT", which is an image with the same name as the class.
        // public override string Texture => base.Texture;

        // Override this function, it will be called once when the particle is generated.
        // PRT entities are independent instances, so the settings in this function
        // can also be applied to each instance individually, similar to ModProjectile.SetDefaults.
        public override void SetProperty() {
            // PRTDrawMode determines which rendering mode the instance will be batched into.
            // This sets the color blending mode for the particle's rendering.
            // Here, we set it to additive blending mode. The effect brought by this field is real-time,
            // and it will batch all PRT instances in each draw call.
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            SetLifetime = true; // Set this to true, and the particle will be subject to a lifetime limit.
            Lifetime = Main.rand.Next(220, 360); // Lifetime of 220 to 360 ticks.
        }

        public override void AI() {
            // A simple AI that randomly changes direction.
            if (--ai[0] <= 0) {
                Velocity = Velocity.RotatedByRandom(0.3f);
                ai[0] = Main.rand.Next(10);
            }
            // Adjust the rotation according to the movement direction.
            Rotation += Velocity.X * 0.02f;

            //// Relative position change
            // Position += Main.LocalPlayer.velocity;

            // A fun brightness transformation.
            Color.A = (byte)(1 - LifetimeCompletion * 255f);
            // Apply a fading effect near the end of its life.
            if (LifetimeCompletion > 0.8f) {
                Color *= 0.9f;
            }
        }

        // Override this drawing function. If you want to customize the drawing, return false here,
        // and the default drawing will not be applied.
        public override bool PreDraw(SpriteBatch spriteBatch) => true;
    }
}
