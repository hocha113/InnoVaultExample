using InnoVault.Narrative.Composition;
using InnoVault.Narrative.Core;
using InnoVault.Narrative.Portraits;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Core;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.NPCs.Tutor
{
    // Showcase: Academy Tutor narrative briefing (NPC-401)
    // Demonstrates: NarrativeScenario, composer choices, popup messages, and local player progress reads
    // Wiki: https://innovault.wiki/cn/narrative/
    // Source: InnoVaultExample/Content/Academy/NPCs/Tutor/AcademyTutorBriefingScenario.cs

    public sealed class AcademyTutorBriefingScenario : NarrativeScenario
    {
        private const string TutorSpeaker = "Tutor";

        public override string Key => "InnoVaultExample/AcademyTutorBriefing";

        public override void SetStaticDefaults() {
            PortraitRegistry.Register(Mod.Name, TutorSpeaker)
                .Name(() => Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.Name"));
        }

        protected override void Build(NarrativeComposer composer) {
            AcademyProgressModule progress = AcademyStudentPlayer.Get(Main.LocalPlayer).Progress;
            int total = 3;

            composer
                .Say(Speaker(TutorSpeaker), Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.LineWelcome"))
                .Say(Speaker(TutorSpeaker), Language.GetTextValue(
                    "Mods.InnoVaultExample.Narrative.Tutor.LineProgress",
                    progress.CompletedCourseCount,
                    total))
                .Choice(Speaker(TutorSpeaker), Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.ChoicePrompt"), choice => {
                    choice.Option("catalog", Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.ChoiceCatalog"), NarrativeTarget.Goto("catalog"));
                    choice.Option("combat", Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.ChoiceCombat"), NarrativeTarget.Goto("combat"));
                    choice.Option("world", Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.ChoiceWorld"), NarrativeTarget.Goto("world"));
                    choice.Option("done", Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.ChoiceDone"), NarrativeTarget.Goto("done"));
                })
                .Label("catalog")
                .Popup(Popups.Message(
                    Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.PopupCatalogTitle"),
                    BuildCatalogSummary()).Claimable(false).Hold(3.5f), blocking: true)
                .Say(Speaker(TutorSpeaker), Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.LineCatalog"))
                .Goto("done")
                .Label("combat")
                .Say(Speaker(TutorSpeaker), Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.LineCombat"))
                .Goto("done")
                .Label("world")
                .Say(Speaker(TutorSpeaker), Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.LineWorld"))
                .Goto("done")
                .Label("done")
                .SayTimed(Speaker(TutorSpeaker), Language.GetTextValue("Mods.InnoVaultExample.Narrative.Tutor.LineClose"), new TimedSettings {
                    Seconds = 4f,
                    AllowManualAdvance = true,
                    ShowIndicator = true,
                })
                .End();
        }

        private static string BuildCatalogSummary() {
            int phase4Count = 0;
            foreach (ShowcaseEntry entry in AcademyRegistry.All) {
                if (entry.Phase >= 4) {
                    phase4Count++;
                }
            }

            return Language.GetTextValue(
                "Mods.InnoVaultExample.Narrative.Tutor.PopupCatalogBody",
                AcademyRegistry.All.Count,
                phase4Count);
        }
    }
}
