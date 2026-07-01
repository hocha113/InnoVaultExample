using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Academy Supply Locker Kit (WORLD-301)
    // Demonstrates: Storages API entry tile, TP-backed inventory
    // Wiki: https://innovault.wiki/cn/advanced/storages/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingLockerItem.cs

    public class AcademyTrainingLockerItem : ModItem
    {
        public override string Texture => "Terraria/Images/Item_48";

        public override void SetDefaults() {
            Item.width = 26;
            Item.height = 26;
            Item.maxStack = 99;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 10;
            Item.useAnimation = 15;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<AcademyTrainingLockerTile>();
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(silver: 10);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.IronBar, 6)
                .AddIngredient(ItemID.Wood, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
