using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InnoVaultExample.Content.ExampleUIs
{
    internal class ExampleGlobalUI : GlobalUIHandle
    {
        public override void PostUIHanderElementUpdate(UIHandle handle) {
            if (handle.ID == UIHandleLoader.GetUIHandleID<ExampleUI>()) {
                ExampleUI exampleUI = (ExampleUI)handle;
                if (exampleUI.Active) {
                    Main.spriteBatch.Draw(Mod.Assets.Request<Texture2D>("Asset/ExampleButtonUI").Value
                        , exampleUI.DrawPosition, null, Color.White * exampleUI._sengs
                    , 0, Vector2.Zero, 2 * exampleUI._sengs, SpriteEffects.None, 0);
                }
            }
        }
    }
}
