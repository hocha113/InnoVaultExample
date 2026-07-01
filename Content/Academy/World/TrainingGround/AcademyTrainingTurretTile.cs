using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Training Turret Tile (WORLD-301)
    // Wiki: https://innovault.wiki/cn/content/tile-processor/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingTurretTile.cs

    public class AcademyTrainingTurretTile : ModTile
    {
        public override string Texture => "Terraria/Images/Tiles_4";

        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileLighted[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;

            AddMapEntry(new Color(255, 120, 60), Language.GetOrRegister("Mods.InnoVaultExample.Tiles.AcademyTrainingTurret.MapEntry"));
            DustType = DustID.Torch;
            AdjTiles = [TileID.Torches];
        }

        public override void NumDust(int i, int j, bool fail, ref int num) {
            num = fail ? 0 : 2;
        }
    }
}
