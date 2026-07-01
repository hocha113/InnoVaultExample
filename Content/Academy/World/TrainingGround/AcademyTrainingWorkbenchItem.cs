using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Training Workbench Kit (WORLD-301)
    // Demonstrates: placeable ModItem, TileProcessor mount on custom ModTile
    // Wiki: https://innovault.wiki/cn/content/tile-processor/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingWorkbenchItem.cs

    public class AcademyTrainingWorkbenchItem : ModItem
    {
        public override string Texture => "Terraria/Images/Item_32";

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 20;
            Item.maxStack = 99;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 10;
            Item.useAnimation = 15;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<AcademyTrainingWorkbenchTile>();
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(silver: 5);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 12)
                .AddIngredient(ItemID.IronBar, 2)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
