using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Training Torch Turret Kit (WORLD-301)
    // Demonstrates: targeting utility sample on academy ModTile
    // Wiki: https://innovault.wiki/cn/utilities/targeting/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingTurretItem.cs

    public class AcademyTrainingTurretItem : ModItem
    {
        public override string Texture => "Terraria/Images/Item_8";

        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 20;
            Item.maxStack = 99;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 10;
            Item.useAnimation = 15;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<AcademyTrainingTurretTile>();
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(silver: 8);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Torch, 8)
                .AddIngredient(ItemID.IronBar, 4)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
