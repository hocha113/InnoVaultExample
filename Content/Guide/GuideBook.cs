using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Guide
{
    //随身手册，也是整个示例模组的入口物品：使用它开合示例目录面板。
    //物品本身刻意保持无聊——所有值得看的代码都在 GuideCatalogUI 里。
    //
    //The pocket guide and the entry point of this example mod: using it toggles
    //the demo catalog panel. The item itself is intentionally boring - everything
    //worth reading lives in GuideCatalogUI.
    //https://innovault.wiki/cn/content/ui-handle/
    internal class GuideBook : ModItem
    {
        //引导物品不值得单独画一张贴图，直接借原版书本（ItemID.Book = 149）
        //
        //Not worth its own sprite; borrow the vanilla Book texture (ItemID.Book = 149)
        public override string Texture => $"Terraria/Images/Item_{ItemID.Book}";

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 28;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Blue;
        }

        public override bool? UseItem(Player player) {
            //UseItem 在多人模式下会在每个客户端和服务器上各跑一遍，
            //而 UI 只属于本地玩家——不加判断的话别人用书会把你的面板打开
            //
            //In multiplayer UseItem runs on every client and on the server, but the
            //UI belongs to the local player only - without this check someone else
            //using their book would toggle your panel
            if (player.whoAmI == Main.myPlayer) {
                GuideCatalogUI.Instance.Toggle();
            }
            return true;
        }
    }
}
