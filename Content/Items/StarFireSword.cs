using InnoVault.PRT;
using InnoVaultExample.Content.ExamplePRTs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Items
{
    // Showcase: Star Fire Sword Spark (Course PRT-102)
    // Demonstrates: custom PRT type, lifetime fields, dedServ guard
    // Wiki: https://innovault.wiki/cn/content/prt/
    // Source: InnoVaultExample/Content/Items/StarFireSword.cs
    internal class StarFireSword : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.EnchantedSword}";

        public override void SetDefaults() {
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

            ExamplePRT_StarFire starFire = (ExamplePRT_StarFire)PRTLoader.NewParticle(
                PRTLoader.GetParticleID<ExamplePRT_StarFire>(),
                Main.MouseWorld,
                Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 6,
                Color.White,
                Main.rand.NextFloat(3.8f, 5.2f));
            starFire.maxLifetime = 60;
            starFire.minLifetime = 30;
        }
    }
}
