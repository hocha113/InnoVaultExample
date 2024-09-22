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
    //这个UI用于演示一个基本的按键功能，并演示如何获取外部的UI实例并进行交互
    internal class ExampleButtonUI : UIHandle
    {
        public override Texture2D Texture => Mod.Assets.Request<Texture2D>("Asset/ExampleButtonUI").Value;
        public override bool Active => player.HeldItem.type == ModContent.ItemType<ExampleButton>();
        //我们让按钮的绘制层级往上层靠近
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
