using InnoVault;
using InnoVault.GameSystem;
using InnoVaultExample.Content;
using InnoVaultExample.Core.Assets;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Core.Loaders
{
    // Showcase: Academy Bootstrap (CORE-001)
    // Demonstrates: IVaultLoader lifecycle, SaveMod load timing, VaultLoaden readiness checks
    // Wiki: https://innovault.wiki/cn/getting-started/ivault-loader
    // Source: InnoVaultExample/Core/Loaders/AcademyAssetLoader.cs

    /// <summary>
    /// Central InnoVault load hook for Vault Academy asset and persistence wiring.
    /// </summary>
    internal sealed class AcademyAssetLoader : IVaultLoader
    {
        private static List<string> _vaultLoadenShowcaseTypes = [];

        /// <summary>
        /// Names of Core/Assets classes discovered during LoadData; Catalog UI can list them later.
        /// </summary>
        public static IReadOnlyList<string> VaultLoadenShowcaseTypes => _vaultLoadenShowcaseTypes;

        /// <summary>
        /// True after client LoadAsset confirms VaultLoaden fields are populated.
        /// </summary>
        public static bool ClientAssetsReady { get; private set; }

        public void LoadData() {
            // Runs at the start of VaultMod.Load, before VaultType registration finishes.
            // Trap · PostSetupContent: VaultLoaden static fields are still null here; do not read textures in LoadData.
            _vaultLoadenShowcaseTypes = [];
            foreach (Type type in VaultUtils.GetAnyModCodeType()) {
                if (type.Namespace == null || !type.Namespace.StartsWith(nameof(InnoVaultExample) + ".Core.Assets", StringComparison.Ordinal)) {
                    continue;
                }

                if (type.GetCustomAttribute<VaultLoadenAttribute>(inherit: false) != null) {
                    _vaultLoadenShowcaseTypes.Add(type.Name);
                }
            }
        }

        public void SetupData() {
            // Runs inside InnoVault VaultMod.PostSetupContent, before VaultLoad.LoadAsset scans [VaultLoaden].
            // Trap · SaveMod: DoLoad belongs here (or any IVaultLoader SetupData), not in Mod.Load.
            SaveContent<SaveMod>.DoLoad<ExampleGuideSaveData>();

            // Trap · VaultLoaden: assets are still null until every SetupData() finishes and VaultLoad.LoadAsset runs.
            if (AcademyEmblemAssets.Emblem != null) {
                ModContent.GetInstance<InnoVaultExample>().Logger.Warn(
                    "AcademyAssetLoader.SetupData saw Emblem before VaultLoaden scan; check load order.");
            }
        }

        public void LoadAsset() {
            // Trap · dedServ: InnoVault skips the entire LoadAsset stage on dedicated servers; mirror that guard for manual client loads.
            if (Main.dedServ) {
                return;
            }

            // VaultLoad.LoadAsset already ran; class-level [VaultLoaden] fields should be populated now.
            // Placeholder art: bundled PNG/WAV under Assets/ are minimal colored squares (Track D); swap before release polish.
            ClientAssetsReady = AcademyEmblemAssets.Emblem != null
                && AcademyBadgeAssets.RankBadges != null
                && AcademyBadgeAssets.RankBadges.Length > 0
                && AcademyBadgeAssets.RankBadges[0] != null
                && AcademyCatalogAssets.Header != null;

            if (!ClientAssetsReady) {
                ModContent.GetInstance<InnoVaultExample>().Logger.Warn(
                    "Academy VaultLoaden assets missing; Catalog UI will fall back to vanilla placeholders.");
            }
        }

        public void UnLoadData() {
            ClientAssetsReady = false;

            if (!Main.dedServ) {
                // Trap · SaveMod: DoSave one target at a time so other SaveMod entries in the same file stay intact.
                SaveContent<SaveMod>.DoSave<ExampleGuideSaveData>();
            }

            _vaultLoadenShowcaseTypes = [];
        }
    }
}
