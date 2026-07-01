using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace InnoVaultExample.Content.ExampleUIs
{
    internal class ExampleGlobalUI : GlobalUIHandle
    {
        public override void PostUIHanderElementUpdate(UIHandle handle) {
            if (handle.ID == UIHandleLoader.GetUIHandleID<ExampleUI>()) {
                ExampleUI exampleUI = (ExampleUI)handle;
                if (exampleUI.Active) {
                    Rectangle badge = new Rectangle((int)exampleUI.DrawPosition.X + 12, (int)exampleUI.DrawPosition.Y + 78, 86, 20);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, badge, Color.CornflowerBlue * exampleUI._sengs);
                }
            }
        }
    }
}
