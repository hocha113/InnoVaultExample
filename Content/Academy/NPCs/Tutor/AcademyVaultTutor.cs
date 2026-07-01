using InnoVault.GameSystem;
using InnoVault.Narrative.Core;
using InnoVault.Narrative.Runtime;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Content.ExampleUIs;
using InnoVaultExample.Core;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.NPCs.Tutor
{
    // Showcase: Vault Academy Tutor (NPC-401)
    // Demonstrates: NPCOverride chat buttons, progress reads, and NarrativeScenario launch
    // Wiki: https://innovault.wiki/cn/narrative/
    // Wiki: https://innovault.wiki/cn/overrides/npc/
    // Source: InnoVaultExample/Content/Academy/NPCs/Tutor/AcademyVaultTutor.cs

    /// <summary>
    /// Course NPC-401: friendly tutor shell. NPCOverride owns the course dialogue buttons.
    /// </summary>
    public class AcademyVaultTutor : ModNPC
    {
        internal static int TypeId => ModContent.NPCType<AcademyVaultTutor>();

        // Placeholder art: Guide sprite until bundled tutor sheet lands.
        public override string Texture => "Terraria/Images/NPC_22";

        public override void SetDefaults() {
            NPC.width = 18;
            NPC.height = 40;
            NPC.lifeMax = 250;
            NPC.defense = 15;
            NPC.damage = 10;
            NPC.knockBackResist = 0.5f;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.friendly = true;
            NPC.townNPC = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            AnimationType = NPCID.Guide;
        }

        public override void SetStaticDefaults() {
            NPCID.Sets.ActsLikeTownNPC[Type] = true;
            NPCID.Sets.NoTownNPCHappiness[Type] = true;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs) {
            return numTownNPCs >= 0;
        }

        public override string GetChat() {
            return Language.GetTextValue("Mods.InnoVaultExample.Dialogue.AcademyVaultTutor.Idle");
        }
    }

    internal class AcademyVaultTutorOverride : NPCOverride
    {
        public override int TargetID => AcademyVaultTutor.TypeId;

        public override void GetChat(ref string chat) {
            AcademyProgressModule progress = AcademyStudentPlayer.Get(Main.LocalPlayer).Progress;
            chat = Language.GetTextValue(
                "Mods.InnoVaultExample.Dialogue.AcademyVaultTutor.Progress",
                progress.CompletedCourseCount,
                AcademyRegistryCount());
        }

        public override bool SetChatButtons(ref string button, ref string button2) {
            button = Language.GetTextValue("Mods.InnoVaultExample.Dialogue.AcademyVaultTutor.ButtonBriefing");
            button2 = Language.GetTextValue("Mods.InnoVaultExample.Dialogue.AcademyVaultTutor.ButtonCatalog");
            return false;
        }

        public override bool PreChatButtonClicked(bool firstButton) {
            if (!firstButton) {
                ApiGuideUI.Instance.Open();
                return false;
            }

            if (!Main.dedServ && !NarrativeRunner.IsScenarioActiveOrPending(NarrativeScenario.GetKey<AcademyTutorBriefingScenario>())) {
                NarrativeScenario.Find<AcademyTutorBriefingScenario>()?.Begin();
            }
            return false;
        }

        private static int AcademyRegistryCount() => AcademyRegistry.All.Count;
    }
}
