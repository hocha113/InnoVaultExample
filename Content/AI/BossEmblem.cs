using InnoVault;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.AI
{
    //秘库看守者的召唤物。生成 Boss 这件事在多人下有讲究：NPC.NewNPC /
    //NPC.SpawnOnPlayer 只能在服务端调用，客户端得发请求让服务器代劳，
    //VaultUtils.TrySpawnBossWithNet 把这套分流封装好了。
    //
    //Summon item for the Vault Warden. Spawning a boss is netcode-sensitive:
    //NPC.NewNPC / NPC.SpawnOnPlayer are server-side calls only, and clients
    //must instead ask the server to do it. VaultUtils.TrySpawnBossWithNet
    //wraps that whole split.
    //
    //https://innovault.wiki/cn/ai/state-machines/

    internal class BossEmblem : ModItem
    {
        //这件召唤物借原版战士徽章的贴图，不单独画图
        //
        //This summon item borrows the vanilla Warrior Emblem sprite instead of
        //shipping one of its own
        public override string Texture => "Terraria/Images/Item_" + ItemID.WarriorEmblem;

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 28;
            //不消耗：这是拿来反复召唤、反复观察状态流转的教具
            //
            //Non-consumable: this is a teaching prop meant for summoning the
            //boss again and again to watch the state flow
            Item.maxStack = 1;
            Item.consumable = false;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 30);
        }

        public override bool CanUseItem(Player player)
            => !NPC.AnyNPCs(ModContent.NPCType<DemoBoss>());

        public override bool? UseItem(Player player) {
            //单机：helper 直接 NPC.SpawnOnPlayer 放出 Boss 并广播"已苏醒"。
            //多人：UseItem 会在使用者客户端、服务器和其他客户端各跑一遍，
            //默认的 checkLocalPlayer 只放行使用者本人那端的 whoAmI 检查，
            //由它发出 SpawnBossUseLicenseStartEvent，服务器核对
            //MPAllowedEnemies 白名单（见 DemoBoss.SetStaticDefaults）后代为生成
            //
            //Single player: the helper calls NPC.SpawnOnPlayer directly and
            //announces "has awoken". In multiplayer this UseItem runs on the
            //using client, the server, and every other client; the default
            //checkLocalPlayer lets only the user's own side past the whoAmI
            //check, and that side sends SpawnBossUseLicenseStartEvent so the
            //server spawns the boss after checking the MPAllowedEnemies
            //whitelist (see DemoBoss.SetStaticDefaults).
            VaultUtils.TrySpawnBossWithNet<DemoBoss>(player);
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Book)
                .AddIngredient(ItemID.FallenStar, 3)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
