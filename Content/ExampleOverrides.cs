using InnoVault.GameSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content
{
    // ItemOverride 可以精准挂到一个已有物品上，不需要继承或改写原来的 ModItem。
    // ItemOverride can attach to an existing item without subclassing or editing the original ModItem.
    internal class ExampleWoodenSwordOverride : ItemOverride
    {
        // 这里选择木剑是因为它稳定存在，进世界也容易拿到。
        // Wooden Sword is used because it always exists and is easy to test in a fresh world.
        public override int TargetID => ItemID.WoodenSword;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            // Tooltip 是最直观的覆写演示：拿起物品就能看到这个类已经生效。
            // Tooltip changes are the easiest proof that this override is active.
            tooltips.Add(new TooltipLine(Mod, "InnoVaultExampleOverride", "InnoVault Override: this line came from ItemOverride."));
            tooltips.Add(new TooltipLine(Mod, "InnoVaultExampleOverrideCN", "InnoVault 覆写示例：这行提示不是写在 ModItem 里的。"));
        }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage) {
            // 小幅增伤能说明战斗钩子已接入，又不会把演示物品变成作弊道具。
            // A small bonus proves combat hooks work without turning the sample into a cheat item.
            damage += 0.15f;
        }

        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            // 背包角标演示绘制钩子；真实项目里可以换成自己的图标纹理。
            // This inventory badge demonstrates the draw hook; a real mod could use its own icon texture.
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            Rectangle mark = new Rectangle((int)position.X + 10, (int)position.Y + 10, 8, 8);
            spriteBatch.Draw(pixel, mark, Color.CornflowerBlue * 0.8f);
        }
    }
}
