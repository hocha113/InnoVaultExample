using InnoVault;
using InnoVault.BehaviorTrees;
using InnoVault.StateMachines;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.NPCs.Gatekeeper
{
    // Showcase: Gatekeeper FSM and BT graph (BOSS-201)
    // Demonstrates: VaultState, PhaseTrigger, BehaviorTreeBuilder, Blackboard keys
    // Wiki: https://innovault.wiki/cn/ai/state-machines/
    // Wiki: https://innovault.wiki/cn/ai/behavior-trees/
    // Source: InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeperAI.cs

    internal static class AcademyGatekeeperKeys
    {
        public static readonly BlackboardKey<int> Phase = new("gatekeeper_phase");
        public static readonly BlackboardKey<int> AttackIndex = new("gatekeeper_attack_index");
        public static readonly BlackboardKey<int> CurrentAttack = new("gatekeeper_current_attack");
        public static readonly BlackboardKey<Vector2> LastTargetPosition = new("gatekeeper_last_target_position");
    }

    internal abstract class GatekeeperState : VaultState<AcademyGatekeeperContext>
    {
        protected static bool CanRunAuthority => !VaultUtils.isClient;

        protected static void HoverNearTarget(AcademyGatekeeperContext ctx, Vector2 offset, float speed, float inertia) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            Vector2 destination = target.Center + offset;
            Vector2 desired = npc.DirectionTo(destination) * speed;
            npc.velocity = (npc.velocity * inertia + desired) / (inertia + 1f);
            npc.rotation = npc.velocity.X * 0.018f;
        }

        protected static void FaceTarget(AcademyGatekeeperContext ctx) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            npc.direction = target.Center.X >= npc.Center.X ? 1 : -1;
            npc.spriteDirection = npc.direction;
        }
    }

    internal sealed class GatekeeperIntroState : GatekeeperState
    {
        public override int StateId => 0;
        public override string StateName => "Intro";

        public override void OnEnter(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            base.OnEnter(machine, ctx);
            ctx.Npc.netUpdate = true;
            if (Main.netMode != NetmodeID.Server) {
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.55f, Pitch = 0.2f }, ctx.Npc.Center);
            }
        }

        public override IVaultState<AcademyGatekeeperContext> OnUpdate(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            base.OnUpdate(machine, ctx);
            FaceTarget(ctx);
            HoverNearTarget(ctx, new Vector2(0f, -220f), 7f, 18f);

            if (Main.netMode != NetmodeID.Server && Timer % 6 == 0) {
                Dust.NewDustDirect(ctx.Npc.position, ctx.Npc.width, ctx.Npc.height, DustID.BlueTorch, 0f, -1f, 160, default, 1.1f).noGravity = true;
            }

            return Timer >= 90 ? new GatekeeperAttackState() : null;
        }
    }

    internal sealed class GatekeeperAttackState : GatekeeperState
    {
        public override int StateId => 1;
        public override string StateName => "Attack";

        public override void OnEnter(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            base.OnEnter(machine, ctx);
            ctx.Npc.ai[0] = 0f;
            ctx.AttackTree.Reset();
            machine.Blackboard.Set(AcademyGatekeeperKeys.LastTargetPosition, ctx.Target.Center);
        }

        public override IVaultState<AcademyGatekeeperContext> OnUpdate(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            base.OnUpdate(machine, ctx);
            FaceTarget(ctx);

            BTStatus status = ctx.AttackTree.Tick(ctx, machine.Blackboard);
            return status == BTStatus.Running ? null : new GatekeeperRecoverState();
        }

        public override void OnExit(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            ctx.AttackTree.Reset();
        }
    }

    internal sealed class GatekeeperRecoverState : GatekeeperState
    {
        public override int StateId => 2;
        public override string StateName => "Recover";

        public override IVaultState<AcademyGatekeeperContext> OnUpdate(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            base.OnUpdate(machine, ctx);
            FaceTarget(ctx);
            HoverNearTarget(ctx, new Vector2(0f, -180f), 5f, 22f);
            return Timer >= 45 ? new GatekeeperAttackState() : null;
        }
    }

    internal sealed class GatekeeperEnragedState : GatekeeperState
    {
        public override int StateId => 3;
        public override string StateName => "Enraged";

        public override void OnEnter(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            base.OnEnter(machine, ctx);
            ctx.Npc.netUpdate = true;
            if (Main.netMode != NetmodeID.Server) {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.15f }, ctx.Npc.Center);
            }
        }

        public override IVaultState<AcademyGatekeeperContext> OnUpdate(VaultStateMachine<AcademyGatekeeperContext> machine, AcademyGatekeeperContext ctx) {
            base.OnUpdate(machine, ctx);
            FaceTarget(ctx);
            HoverNearTarget(ctx, new Vector2(0f, -260f), 9f, 12f);

            if (Timer % 12 == 0 && Main.netMode != NetmodeID.Server) {
                Dust.NewDustDirect(ctx.Npc.position, ctx.Npc.width, ctx.Npc.height, DustID.Electric, 0f, -2f, 100, default, 1.2f).noGravity = true;
            }

            return Timer >= 70 ? new GatekeeperAttackState() : null;
        }
    }

    internal static class AcademyGatekeeperBehaviors
    {
        public static BTNode<AcademyGatekeeperContext> CreateAttackTree() {
            return BehaviorTreeBuilder.For<AcademyGatekeeperContext>()
                .Selector()
                    .Sequence()
                        .Condition(ctx => ctx.Npc.life * 2 <= ctx.Npc.lifeMax)
                        .Action(ChooseAlternatingAttack)
                    .End()
                    .Action(ChooseNormalAttack)
                .End()
                .Build();
        }

        private static BTStatus ChooseAlternatingAttack(AcademyGatekeeperContext ctx, Blackboard blackboard) {
            if (ctx.Npc.ai[0] == 0f) {
                int index = blackboard.Get(AcademyGatekeeperKeys.AttackIndex, 0);
                blackboard.Set(AcademyGatekeeperKeys.AttackIndex, index + 1);
                blackboard.Set(AcademyGatekeeperKeys.CurrentAttack, index % 2);
            }

            return blackboard.Get(AcademyGatekeeperKeys.CurrentAttack, 0) == 0
                ? FireRadialVolley(ctx, blackboard, enraged: true)
                : StartDash(ctx, blackboard, enraged: true);
        }

        private static BTStatus ChooseNormalAttack(AcademyGatekeeperContext ctx, Blackboard blackboard) {
            if (ctx.Npc.ai[0] == 0f) {
                int index = blackboard.Get(AcademyGatekeeperKeys.AttackIndex, 0);
                blackboard.Set(AcademyGatekeeperKeys.AttackIndex, index + 1);
                blackboard.Set(AcademyGatekeeperKeys.CurrentAttack, index % 3 == 2 ? 1 : 0);
            }

            return blackboard.Get(AcademyGatekeeperKeys.CurrentAttack, 0) == 1
                ? StartDash(ctx, blackboard, enraged: false)
                : FireRadialVolley(ctx, blackboard, enraged: false);
        }

        private static BTStatus FireRadialVolley(AcademyGatekeeperContext ctx, Blackboard blackboard, bool enraged) {
            NPC npc = ctx.Npc;
            HoverDuringAttack(ctx, enraged);
            blackboard.Set(AcademyGatekeeperKeys.LastTargetPosition, ctx.Target.Center);

            if (CanShoot() && npc.ai[0] % (enraged ? 18f : 26f) == 0f) {
                int count = enraged ? 7 : 5;
                float spread = enraged ? 0.95f : 0.7f;
                Vector2 toTarget = npc.DirectionTo(ctx.Target.Center);
                for (int i = 0; i < count; i++) {
                    float progress = count == 1 ? 0.5f : i / (float)(count - 1);
                    Vector2 velocity = toTarget.RotatedBy(MathHelper.Lerp(-spread, spread, progress)) * (enraged ? 8.5f : 7f);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity, ModContent.ProjectileType<AcademyGatekeeperBolt>(), npc.damage / 5, 1f, Main.myPlayer);
                }
                npc.netUpdate = true;
            }

            npc.ai[0]++;
            int duration = enraged ? 74 : 86;
            if (npc.ai[0] >= duration) {
                npc.ai[0] = 0f;
                return BTStatus.Success;
            }
            return BTStatus.Running;
        }

        private static BTStatus StartDash(AcademyGatekeeperContext ctx, Blackboard blackboard, bool enraged) {
            NPC npc = ctx.Npc;
            int warmup = enraged ? 22 : 30;
            int duration = enraged ? 58 : 68;
            Vector2 targetPosition = blackboard.Get(AcademyGatekeeperKeys.LastTargetPosition, ctx.Target.Center);

            if (npc.ai[0] == 0f) {
                targetPosition = ctx.Target.Center;
                blackboard.Set(AcademyGatekeeperKeys.LastTargetPosition, targetPosition);
            }

            if (npc.ai[0] < warmup) {
                HoverNearTarget(ctx, new Vector2(-ctx.Npc.direction * 260f, -90f), enraged ? 10f : 8f, 10f);
                if (Main.netMode != NetmodeID.Server && npc.ai[0] % 4f == 0f) {
                    Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.GoldFlame, 0f, 0f, 120, default, 1f).noGravity = true;
                }
            }
            else if (npc.ai[0] == warmup) {
                Vector2 dashVelocity = npc.DirectionTo(targetPosition) * (enraged ? 17f : 14f);
                npc.velocity = dashVelocity;
                npc.netUpdate = true;
                if (Main.netMode != NetmodeID.Server) {
                    SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.65f }, npc.Center);
                }
            }
            else {
                npc.velocity *= 0.985f;
            }

            npc.ai[0]++;
            if (npc.ai[0] >= duration) {
                npc.ai[0] = 0f;
                return BTStatus.Success;
            }
            return BTStatus.Running;
        }

        private static void HoverDuringAttack(AcademyGatekeeperContext ctx, bool enraged) {
            float side = ctx.Npc.Center.X < ctx.Target.Center.X ? -1f : 1f;
            HoverNearTarget(ctx, new Vector2(side * 210f, enraged ? -190f : -160f), enraged ? 7.5f : 6f, enraged ? 13f : 18f);
        }

        private static void HoverNearTarget(AcademyGatekeeperContext ctx, Vector2 offset, float speed, float inertia) {
            NPC npc = ctx.Npc;
            Vector2 destination = ctx.Target.Center + offset;
            Vector2 desired = npc.DirectionTo(destination) * speed;
            npc.velocity = (npc.velocity * inertia + desired) / (inertia + 1f);
            npc.rotation = npc.velocity.X * 0.02f;
        }

        private static bool CanShoot() => !VaultUtils.isClient;
    }
}
