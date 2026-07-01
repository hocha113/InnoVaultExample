using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.NPCs.Gatekeeper
{
    // Showcase: Gatekeeper summon item (BOSS-201)
    // Demonstrates: multiplayer-safe boss entry point for the FSM/BT sample
    // Wiki: https://innovault.wiki/cn/ai/state-machines/
    // Source: InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeperSummon.cs

    public class AcademyGatekeeperSummon : ModItem
    {
        public override string Texture => "Terraria/Images/Item_109";

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 20;
            Item.consumable = true;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 15);
        }

        public override bool CanUseItem(Player player)
            => !NPC.AnyNPCs(AcademyGatekeeper.TypeId);

        public override bool? UseItem(Player player) {
            int bossType = AcademyGatekeeper.TypeId;
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: bossType);
            }
            else {
                NPC.SpawnOnPlayer(player.whoAmI, bossType);
            }
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
