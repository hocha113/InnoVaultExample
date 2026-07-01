using InnoVault.GameSystem;
using InnoVaultExample.Core;
using Terraria.ModLoader.IO;

namespace InnoVaultExample.Content.Academy.Persistence
{
    // Showcase: Training Ground World Save (WORLD-301)
    // Demonstrates: SaveWorld subclass, world-scoped clear flag via VaultSave pipeline
    // Wiki: https://innovault.wiki/cn/persistence/save-content/
    // Source: InnoVaultExample/Content/Academy/Persistence/AcademyTrainingWorldSave.cs

    internal class AcademyTrainingWorldSave : SaveWorld
    {
        public static bool GroundCleared { get; private set; }

        public static void MarkGroundCleared() {
            if (!AcademyAuthority.HasAuthority()) {
                return;
            }

            GroundCleared = true;
        }

        public override void SaveData(TagCompound tag) {
            tag["groundCleared"] = GroundCleared;
        }

        public override void LoadData(TagCompound tag) {
            GroundCleared = tag.GetBool("groundCleared");
        }
    }
}
