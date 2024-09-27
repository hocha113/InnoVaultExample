using InnoVault.UIHandles;
using InnoVaultExample.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.ExampleUIs
{
    // This UI is used to demonstrate a basic keystroke function and how to get an external UI instance and interact with it
    internal class ExampleButtonUI : UIHandle
    {
        public override Texture2D Texture => Mod.Assets.Request<Texture2D>("Asset/ExampleButtonUI").Value;
        public override bool Active => player.HeldItem.type == ModContent.ItemType<ExampleButton>();
        // Let's draw the buttons closer to the top
        public override float RenderPriority => 2;
        public override void Update() {
            DrawPosition = new Vector2(Main.screenWidth / 2 - Texture.Width / 2, Main.screenHeight / 2 - 200);
            UIHitBox = new Rectangle((int)DrawPosition.X, (int)DrawPosition.Y, Texture.Width * 2, Texture.Height * 2);
            var mouseTarget = new Rectangle(Main.mouseX, Main.mouseY, 1, 1);

            if (UIHitBox.Contains(mouseTarget)) {
                ExampleUI exampleUI = (ExampleUI)UIHandleLoader.GetUIHandleInstance<ExampleUI>();
                if (keyLeftPressState == KeyPressState.Pressed) {
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    exampleUI._active = !exampleUI._active;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Texture, DrawPosition, null, Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
        }
    }
}
