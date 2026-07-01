using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.NPCs.Tutor
{
    // Showcase: Tutor beacon item (NPC-401)
    // Demonstrates: simple authority-guarded NPC spawn entry point for NPCOverride/Narrative testing
    // Wiki: https://innovault.wiki/cn/overrides/npc/
    // Source: InnoVaultExample/Content/Academy/NPCs/Tutor/AcademyTutorBeacon.cs

    public class AcademyTutorBeacon : ModItem
    {
        public override string Texture => "Terraria/Images/Item_29";

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 30;
            Item.maxStack = 20;
            Item.consumable = true;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(silver: 10);
        }

        public override bool CanUseItem(Player player)
            => Main.netMode != NetmodeID.MultiplayerClient && !NPC.AnyNPCs(AcademyVaultTutor.TypeId);

        public override bool? UseItem(Player player) {
            Vector2 spawnPos = player.Center + new Vector2(player.direction * 80f, -8f);
            int index = NPC.NewNPC(new EntitySource_WorldEvent(nameof(AcademyTutorBeacon)), (int)spawnPos.X, (int)spawnPos.Y, AcademyVaultTutor.TypeId);
            if (index >= 0 && index < Main.maxNPCs) {
                Main.npc[index].homeless = true;
                Main.npc[index].netUpdate = true;
            }
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Book)
                .AddIngredient(ItemID.Torch, 3)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
