using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.Overrides
{
    // Showcase: Override Lens (OVERRIDE-401)
    // Demonstrates: opt-in switch for broad Player/Tile/Scene/Menu/Dust overrides
    // Wiki: https://innovault.wiki/cn/overrides/
    // Source: InnoVaultExample/Content/Academy/Overrides/AcademyOverrideLens.cs

    public class AcademyOverrideLens : ModItem
    {
        public override string Texture => "Terraria/Images/Item_1299";

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.MenuTick;
            Item.value = Item.buyPrice(silver: 20);
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                player.GetModPlayer<AcademyOverrideGalleryPlayer>().ToggleGallery();
            }
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Lens)
                .AddIngredient(ItemID.FallenStar)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
