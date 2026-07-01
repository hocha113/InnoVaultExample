using InnoVault.GameSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.Weapons.DrillGun
{
    // Showcase: Academy Drill Gun (COMBAT-201)
    // Demonstrates: ModItem held spawn, ItemOverride combat hooks, BaseHeldProj entry point
    // Wiki: https://innovault.wiki/cn/content/base-held-proj/
    // Wiki: https://innovault.wiki/cn/overrides/item/
    // Source: InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillGun.cs

    /// <summary>
    /// Course COMBAT-201 weapon. Shoot spawns <see cref="AcademyDrillHeld"/> instead of a vanilla projectile.
    /// </summary>
    public class AcademyDrillGun : ModItem
    {
        internal static int TypeId => ModContent.ItemType<AcademyDrillGun>();

        // Placeholder art: Minishark sprite until bundled academy PNG lands.
        public override string Texture => "Terraria/Images/Item_98";

        public override void SetDefaults() {
            Item.damage = 19;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 40;
            Item.height = 24;
            Item.useTime = Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2.5f;
            Item.value = Item.buyPrice(silver: 75);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item41;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AcademyDrillHeld>();
            Item.shootSpeed = 12f;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.IronBar, 12)
                .AddIngredient(ItemID.Wood, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }

        // One held instance per player; repeated use ticks refresh the held projectile instead of stacking.
        public override bool CanUseItem(Player player)
            => player.ownedProjectileCounts[ModContent.ProjectileType<AcademyDrillHeld>()] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            int heldType = ModContent.ProjectileType<AcademyDrillHeld>();
            if (player.ownedProjectileCounts[heldType] <= 0) {
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, heldType, damage, knockback, player.whoAmI);
            }
            return false;
        }
    }

    /// <summary>
    /// ItemOverride layer: same item type, extra combat/tooltip hooks without subclassing the ModItem again.
    /// </summary>
    internal class AcademyDrillGunOverride : ItemOverride
    {
        public override int TargetID => AcademyDrillGun.TypeId;

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage) {
            // Small training bonus so the override is visible on the target dummy, not a cheat tier boost.
            if (player.HasBuff(BuffID.WellFed)) {
                damage += 0.05f;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            tooltips.Add(new TooltipLine(Mod, "AcademyDrillCourseTag", "[COMBAT-201]"));
        }

        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle mark = new((int)position.X + 2, (int)position.Y + 2, 6, 6);
            spriteBatch.Draw(pixel, mark, new Color(120, 200, 255) * 0.85f);
        }
    }
}
