using InnoVault.TileProcessors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace InnoVaultExample.Content.ExampleTileProcessors
{
    // We recommend adding the TP suffix to any subclass of TileProcessor.
    // This TP entity will be associated with the vanilla Wooden Workbench.
    // Unlike TileEntity, we don't need to write any code in the Tile itself because TileProcessor is quite independent.
    internal class ExampleWorkbenchTP : TileProcessor
    {
        // Override this property to point to TileID.WorkBenches, which is the tile ID for the vanilla workbench.
        // This will automatically generate and attach this TP entity when the player places a workbench.
        // All existing workbenches in the world will also automatically get this type of TP entity attached to them.
        public override int TargetTileID => TileID.WorkBenches;

        private Vector2 Center => PosInWorld + new Vector2(16, 8);
        private Color tpColor = Color.White;
        private float light;

        // This function is just to demonstrate the purpose of the overridable function.
        // It will be called once when the TP entity is placed or generated during world initialization.
        // We can use this to set some values.
        public override void SetProperty() {
            tpColor = new Color(Main.rand.NextFloat(), Main.rand.NextFloat(), Main.rand.NextFloat());
            light = 1 + Main.rand.NextFloat();
        }

        // This update function runs on both the client and server and is called every frame.
        public override void Update() {
            // For example, we let this TP entity emit light with varying colors and intensities during updates.
            // You can choose to do anything here. The only thing to note is that actions like spawning Projectiles or NPCs must only run on the server in multiplayer mode.
            Lighting.AddLight(Center, tpColor.ToVector3() * light);
        }

        // This draw function will allow you to draw something interesting on the tile.
        // Here, we make the workbench display the player's held item as if it were an exhibition.
        public override void Draw(SpriteBatch spriteBatch) {
            int itemType = (Main.mouseItem.IsAir ? Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem] : Main.mouseItem).type;
            Vector2 orig = default;
            if (itemType > ItemID.None) {
                Main.instance.LoadItem(itemType);
                Texture2D texture = TextureAssets.Item[itemType].Value;
                Rectangle? rectangle = Main.itemAnimations[itemType] != null
                    ? Main.itemAnimations[itemType].GetFrame(texture) : texture.Frame(1, 1, 0, 0);
                orig = texture.Size() / 2;
                if (rectangle.HasValue) {
                    orig = rectangle.Value.Size() / 2;
                }
                float gfkY = -16 - rectangle.Value.Height / 2 + MathF.Sin(Main.GameUpdateCount * 0.1f) * 4;
                Vector2 drawPos = Center + new Vector2(0, gfkY);
                drawPos -= Main.screenPosition;
                spriteBatch.Draw(texture, drawPos, rectangle, Color.White, 0, orig, 1, SpriteEffects.None, 0);
            }
        }
    }
}
