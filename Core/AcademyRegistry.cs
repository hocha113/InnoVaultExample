using System.Collections.Generic;

namespace InnoVaultExample.Core
{
    /// <summary>
    /// Catalog metadata for Guidebook UI and Wiki cross-links. Does not replace VaultType auto-registration.
    /// </summary>
    public interface IShowcaseMetadata
    {
        string Id { get; }
        string DisplayName { get; }
        string WikiSlug { get; }
        string SourcePath { get; }
        string TryInGame { get; }
        int Phase { get; }
        string Tier { get; }
    }

    public sealed class ShowcaseEntry : IShowcaseMetadata
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public required string WikiSlug { get; init; }
        public required string SourcePath { get; init; }
        public required string TryInGame { get; init; }
        public required int Phase { get; init; }
        public required string Tier { get; init; }

        public string WikiUrl => $"https://innovault.wiki/cn/{WikiSlug.TrimStart('/')}/";
    }

    public static class AcademyRegistry
    {
        private static readonly ShowcaseEntry[] Entries =
        [
            // Phase 0 — legacy hub samples
            new() {
                Id = "manual",
                DisplayName = "Academy Manual",
                WikiSlug = "content/ui-handle",
                SourcePath = "InnoVaultExample/Content/Items/InnoVaultGuidebook.cs",
                TryInGame = "Right-click the manual to open the course catalog.",
                Phase = 0,
                Tier = "MVP",
            },
            new() {
                Id = "catalog-ui",
                DisplayName = "Course Catalog UI",
                WikiSlug = "content/ui-handle",
                SourcePath = "InnoVaultExample/Content/ExampleUIs/ApiGuideUI.cs",
                TryInGame = "Right-click the manual; drag the title bar to move the panel.",
                Phase = 0,
                Tier = "MVP",
            },
            new() {
                Id = "save-registrar",
                DisplayName = "Guide Save Counters",
                WikiSlug = "persistence/save-content",
                SourcePath = "InnoVaultExample/Content/ExampleGuideSaveData.cs",
                TryInGame = "Use the manual; stats appear in the catalog footer.",
                Phase = 0,
                Tier = "MVP",
            },
            new() {
                Id = "item-override",
                DisplayName = "Wooden Sword Syllabus",
                WikiSlug = "overrides/item",
                SourcePath = "InnoVaultExample/Content/ExampleOverrides.cs",
                TryInGame = "Check the starting wooden sword tooltip and inventory tint.",
                Phase = 0,
                Tier = "MVP",
            },
            new() {
                Id = "prt-vault-sword",
                DisplayName = "Vault Sword Hold Spark (PRT-101)",
                WikiSlug = "content/prt",
                SourcePath = "InnoVaultExample/Content/Items/VaultSword.cs",
                TryInGame = "Hold the Vault Sword to emit pooled sparks around the player.",
                Phase = 0,
                Tier = "MVP",
            },
            new() {
                Id = "prt-starfire",
                DisplayName = "Star Fire Sword Spark (PRT-102)",
                WikiSlug = "content/prt",
                SourcePath = "InnoVaultExample/Content/Items/StarFireSword.cs",
                TryInGame = "Hold the Star Fire Sword to spawn mouse-anchored star particles.",
                Phase = 0,
                Tier = "MVP",
            },
            new() {
                Id = "example-button",
                DisplayName = "Example Button UI",
                WikiSlug = "content/ui-handle",
                SourcePath = "InnoVaultExample/Content/ExampleUIs/ExampleButtonUI.cs",
                TryInGame = "Right-click the starting Example Button item to open the legacy button panel.",
                Phase = 0,
                Tier = "MVP",
            },
            new() {
                Id = "guide-orb",
                DisplayName = "Guide Orb Actor (Legacy Reference)",
                WikiSlug = "persistence/actor",
                SourcePath = "InnoVaultExample/Content/ExampleGuideOrb.cs",
                TryInGame = "Orphan sample: not spawned by current loadout. Read for SyncVar SP pattern; live demo is Academy Beacon Actor.",
                Phase = 0,
                Tier = "Legacy",
            },
            new() {
                Id = "workbench-tp",
                DisplayName = "Example Workbench TP (Legacy)",
                WikiSlug = "content/tile-processor",
                SourcePath = "InnoVaultExample/Content/ExampleTileProcessors/ExampleWorkbenchTP.cs",
                TryInGame = "Legacy pre-Academy TP; prefer Academy Training Workbench from starting loadout.",
                Phase = 0,
                Tier = "Legacy",
            },
            new() {
                Id = "torch-turret-tp",
                DisplayName = "Demon Torch Turret TP (Legacy)",
                WikiSlug = "content/tile-processor",
                SourcePath = "InnoVaultExample/Content/ExampleTileProcessors/ExampleDemonTorchTurretTP.cs",
                TryInGame = "Legacy pre-Academy turret; prefer Academy Training Turret from starting loadout.",
                Phase = 0,
                Tier = "Legacy",
            },
            new() {
                Id = "legacy-ui",
                DisplayName = "Legacy Example UI",
                WikiSlug = "content/ui-handle",
                SourcePath = "InnoVaultExample/Content/ExampleUIs/ExampleUI.cs",
                TryInGame = "Legacy panel; prefer ApiGuideUI for new UIHandle code.",
                Phase = 0,
                Tier = "Legacy",
            },
            new() {
                Id = "arcade",
                DisplayName = "Academy Arcade (MenuGame)",
                WikiSlug = "content/ui-handle",
                SourcePath = "InnoVaultExample/Content/ExampleUIs/ExampleMenuGame/MenuGame.cs",
                TryInGame = "Enable Menu Game in mod config, then open the main menu.",
                Phase = 0,
                Tier = "Stretch",
            },

            // Phase 1 — bootstrap and VaultLoaden
            new() {
                Id = "asset-loader",
                DisplayName = "Academy Asset Loader (CORE-001)",
                WikiSlug = "getting-started/ivault-loader",
                SourcePath = "InnoVaultExample/Core/Loaders/AcademyAssetLoader.cs",
                TryInGame = "Reload the mod; check tModLoader logs for VaultLoaden readiness warnings on missing PNGs.",
                Phase = 1,
                Tier = "Flagship",
            },
            new() {
                Id = "vault-loaden-assets",
                DisplayName = "VaultLoaden Asset Classes (CORE-002..004)",
                WikiSlug = "core/vault-loaden",
                SourcePath = "InnoVaultExample/Core/Assets/AcademyEmblemAssets.cs",
                TryInGame = "Read Emblem, Badge, and Catalog VaultLoaden classes; bundle PNGs under Assets/ to clear loader warnings.",
                Phase = 1,
                Tier = "MVP",
            },

            // Phase 2 — COMBAT-201 Drill Gun stack
            new() {
                Id = "drill-gun",
                DisplayName = "Academy Drill Gun (COMBAT-201)",
                WikiSlug = "content/base-held-proj",
                SourcePath = "InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillGun.cs",
                TryInGame = "Craft at an anvil (iron bar + musket). Left click fires pierce bolts; right click reloads.",
                Phase = 2,
                Tier = "Flagship",
            },
            new() {
                Id = "drill-held",
                DisplayName = "Drill Gun Held Body",
                WikiSlug = "content/base-held-proj",
                SourcePath = "InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillHeld.cs",
                TryInGame = "Equip the drill; watch magazine ticks, reload timer, and muzzle PRT (client only).",
                Phase = 2,
                Tier = "MVP",
            },
            new() {
                Id = "drill-bolt",
                DisplayName = "Drill Bolt + ProjOverride",
                WikiSlug = "overrides/projectile",
                SourcePath = "InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillBolt.cs",
                TryInGame = "Fire the drill at enemies; bolt homing and pierce come from ProjOverride hooks.",
                Phase = 2,
                Tier = "MVP",
            },
            new() {
                Id = "drill-trail",
                DisplayName = "Drill Bolt Ribbon Trail",
                WikiSlug = "content/trails",
                SourcePath = "InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillTrail.cs",
                TryInGame = "Fire the drill; watch the ColoredVertex ribbon trail on each bolt.",
                Phase = 2,
                Tier = "MVP",
            },
            new() {
                Id = "drill-use-anim",
                DisplayName = "Drill Gun ItemUseAnimation",
                WikiSlug = "overrides/item-use-animation",
                SourcePath = "InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillUseAnim.cs",
                TryInGame = "Aim the drill; recoil pose syncs via PlayerNetwork on multiplayer clients.",
                Phase = 2,
                Tier = "MVP",
            },

            // Phase 3 — WORLD-301 Training Ground and persistence
            new() {
                Id = "beacon-actor",
                DisplayName = "Academy Beacon Actor",
                WikiSlug = "persistence/actor",
                SourcePath = "InnoVaultExample/Content/Academy/Actors/AcademyBeaconActor.cs",
                TryInGame = "Left-click the manual at the cursor to spawn a synced beacon with SyncVar lifetime.",
                Phase = 3,
                Tier = "Flagship",
            },
            new() {
                Id = "beacon-aura",
                DisplayName = "Beacon Aura RenderHandle",
                WikiSlug = "content/render-handle",
                SourcePath = "InnoVaultExample/Content/Academy/Render/AcademyBeaconAuraRH.cs",
                TryInGame = "Spawn a beacon; aura ring draws in DrawAfterPlayers (skipped on dedServ).",
                Phase = 3,
                Tier = "MVP",
            },
            new() {
                Id = "training-workbench",
                DisplayName = "Academy Training Workbench",
                WikiSlug = "content/tile-processor",
                SourcePath = "InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingWorkbenchTP.cs",
                TryInGame = "Place the starting workbench kit; hover to mirror held item. Sneak-right-click marks ground cleared.",
                Phase = 3,
                Tier = "Flagship",
            },
            new() {
                Id = "training-turret",
                DisplayName = "Academy Training Turret",
                WikiSlug = "utilities/targeting",
                SourcePath = "InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingTurretTP.cs",
                TryInGame = "Place the starting turret kit near hostile NPCs; server-side bolts auto-target.",
                Phase = 3,
                Tier = "MVP",
            },
            new() {
                Id = "training-locker",
                DisplayName = "Academy Supply Locker",
                WikiSlug = "advanced/storages",
                SourcePath = "InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingLockerTP.cs",
                TryInGame = "Place the starting locker kit; right-click deposit held items, empty hand withdraws one stack.",
                Phase = 3,
                Tier = "MVP",
            },
            new() {
                Id = "locker-storage",
                DisplayName = "Locker Storage Provider",
                WikiSlug = "advanced/storages",
                SourcePath = "InnoVaultExample/Content/Academy/Storages/AcademyLockerStorageProvider.cs",
                TryInGame = "Read IStorageProviderFactory auto-registration backing the locker TP inventory.",
                Phase = 3,
                Tier = "MVP",
            },
            new() {
                Id = "progress-module",
                DisplayName = "Student Progress Module",
                WikiSlug = "persistence/data-modules",
                SourcePath = "InnoVaultExample/Content/Academy/Persistence/AcademyProgressModule.cs",
                TryInGame = "Visit training tiles and use the manual; course flags update in the catalog progress footer.",
                Phase = 3,
                Tier = "MVP",
            },
            new() {
                Id = "student-player",
                DisplayName = "Academy Student Player Save",
                WikiSlug = "persistence/data-modules",
                SourcePath = "InnoVaultExample/Content/Academy/Persistence/AcademyStudentPlayer.cs",
                TryInGame = "Progress persists per player via DataModuleStore on ModPlayer SaveData.",
                Phase = 3,
                Tier = "MVP",
            },
            new() {
                Id = "world-save",
                DisplayName = "Training Ground World Save",
                WikiSlug = "persistence/save-content",
                SourcePath = "InnoVaultExample/Content/Academy/Persistence/AcademyTrainingWorldSave.cs",
                TryInGame = "Sneak-right-click the academy workbench to set the world-scoped ground-cleared flag.",
                Phase = 3,
                Tier = "MVP",
            },

            // Phase 4 — BOSS-201 AI systems
            new() {
                Id = "gatekeeper-boss",
                DisplayName = "Academy Gatekeeper Boss (BOSS-201)",
                WikiSlug = "ai/state-machines",
                SourcePath = "InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeper.cs",
                TryInGame = "Use the starting Gatekeeper Sigil to spawn a two-phase FSM boss.",
                Phase = 4,
                Tier = "Flagship",
            },
            new() {
                Id = "gatekeeper-ai",
                DisplayName = "Gatekeeper FSM + Behavior Tree",
                WikiSlug = "ai/behavior-trees",
                SourcePath = "InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeperAI.cs",
                TryInGame = "Fight the Gatekeeper; watch intro, recover, half-health phase trigger, and alternating BT attacks.",
                Phase = 4,
                Tier = "MVP",
            },
            new() {
                Id = "gatekeeper-summon",
                DisplayName = "Gatekeeper Sigil",
                WikiSlug = "ai/state-machines",
                SourcePath = "InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeperSummon.cs",
                TryInGame = "Use the sigil from the starting loadout; multiplayer clients request server spawn safely.",
                Phase = 4,
                Tier = "MVP",
            },
            new() {
                Id = "gatekeeper-bolt",
                DisplayName = "Gatekeeper Bolt",
                WikiSlug = "ai/behavior-trees",
                SourcePath = "InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeperBolt.cs",
                TryInGame = "During the volley attack, hostile bolts are spawned only by the authoritative side.",
                Phase = 4,
                Tier = "MVP",
            },

            // Phase 5 — NPC-401 Tutor narrative and NPCOverride
            new() {
                Id = "vault-tutor",
                DisplayName = "Academy Vault Tutor (NPC-401)",
                WikiSlug = "overrides/npc",
                SourcePath = "InnoVaultExample/Content/Academy/NPCs/Tutor/AcademyVaultTutor.cs",
                TryInGame = "Use the starting Tutor Beacon, talk to the tutor, then click Briefing or Catalog.",
                Phase = 5,
                Tier = "Flagship",
            },
            new() {
                Id = "tutor-briefing",
                DisplayName = "Tutor Briefing Scenario",
                WikiSlug = "narrative",
                SourcePath = "InnoVaultExample/Content/Academy/NPCs/Tutor/AcademyTutorBriefingScenario.cs",
                TryInGame = "Click the tutor's Briefing button to play a default Dialogue/Choice/Popup narrative.",
                Phase = 5,
                Tier = "MVP",
            },
            new() {
                Id = "tutor-beacon",
                DisplayName = "Tutor Beacon",
                WikiSlug = "overrides/npc",
                SourcePath = "InnoVaultExample/Content/Academy/NPCs/Tutor/AcademyTutorBeacon.cs",
                TryInGame = "Use the beacon from the starting loadout to spawn the tutor next to the player.",
                Phase = 5,
                Tier = "MVP",
            },

            // Phase 6 — OVERRIDE-401 gallery
            new() {
                Id = "override-lens",
                DisplayName = "Override Lens (OVERRIDE-401)",
                WikiSlug = "overrides",
                SourcePath = "InnoVaultExample/Content/Academy/Overrides/AcademyOverrideLens.cs",
                TryInGame = "Use the starting Override Lens to toggle opt-in gallery effects.",
                Phase = 6,
                Tier = "Flagship",
            },
            new() {
                Id = "override-gallery",
                DisplayName = "Override Gallery Hooks",
                WikiSlug = "overrides/player",
                SourcePath = "InnoVaultExample/Content/Academy/Overrides/AcademyOverrideGallery.cs",
                TryInGame = "Toggle the lens, hit an NPC, right-click a workbench, and visit the title menu.",
                Phase = 6,
                Tier = "MVP",
            },
            new() {
                Id = "override-gallery-state",
                DisplayName = "Override Gallery State",
                WikiSlug = "overrides/global-dust",
                SourcePath = "InnoVaultExample/Content/Academy/Overrides/AcademyOverrideGalleryState.cs",
                TryInGame = "Read the opt-in ModPlayer state that keeps broad overrides from always mutating gameplay.",
                Phase = 6,
                Tier = "MVP",
            },
        ];

        public static IReadOnlyList<ShowcaseEntry> All { get; private set; } = Entries;

        public static void Initialize() {
            All = Entries;
        }
    }
}
