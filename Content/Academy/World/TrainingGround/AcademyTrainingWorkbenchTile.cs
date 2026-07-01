using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Training Workbench Tile (WORLD-301)
    // Demonstrates: ModTile + TileObjectData, TP auto-mount via TargetTileID
    // Wiki: https://innovault.wiki/cn/content/tile-processor/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingWorkbenchTile.cs

    public class AcademyTrainingWorkbenchTile : ModTile
    {
        public override string Texture => "Terraria/Images/Tiles_18";

        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;

            AddMapEntry(new Color(168, 132, 92), Language.GetOrRegister("Mods.InnoVaultExample.Tiles.AcademyTrainingWorkbench.MapEntry"));
            DustType = DustID.WoodFurniture;
            AdjTiles = [TileID.WorkBenches];
        }
    }
}
