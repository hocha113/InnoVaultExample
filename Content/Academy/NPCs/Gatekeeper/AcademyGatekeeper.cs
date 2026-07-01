using InnoVault;
using InnoVault.BehaviorTrees;
using InnoVault.StateMachines;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.NPCs.Gatekeeper
{
    // Showcase: Academy Gatekeeper Boss (BOSS-201)
    // Demonstrates: NpcStateMachine, BehaviorTreeBuilder, Blackboard, and ai[] state sync
    // Wiki: https://innovault.wiki/cn/ai/state-machines/
    // Wiki: https://innovault.wiki/cn/ai/behavior-trees/
    // Source: InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeper.cs

    /// <summary>
    /// Course BOSS-201: a compact boss that shows server-authoritative FSM state and BT attack selection.
    /// </summary>
    public class AcademyGatekeeper : ModNPC
    {
        internal static int TypeId => ModContent.NPCType<AcademyGatekeeper>();

        private AcademyGatekeeperContext _context;
        private NpcStateMachine<AcademyGatekeeperContext> _stateMachine;

        // Placeholder art: Skeletron head until bundled boss sheet lands.
        public override string Texture => "Terraria/Images/NPC_35";

        public override void SetStaticDefaults() {
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults() {
            NPC.width = 80;
            NPC.height = 80;
            NPC.lifeMax = 2600;
            NPC.defense = 12;
            NPC.damage = 24;
            NPC.knockBackResist = 0.2f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = Item.buyPrice(gold: 2);
            Music = MusicID.Boss1;
        }

        public override void AI() {
            EnsureMachine();
            _context.Refresh();
            _stateMachine.Update();

            if (Main.netMode != NetmodeID.Server) {
                Lighting.AddLight(NPC.Center, 0.45f, 0.55f, 0.9f);
            }
        }

        public override void OnKill() {
            if (Main.netMode != NetmodeID.Server) {
                for (int i = 0; i < 24; i++) {
                    Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.BlueTorch, 0f, -1.5f, 120, default, 1.25f).noGravity = true;
                }
            }
        }

        private void EnsureMachine() {
            if (_stateMachine != null) {
                return;
            }

            _context = new AcademyGatekeeperContext(NPC);
            _stateMachine = new NpcStateMachine<AcademyGatekeeperContext>(_context);
            _stateMachine.Blackboard.Set(AcademyGatekeeperKeys.Phase, 1);
            _stateMachine.Blackboard.Set(AcademyGatekeeperKeys.AttackIndex, 0);
            _stateMachine.SetInitialState(new GatekeeperIntroState());
            _stateMachine.PhaseTriggers.Add(new PhaseTrigger<AcademyGatekeeperContext> {
                When = ctx => ctx.Npc.life > 0 && ctx.Npc.life * 2 <= ctx.Npc.lifeMax,
                Transition = () => new GatekeeperEnragedState(),
                OnFire = ctx => {
                    _stateMachine.Blackboard.Set(AcademyGatekeeperKeys.Phase, 2);
                    _stateMachine.Blackboard.Set(AcademyGatekeeperKeys.AttackIndex, 0);
                    ctx.Npc.netUpdate = true;
                },
                Label = "hp-below-half",
            });
        }
    }

    internal sealed class AcademyGatekeeperContext : INpcStateContext
    {
        public AcademyGatekeeperContext(NPC npc) {
            Npc = npc;
            AttackTree = AcademyGatekeeperBehaviors.CreateAttackTree();
        }

        public NPC Npc { get; }
        public Player Target => Main.player[Npc.target];
        public BTNode<AcademyGatekeeperContext> AttackTree { get; }

        public void Refresh() {
            if (!Npc.HasValidTarget || Target.dead || !Target.active) {
                Npc.TargetClosest();
            }
        }
    }
}
