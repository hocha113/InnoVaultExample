using InnoVault.PRT;
using InnoVaultExample.Content.ExamplePRTs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Items
{ 
	public class VaultSword : ModItem
	{
		public override void SetDefaults()
		{
			Item.damage = 50;
			Item.DamageType = DamageClass.Melee;
			Item.width = 40;
			Item.height = 40;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 6;
			Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
		}

        public override void HoldItem(Player player) {
			PRTLoader.NewParticle(
				PRTLoader.GetParticleID<ExamplePRT>()// Get our particle ID for generation
                , player.Center// Generated on the player's position
                , Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 6// A random direction
                , Color.White, Main.rand.NextFloat(1, 2));
        }
    }
}
