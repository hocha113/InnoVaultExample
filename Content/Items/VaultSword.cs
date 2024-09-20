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
				PRTLoader.GetParticleID<ExamplePRT>()//获取我们的粒子ID，用于生成
				, player.Center//在玩家位置上生成
				, Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 6//一个随机的方向
				, Color.White, Main.rand.NextFloat(1, 2));
        }
    }
}
