using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InnoVaultExample.Content.ExampleUIs
{
    internal class ExampleUI : UIHandle
    {
        // This property by default points to Vanilla_Mouse_Text, which is the mouse text layer.
        // Its return value determines which update collection the UI will run in. 
        // Don't attempt to change this value during gameplay, it won't have any effect. 
        // The UI's update collection is only allocated once when the game loads.
        public override LayersModeEnum LayersMode => base.LayersMode;

        // Override the texture to point to the custom UI panel texture.
        public override Texture2D Texture => Mod.Assets.Request<Texture2D>("Asset/ExampleUIPanel").Value;

        internal bool _active; // Determines whether the UI is active.
        internal float _sengs; // Controls the UI transition effect.

        // The UI is considered active if `_active` is true or `_sengs` is greater than 0.
        public override bool Active => _active || _sengs > 0;

        // This method updates the UI's position and transition effect.
        public override void Update() {
            // It's important to respect the DrawPosition field and ensure a consistent draw origin.
            DrawPosition = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + new Vector2(0, -600 + 600 * _sengs);
            if (_active) {
                // If active, increase `_sengs` to create a smooth transition.
                if (_sengs < 1) {
                    _sengs += 0.02f;
                }
            }
            else {
                // If not active, decrease `_sengs` to fade the UI out.
                if (_sengs > 0) {
                    _sengs -= 0.02f;
                }
            }
        }

        // This method draws the UI panel with a fade effect based on `_sengs`.
        public override void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Texture, DrawPosition, null, Color.White * _sengs, 0, Vector2.Zero, 2 * _sengs, SpriteEffects.None, 0);
        }
    }
}
