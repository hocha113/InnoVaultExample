using InnoVault.Actors;
using InnoVault.PRT;
using InnoVaultExample.Content;
using InnoVaultExample.Content.Academy.Actors;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Content.ExamplePRTs;
using InnoVaultExample.Content.ExampleUIs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Items
{
    // Showcase: Academy Manual (Guidebook Hub)
    // Demonstrates: UIHandle catalog entry, Actor spawn, PRT burst, SaveMod counters
    // Wiki: https://innovault.wiki/cn/content/ui-handle/
    // Source: InnoVaultExample/Content/Items/InnoVaultGuidebook.cs
    internal class InnoVaultGuidebook : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.Book}";

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.MenuOpen;
            Item.value = Item.buyPrice(silver: 25);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? UseItem(Player player) {
            if (player.whoAmI != Main.myPlayer) {
                return true;
            }

            ExampleGuideSaveData.RecordGuideUse();

            if (player.altFunctionUse == 2) {
                ApiGuideUI.Instance.Toggle();
                return true;
            }

            SpawnWorldDemo(player);
            return true;
        }

        private static void SpawnWorldDemo(Player player) {
            // Trap · Actor spawn: client calls ActorLoader.NewActor; framework handles MP routing.
            Vector2 spawnPosition = Main.MouseWorld - new Vector2(18f, 18f);
            ActorLoader.NewActor<AcademyBeaconActor>(spawnPosition, Vector2.Zero);
            AcademyStudentPlayer.Get(player).Progress.MarkCoursePrt101Complete();
            ExampleGuideSaveData.RecordActorSpawn();

            // Trap · dedServ: skip all PRT spawn when Main.dedServ is true (includes listen-host client).
            if (Main.dedServ) {
                return;
            }

            for (int i = 0; i < 10; i++) {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                PRTLoader.NewParticle<ExampleGuideSpark>(Main.MouseWorld, velocity, Color.CornflowerBlue, Main.rand.NextFloat(0.8f, 1.4f));
            }
        }
    }
}
