using InnoVault.PRT;
using InnoVaultExample.Content.ExamplePRTs;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Items
{
    internal class StarFireSword : ModItem
    {
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
            ExamplePRT_StarFire starFire = (ExamplePRT_StarFire)PRTLoader.NewParticle(
                PRTLoader.GetParticleID<ExamplePRT_StarFire>()// Get our particle ID for generation
                , Main.MouseWorld// Generated at the mouse position
                , Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 6// A random direction
                , Color.White, Main.rand.NextFloat(3.8f, 5.2f));
            starFire.maxLifetime = 60;
            starFire.minLifetime = 30;
        }
    }
}
