using InnoVaultExample.Content.Items;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content
{
    internal class VEPlayer : ModPlayer
    {
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath) {
            yield return new Item(ModContent.ItemType<ExampleButton>());
            yield return new Item(ModContent.ItemType<StarFireSword>());
            yield return new Item(ModContent.ItemType<VaultSword>());
        }
    }
}
