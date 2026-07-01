using InnoVault.PRT;
using InnoVaultExample.Content.ExamplePRTs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Items
{
    // Showcase: Vault Sword Hold Spark (Course PRT-101)
    // Demonstrates: pooled PRT on HoldItem, client-only spawn guard
    // Wiki: https://innovault.wiki/cn/content/prt/
    // Source: InnoVaultExample/Content/Items/VaultSword.cs
    public class VaultSword : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.PurplePhaseblade}";

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
            // Trap · dedServ: skip all PRT spawn when Main.dedServ is true (includes listen-host client).
            if (Main.dedServ) {
                return;
            }

            PRTLoader.NewParticle(
                PRTLoader.GetParticleID<ExamplePRT>(),
                player.Center,
                Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 6,
                Color.White,
                Main.rand.NextFloat(1, 2));
        }
    }
}
