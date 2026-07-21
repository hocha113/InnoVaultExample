using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Guide
{
    //新角色出生时只塞一本手册。示例物品全部从目录面板里按需领取，
    //初始背包保持干净，玩家也顺便被引导到目录这条主线上。
    //
    //New characters start with just the guide book. Every demo item is claimed on
    //demand from the catalog panel, keeping the starting inventory clean and
    //funneling players toward the catalog itself.
    internal class GuidePlayer : ModPlayer
    {
        //中核死亡重建背包时也重发一本：手册可能随掉落物丢在了尸体那边
        //
        //Also handed out on a mediumcore inventory rebuild: the old copy may be
        //lying in the item drops at the corpse
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
            => [new Item(ModContent.ItemType<GuideBook>())];
    }
}
