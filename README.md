# InnoVault Example (Vault Academy)

Runnable companion mod for [InnoVault](https://innovault.wiki). Each showcase maps to a Wiki chapter with an in-game try step, not a bare API stub.

**Version:** 0.9  
**Player docs:** [innovault.wiki](https://innovault.wiki)  
**Example mod index:** [getting-started/example-mod](https://innovault.wiki/en/getting-started/example-mod/)

## Theme

**Vault Academy / 金库学院** treats the Guidebook as a student manual. Items are course props; the catalog UI lists registry entries with Wiki slugs, phase tags, trap gallery notes, and try-in-game steps.

## Build

1. Install tModLoader and enable **InnoVault** plus **InnoVault Example**.
2. Open `InnoVaultExample.csproj` in the tModLoader `ModSources` folder (sibling to `InnoVault` and `tModLoader.targets`).
3. Build:

```bash
dotnet build
```

Or build and reload from the tModLoader mod sources menu in-game.

Requires **InnoVault 1.9.97** (see `build.txt` `modReferences`).

## How to play the showcases

1. Create a character. Starting items include the **Academy Manual**, PRT swords, wooden sword override demo, and **Training Ground** placeables (workbench, turret, locker).
2. **Right-click** the manual: open the **Course Catalog** (`UIHandle<T>` panel driven by `AcademyRegistry`, trap gallery, and course progress footer).
3. **Left-click** the manual: spawn a synced **Academy Beacon Actor** and client PRT sparks at the cursor.
4. **Hold** Vault Sword or Star Fire Sword: PRT hold demos (client only; skipped on dedicated servers).
5. Check the wooden sword tooltip for **ItemOverride** tinting.
6. **Training Ground (WORLD-301):** place the starting workbench, turret, and locker kits. Hover the workbench, let the turret target NPCs, deposit and withdraw locker items.
7. **Drill Gun (COMBAT-201):** craft **Academy Drill Gun** at an anvil (iron bars + wood). Left click fires pierce bolts with ribbon trails; right click reloads the magazine.
8. **Gatekeeper (BOSS-201):** use the starting **Gatekeeper Sigil** to fight a two-phase FSM + BehaviorTree boss.
9. **Tutor (NPC-401):** use the starting **Tutor Beacon**, talk to the tutor, then open the Narrative briefing or catalog button.
10. **Override Gallery (OVERRIDE-401):** use the starting **Override Lens** to toggle Player/Tile/Dust gallery effects; visit the title menu for MenuOverride.
11. Optional: enable **Academy Arcade** in mod config for the MenuGame sample on the title screen.

## Repository layout (Phases 0-6)

| Path | Role |
|------|------|
| `Core/AcademyRegistry.cs` | Catalog metadata for UI and Wiki links (28 showcases) |
| `Core/AcademyAuthority.cs` | Shared SP + server mutation authority helper |
| `Core/CommentStandards.cs` | Shared trap comment prefixes and catalog trap gallery |
| `Core/Loaders/AcademyAssetLoader.cs` | IVaultLoader bootstrap and SaveMod timing (CORE-001) |
| `Core/Assets/` | VaultLoaden emblem, badge, and catalog asset classes |
| `Content/Academy/Weapons/DrillGun/` | BaseHeldProj drill gun stack (COMBAT-201) |
| `Content/Academy/World/TrainingGround/` | TileProcessor trio and placeable kits (WORLD-301) |
| `Content/Academy/Actors/` | Academy Beacon Actor + SyncVar lifetime |
| `Content/Academy/Persistence/` | DataModule progress, ModPlayer save, SaveWorld sample |
| `Content/Academy/Storages/` | Locker IStorageProvider factory |
| `Content/Academy/NPCs/Gatekeeper/` | BOSS-201 NpcStateMachine + BehaviorTree boss |
| `Content/Academy/NPCs/Tutor/` | NPC-401 NPCOverride chat buttons + NarrativeScenario briefing |
| `Content/Academy/Overrides/` | OVERRIDE-401 Player/Tile/Scene/Menu/GlobalDust opt-in gallery |
| `Localization/zh-Hans_*.hjson` | Chinese player-facing strings |
| `Localization/en-US_*.hjson` | English player-facing strings |

Legacy Phase 0 samples remain under `Content/` (PRT swords, legacy TPs, ExampleGuideOrb reference).

## Contributing

Match comment rules in the [Vault Academy deep plan](https://github.com/hocha113/InnoVaultExample) (English code comments, Chinese hjson, file headers with Wiki URLs). Do not link readers to InnoVault internal `Doc/` folders.
