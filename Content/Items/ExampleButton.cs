using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Items
{
    internal class ExampleButton : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.ActuationRod}";

        public override void SetDefaults() {
            Item.width = 40;
            Item.height = 40;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
        }
    }
}
