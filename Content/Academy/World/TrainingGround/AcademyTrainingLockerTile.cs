using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Academy Supply Locker Tile (WORLD-301)
    // Wiki: https://innovault.wiki/cn/advanced/storages/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingLockerTile.cs

    public class AcademyTrainingLockerTile : ModTile
    {
        public override string Texture => "Terraria/Images/Tiles_21";

        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(120, 140, 180), Language.GetOrRegister("Mods.InnoVaultExample.Tiles.AcademyTrainingLocker.MapEntry"));
            DustType = DustID.Iron;
        }
    }
}
