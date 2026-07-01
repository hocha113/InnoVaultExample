using InnoVaultExample.Content.Academy.Weapons.DrillGun;
using InnoVaultExample.Content.Academy.World.TrainingGround;
using InnoVaultExample.Content.Academy.NPCs.Gatekeeper;
using InnoVaultExample.Content.Academy.NPCs.Tutor;
using InnoVaultExample.Content.Academy.Overrides;
using InnoVaultExample.Content.Items;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content
{
    internal class VEPlayer : ModPlayer
    {
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath) {
            yield return new Item(ModContent.ItemType<InnoVaultGuidebook>());
            yield return new Item(ModContent.ItemType<ExampleButton>());
            yield return new Item(ModContent.ItemType<StarFireSword>());
            yield return new Item(ModContent.ItemType<VaultSword>());
            yield return new Item(ItemID.WoodenSword);
            yield return new Item(ModContent.ItemType<AcademyTrainingWorkbenchItem>());
            yield return new Item(ModContent.ItemType<AcademyTrainingTurretItem>());
            yield return new Item(ModContent.ItemType<AcademyTrainingLockerItem>());
            yield return new Item(ModContent.ItemType<AcademyDrillGun>());
            yield return new Item(ModContent.ItemType<AcademyGatekeeperSummon>(), 3);
            yield return new Item(ModContent.ItemType<AcademyTutorBeacon>(), 3);
            yield return new Item(ModContent.ItemType<AcademyOverrideLens>());
        }
    }
}
