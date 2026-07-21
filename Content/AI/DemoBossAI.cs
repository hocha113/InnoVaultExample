using InnoVault;
using InnoVault.BehaviorTrees;
using InnoVault.StateMachines;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.AI
{
    //秘库看守者的全部"脑子"：状态机管宏观节奏（出场 → 攻击 ⇄ 休整，半血插入狂暴），
    //行为树在攻击状态内挑招（扇形齐射 / 预警冲刺 交替）。Boss 本体见 DemoBoss.cs。
    //想现场观察：进入世界后输入 /vaultdebug 打开 InnoVault 开发者面板，在 StateMachine
    //标签页勾选状态机与行为树两个叠层，屏幕左侧就会实时列出当前状态、Blackboard
    //内容和最近的转移记录——数据来自本文件挂上的 StateMachineProbe / BehaviorTreeProbe。
    //
    //Everything that thinks for the Vault Warden: the FSM owns the macro rhythm
    //(intro, attack/recover loop, an enrage beat at half HP) while the behavior
    //tree picks the actual move (fan volley / telegraphed dash) inside the attack
    //state. The ModNPC itself lives in DemoBoss.cs. To watch it live, type
    //"/vaultdebug" in-game and enable both overlays on the StateMachine tab; the
    //overlay is fed by the probes attached below.
    //
    //https://innovault.wiki/cn/ai/state-machines/
    //https://innovault.wiki/cn/ai/behavior-trees/

    //Blackboard 键集中声明，FSM 转移条件和 BT 叶节点读写的是同一份黑板。
    //AttackCycle 只在权威端自增（选招轮换），DashAim 是冲刺锁定时记下的目标位置。
    //
    //Blackboard keys in one place; the FSM and the BT share a single blackboard
    //instance. AttackCycle only advances on the authoritative side (attack
    //rotation), DashAim is the target position captured when the dash locks on.
    internal static class DemoBossKeys
    {
        public static readonly BlackboardKey<int> AttackCycle = new("demo_boss_attack_cycle");
        public static readonly BlackboardKey<Vector2> DashAim = new("demo_boss_dash_aim");
    }

    //状态机上下文：包一层 NPC 引用加几个跨状态共享的字段。实现 INpcStateContext
    //是为了对齐框架对"NPC 上下文"的约定（NpcStateMachine 包装类也认这个接口）。
    //
    //The FSM context: an NPC reference plus a few fields shared across states.
    //Implementing INpcStateContext matches the framework contract for NPC
    //contexts (the NpcStateMachine wrapper keys off the same interface).
    internal sealed class DemoBossContext : INpcStateContext
    {
        public DemoBossContext(NPC npc) {
            Npc = npc;
            AttackTree = DemoBossAI.BuildAttackTree();
        }

        public NPC Npc { get; }
        public Player Target => Main.player[Npc.target];
        public BTNode<DemoBossContext> AttackTree { get; }

        //Builder 的转移条件只能看到上下文、看不到状态实例，所以各状态把自己的
        //Timer 镜像到这里供 When(...) 读取
        //
        //Builder transition conditions only see the context, never the state
        //instance, so each state mirrors its Timer here for When(...) to read
        public int Timer;

        //攻击状态里行为树打完一轮后置位，Builder 用它切去休整
        //
        //Set when the attack tree settles for the round; the Builder transition
        //to Recover keys off it
        public bool AttackRoundDone;

        //狂暴不是靠"当前在哪个状态"记的，而是直接由血量推导：过了半血的
        //Enraged 只是一次性的演出节拍，之后攻击/休整循环读这个标志自我强化
        //
        //Enrage is derived from HP instead of being remembered as a state: the
        //Enraged state is a one-shot beat, afterwards the attack/recover loop
        //reads this flag to power itself up
        public bool IsEnraged => Npc.life * 2 <= Npc.lifeMax;

        //调试探针引用，Boss 死亡时由 DemoBoss.DetachProbes 摘除
        //
        //Debug probe references, detached by DemoBoss.DetachProbes on death
        public StateMachineProbe<DemoBossContext> FsmProbe;
        public BehaviorTreeProbe<DemoBossContext> TreeProbe;
    }

    //状态机与行为树的组装工厂，外加所有招式叶节点的实现。
    //ai[] 槽位分配写死在这里：ai[0] 当前招式 id、ai[1] 招式内计时、ai[3] 归
    //AiSlotNetSync 同步 StateId；ai[2] 空闲。boss 本体 aiStyle = -1，原版 AI
    //不会来抢这些槽。
    //
    //Assembly of the FSM and the attack tree, plus every attack leaf. The ai[]
    //slot map is fixed here: ai[0] current attack id, ai[1] in-attack timer,
    //ai[3] owned by AiSlotNetSync for the StateId; ai[2] is free. The boss sets
    //aiStyle = -1 so vanilla AI never touches these slots.
    internal static class DemoBossAI
    {
        public const int SlotAttackId = 0;
        public const int SlotAttackTimer = 1;

        public const int AttackVolley = 0;
        public const int AttackDash = 1;

        //构建整台状态机。上下文实现了 INpcStateContext，其实可以直接
        //new NpcStateMachine<DemoBossContext>(ctx) 一行拿到"已接好 ai[3] 同步"的
        //机器；这里选择走 Builder，是因为转移关系声明在一处更适合当教材，
        //WithNetSync + AiSlotNetSync.ForNpc 与那个包装类做的事完全相同。
        //
        //Builds the whole machine. Since the context implements INpcStateContext
        //we could just write new NpcStateMachine<DemoBossContext>(ctx) and get a
        //machine pre-wired to ai[3]; we go through the Builder instead because
        //declaring every transition in one chain reads better as teaching
        //material. WithNetSync + AiSlotNetSync.ForNpc is exactly what that
        //wrapper does internally.
        public static VaultStateMachine<DemoBossContext> BuildMachine(DemoBossContext ctx) {
            VaultStateMachine<DemoBossContext> machine = VaultStateMachineBuilder.For(ctx)
                //把 StateId 写进 npc.ai[3]（RecommendedSlot），写入即置 netUpdate，
                //客户端每帧读回并被动对齐——这就是本 Boss 唯一的"状态网络同步"
                //
                //Writes the StateId into npc.ai[3] (RecommendedSlot) and flags
                //netUpdate on write; clients read it back every frame and align
                //passively. This is the boss's entire state network sync.
                .WithNetSync(AiSlotNetSync<DemoBossContext>.ForNpc(c => c.Npc, AiSlotNetSync<DemoBossContext>.RecommendedSlot))
                //Build() 会替我们调用一次 SetInitialState。注意这个方法整个生命周期
                //只允许调一次——CurrentState 非空时第二次调用会被忽略并记错误日志，
                //想重置状态机请用 Restart(newInitial)，不要再来一发 SetInitialState
                //
                //Build() calls SetInitialState for us, exactly once. That method
                //must only ever run once: a second call while CurrentState is set
                //gets ignored with an error log. To reset the machine use
                //Restart(newInitial), never a second SetInitialState.
                .Initial<DemoBossIntroState>()
                .From<DemoBossIntroState>().To<DemoBossAttackState>()
                    .When(c => c.Timer >= 110).Label("intro-done").End()
                .From<DemoBossAttackState>().To<DemoBossRecoverState>()
                    .When(c => c.AttackRoundDone).Label("attack-settled").End()
                //狂暴后休整时间近乎砍半，循环自然提速——"强化循环"不需要复制一套状态
                //
                //After enrage the rest window is nearly halved, so the loop speeds
                //up without duplicating any state classes
                .From<DemoBossRecoverState>().To<DemoBossAttackState>()
                    .When(c => c.Timer >= (c.IsEnraged ? 34 : 62)).Label("rested").End()
                .From<DemoBossEnragedState>().To<DemoBossAttackState>()
                    .When(c => c.Timer >= 70).Label("enrage-burst-done").End()
                //半血一次性阶段触发：任意状态下命中就切入 Enraged，语义上等价
                //AnyState + Once。谓词和 OnFire 只在权威端评估/执行，客户端靠
                //ai[3] 被动进入 Enraged，所以 OnFire 里做的是纯服务端清理
                //
                //One-shot phase trigger at half HP: fires from any state,
                //semantically AnyState + Once. Predicate and OnFire only run on
                //the authoritative side; clients enter Enraged via ai[3], so
                //OnFire is server-side cleanup only.
                .Phase<DemoBossEnragedState>(
                    when: c => c.Npc.life > 0 && c.Npc.life * 2 <= c.Npc.lifeMax,
                    onFire: c => {
                        //打断了进行中的招式，把招式内计时清零并立刻刷一次
                        //netUpdate，客户端拿到的 ai[1] 才不会是半截旧进度
                        //
                        //The trigger may interrupt a move mid-swing; zero the
                        //in-attack timer and push a netUpdate so clients don't
                        //keep half of a stale move timer
                        c.Npc.ai[SlotAttackTimer] = 0f;
                        c.Npc.netUpdate = true;
                    },
                    label: "half-hp-enrage")
                .Build();

            //探针只在有画面的端挂：专用服务器既没有 /vaultdebug 叠层也不需要
            //这份采样。探针纯属演示用途，正式模组建议 #if DEBUG 包起来
            //
            //Probes only attach where there is a screen; a dedicated server has
            //no /vaultdebug overlay. They exist purely for the demo; a shipping
            //mod should wrap them in #if DEBUG.
            if (!Main.dedServ) {
                ctx.FsmProbe = new StateMachineProbe<DemoBossContext>("DemoBoss FSM", machine);
                ctx.TreeProbe = new BehaviorTreeProbe<DemoBossContext>("DemoBoss AttackTree", ctx.AttackTree);
            }
            return machine;
        }

        //攻击树。根 Sequence 先选招再出招；Selector 按 ai[0] 走冲刺分支，
        //Condition 不命中就自然落到兜底的齐射。Running 期间 Sequence/Selector
        //都会记住子节点索引，下一帧直接从出招叶继续，不会反复重选。
        //
        //The attack tree. The root Sequence picks a move, then performs it; the
        //Selector routes to the dash branch by ai[0], and when the Condition
        //fails it falls through to the volley. While a leaf reports Running the
        //composites remember their child index, so the next frame resumes inside
        //the move instead of re-picking.
        public static BTNode<DemoBossContext> BuildAttackTree() {
            return BehaviorTreeBuilder.For<DemoBossContext>()
                .Sequence()
                    .Action(PickAttack)
                    .Selector()
                        .Sequence()
                            .Condition(c => (int)c.Npc.ai[SlotAttackId] == AttackDash)
                            .Action(TickDash)
                        .End()
                        .Action(TickVolley)
                    .End()
                .End()
                .Build();
        }

        //选招叶：权威端轮换 Blackboard 里的计数并把结果写进 ai[0]，netUpdate 把
        //选择捎给客户端；客户端这里什么都不写，直接用服务器发来的 ai[0] 走分支。
        //返回 Success 让根 Sequence 继续推进到出招叶。
        //
        //Pick leaf: the authoritative side rotates the blackboard counter and
        //writes the result into ai[0]; netUpdate ships the choice to clients,
        //which write nothing here and just branch on whatever ai[0] the server
        //sent. Returning Success lets the root Sequence advance to the move.
        private static BTStatus PickAttack(DemoBossContext ctx, Blackboard bb) {
            NPC npc = ctx.Npc;
            //计时非零说明有招式在半路上——例如服务器包刚把进行中的 ai[1] 同步
            //过来——此时只放行、不轮换选招
            //
            //A non-zero timer means a move is still in flight — e.g. a server
            //packet just synced a mid-move ai[1] over — so pass through without
            //rotating the pick
            if (npc.ai[SlotAttackTimer] > 0f) {
                return BTStatus.Success;
            }
            if (!VaultUtils.isClient) {
                int cycle = bb.Get(DemoBossKeys.AttackCycle, 0);
                npc.ai[SlotAttackId] = cycle % 2 == 0 ? AttackVolley : AttackDash;
                bb.Set(DemoBossKeys.AttackCycle, cycle + 1);
                npc.netUpdate = true;
            }
            return BTStatus.Success;
        }

        //扇形齐射：悬在目标侧上方打三轮（狂暴四轮）。每轮前 10 帧有聚能预警，
        //开火瞬间带后坐位移，节奏靠 ai[1] 计时推进。两端都跑移动与特效，
        //但弹幕只在权威端生成——客户端生成的弹幕不会被服务器认账。
        //返回 Success 前必须把 ai[1] 清零：BT 契约要求叶节点结束周期时自我复位。
        //
        //Fan volley: hover beside-above the target and fire three waves (four
        //when enraged). Each wave gets a 10-tick charge-up telegraph and a
        //recoil kick on release; the rhythm runs off the ai[1] timer. Both sides
        //run movement and effects, but projectiles spawn only on the
        //authoritative side. ai[1] must be zeroed before returning Success: the
        //BT contract says a leaf resets itself when its cycle ends.
        private static BTStatus TickVolley(DemoBossContext ctx, Blackboard bb) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            bool enraged = ctx.IsEnraged;

            int firstWave = enraged ? 26 : 36;
            int cadence = enraged ? 20 : 28;
            int waves = enraged ? 4 : 3;
            int lastWave = firstWave + cadence * (waves - 1);
            int duration = lastWave + 16;
            int t = (int)npc.ai[SlotAttackTimer];

            FaceTarget(ctx);
            //待在自己这一侧不横穿目标，加一点纵向浮沉让悬停不像木桩
            //
            //Stay on the current side instead of crossing the target, with a
            //vertical bob so the hover doesn't look like a statue
            float side = npc.Center.X < target.Center.X ? -1f : 1f;
            float bob = (float)Math.Sin(t * 0.11f) * 22f;
            Hover(ctx, new Vector2(side * 240f, -150f + bob), enraged ? 7.5f : 6f, enraged ? 12f : 18f);

            //距下一轮开火的帧数；不在任何一轮的窗口内则保持 MaxValue
            //
            //Frames until the next wave fires; stays MaxValue outside all windows
            int untilNext = int.MaxValue;
            for (int w = 0; w < waves; w++) {
                int fireTick = firstWave + cadence * w;
                if (t <= fireTick) {
                    untilNext = fireTick - t;
                    break;
                }
            }

            Vector2 aimDir = (target.Center + target.velocity * 10f - npc.Center).SafeNormalize(-Vector2.UnitX * side);
            if (untilNext > 0 && untilNext <= 10 && !Main.dedServ) {
                //聚能预警：往"枪口"处收拢的暗影火
                //
                //Charge-up telegraph: shadowflame converging on the muzzle
                Vector2 muzzle = npc.Center + aimDir * 34f;
                Vector2 spawn = muzzle + Main.rand.NextVector2Unit() * 26f;
                Dust d = Dust.NewDustPerfect(spawn, DustID.Shadowflame, (muzzle - spawn) * 0.15f, 120, default, 1.2f);
                d.noGravity = true;
            }

            if (untilNext == 0) {
                //后坐是确定性的（两端同帧同算式），不需要额外同步；位置微小的
                //漂移由服务器周期性的 NPC 同步拉回
                //
                //The recoil is deterministic (same formula, same tick, both
                //sides) so it needs no extra sync; tiny positional drift gets
                //pulled back by the server's periodic NPC sync
                npc.velocity -= aimDir * 2.6f;
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.55f, Pitch = -0.2f }, npc.Center);
                    for (int i = 0; i < 8; i++) {
                        Dust d = Dust.NewDustPerfect(npc.Center + aimDir * 34f, DustID.Shadowflame, aimDir.RotatedByRandom(0.6f) * Main.rand.NextFloat(2f, 5f), 100, default, 1.5f);
                        d.noGravity = true;
                    }
                }
                //弹幕生成 = 权威端专属决策。客户端跑到这里只放特效不发弹，
                //真正的弹体由服务器生成后按弹幕常规流程同步过来
                //
                //Spawning projectiles is authority-only. Clients reaching this
                //point only play effects; the real bolts are spawned by the
                //server and arrive through normal projectile sync.
                if (!VaultUtils.isClient) {
                    int count = enraged ? 7 : 5;
                    float halfArc = enraged ? 0.95f : 0.72f;
                    float speed = enraged ? 5.4f : 4.4f;
                    int damage = enraged ? 28 : 22;
                    for (int i = 0; i < count; i++) {
                        float progress = count == 1 ? 0.5f : i / (float)(count - 1);
                        Vector2 velocity = aimDir.RotatedBy(MathHelper.Lerp(-halfArc, halfArc, progress)) * speed;
                        //ai0 = 1 让弹幕换上狂暴配色
                        //
                        //ai0 = 1 switches the bolt to its enraged tint
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity,
                            ModContent.ProjectileType<DemoBossBolt>(), damage, 1f, Main.myPlayer, enraged ? 1f : 0f);
                    }
                    //netUpdate 触发点：开火帧把后坐后的位置/速度立刻刷给客户端
                    //
                    //netUpdate point: push the post-recoil position/velocity to
                    //clients right on the fire tick
                    npc.netUpdate = true;
                }
            }

            t++;
            npc.ai[SlotAttackTimer] = t;
            if (t >= duration) {
                npc.ai[SlotAttackTimer] = 0f;
                return BTStatus.Success;
            }
            return BTStatus.Running;
        }

        //预警冲刺：先向侧后方拉开距离蓄力（金色聚能 + 锁定音），锁定时把目标
        //位置写进 Blackboard，随后朝锁定点直线冲出再滑行减速。因为冲的是
        //"锁定的旧位置"，玩家在预警期间移动就能躲——这是给读者演示"可反应"
        //设计，不是手下留情。
        //
        //Telegraphed dash: pull away to the rear-side while charging (gold
        //converging dust + a lock chime), stash the target position on the
        //blackboard at lock time, then launch straight at that stale point and
        //glide out. Dashing at the locked position is what makes it dodgeable
        //during the telegraph — a deliberate readability lesson, not mercy.
        private static BTStatus TickDash(DemoBossContext ctx, Blackboard bb) {
            NPC npc = ctx.Npc;
            Player target = ctx.Target;
            bool enraged = ctx.IsEnraged;

            int warmup = enraged ? 26 : 36;
            int duration = warmup + (enraged ? 44 : 52);
            int t = (int)npc.ai[SlotAttackTimer];

            if (t < warmup) {
                FaceTarget(ctx);
                //越接近锁定拉力越小，营造"绷紧弓弦"的顿感
                //
                //Pull strength eases off toward the lock tick, like drawing a
                //bowstring tight
                float side = Math.Sign(npc.Center.X - target.Center.X);
                if (side == 0f) {
                    side = 1f;
                }
                float pull = MathHelper.Lerp(enraged ? 11f : 9f, 2.5f, t / (float)warmup);
                Hover(ctx, new Vector2(side * 300f, -140f), pull, 8f);

                if (!Main.dedServ && t % 2 == 0) {
                    Vector2 spawn = npc.Center + Main.rand.NextVector2Unit() * 46f;
                    Dust d = Dust.NewDustPerfect(spawn, DustID.GoldFlame, (npc.Center - spawn) * 0.12f, 100, default, 1.3f);
                    d.noGravity = true;
                }

                if (t == warmup - 8) {
                    //锁定：两端各自记下"此刻看到的目标位置"。双方视角的细微
                    //偏差无所谓——服务器的冲刺速度会在下面用 netUpdate 覆盖
                    //客户端的本地预测
                    //
                    //Lock: each side records the target position it sees right
                    //now. Tiny per-side differences don't matter — the server's
                    //dash velocity below overrides the client's local guess via
                    //netUpdate.
                    bb.Set(DemoBossKeys.DashAim, target.Center);
                    if (!Main.dedServ) {
                        SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.8f, Pitch = -0.3f }, npc.Center);
                    }
                    //反向一小段"下蹲"，让起跳更有弹性
                    //
                    //A short backwards crouch so the launch reads springy
                    npc.velocity = (target.Center - npc.Center).SafeNormalize(Vector2.UnitX) * -4f;
                }
            }
            else if (t == warmup) {
                Vector2 aim = bb.Get(DemoBossKeys.DashAim, target.Center);
                npc.velocity = (aim - npc.Center).SafeNormalize(-Vector2.UnitY) * (enraged ? 19f : 15.5f);
                //netUpdate 触发点：冲刺起跳帧，权威速度立刻下发
                //
                //netUpdate point: the launch tick, ship the authoritative
                //velocity immediately
                if (!VaultUtils.isClient) {
                    npc.netUpdate = true;
                }
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.7f }, npc.Center);
                    for (int i = 0; i < 14; i++) {
                        Dust d = Dust.NewDustPerfect(npc.Center, DustID.GoldFlame, -npc.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 0.35f), 100, default, 1.6f);
                        d.noGravity = true;
                    }
                }
            }
            else {
                //滑行减速段：不转向，冲过头就是冲过头，给玩家惩罚窗口
                //
                //Glide-out: no steering. Overshooting is the point — it hands
                //the player a punish window.
                npc.velocity *= 0.965f;
                npc.rotation = npc.velocity.X * 0.03f;
                if (!Main.dedServ) {
                    Dust d = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(12f, 20f), DustID.Shadowflame, -npc.velocity * 0.1f, 130, default, 1.2f);
                    d.noGravity = true;
                }
            }

            t++;
            npc.ai[SlotAttackTimer] = t;
            if (t >= duration) {
                //叶节点自清理：计时归零，下次 Tick 等同首次进入
                //
                //Leaf self-cleanup: zero the timer so the next Tick behaves like
                //a first entry
                npc.ai[SlotAttackTimer] = 0f;
                return BTStatus.Success;
            }
            return BTStatus.Running;
        }

        //朝目标附近某个偏移点惯性悬浮，是所有状态共用的移动原语
        //
        //Inertial hover toward an offset around the target; the movement
        //primitive every state shares
        internal static void Hover(DemoBossContext ctx, Vector2 offset, float speed, float inertia) {
            NPC npc = ctx.Npc;
            Vector2 destination = ctx.Target.Center + offset;
            Vector2 desired = (destination - npc.Center).SafeNormalize(Vector2.Zero) * speed;
            npc.velocity = (npc.velocity * inertia + desired) / (inertia + 1f);
            npc.rotation = npc.velocity.X * 0.02f;
        }

        internal static void FaceTarget(DemoBossContext ctx) {
            NPC npc = ctx.Npc;
            npc.direction = ctx.Target.Center.X >= npc.Center.X ? 1 : -1;
            npc.spriteDirection = npc.direction;
        }
    }

    //所有状态的公共基类：VaultState 自带的 Timer 在 OnEnter 归零、OnUpdate 自增，
    //这里把它镜像到上下文，Builder 的 When(...) 才读得到。子类重写 OnUpdate 时
    //先调 base 再写逻辑，镜像就不会断。
    //
    //Shared base for every state: VaultState's built-in Timer resets on OnEnter
    //and ticks in OnUpdate; we mirror it onto the context so the Builder's
    //When(...) conditions can read it. Subclasses call base first in OnUpdate to
    //keep the mirror alive.
    internal abstract class DemoBossState : VaultState<DemoBossContext>
    {
        public override void OnEnter(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnEnter(machine, ctx);
            ctx.Timer = 0;
        }

        public override IVaultState<DemoBossContext> OnUpdate(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnUpdate(machine, ctx);
            ctx.Timer = Timer;
            return null;
        }
    }

    //出场：吼一声，从生成点压向玩家头顶，同时暗影火向体内收拢蓄势。
    //转移条件（Timer >= 110）声明在 Builder 里，这里只负责演出。
    //[VaultState] 把状态登记进注册表拿到 StateId——没有它状态就不会写进 ai[3]，
    //客户端也无从对齐。
    //
    //Intro: roar once, press down toward a point above the player while
    //shadowflame gathers inward. The transition (Timer >= 110) is declared in
    //the Builder; this class is pure performance. [VaultState] registers the
    //state for a StateId — without it nothing gets written to ai[3] and clients
    //cannot align.
    [VaultState(0, typeof(DemoBossContext))]
    internal sealed class DemoBossIntroState : DemoBossState
    {
        public override string StateName => "Intro";

        public override void OnEnter(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnEnter(machine, ctx);
            //音效只属于有画面的端；OnEnter 在服务器和每个客户端都会各跑一次
            //
            //Sounds belong to sides with a screen; OnEnter runs once on the
            //server and once on every client
            if (!Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.6f, Pitch = 0.15f }, ctx.Npc.Center);
            }
        }

        public override IVaultState<DemoBossContext> OnUpdate(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnUpdate(machine, ctx);
            DemoBossAI.FaceTarget(ctx);
            //前段快后段慢地压到头顶，比匀速逼近更有登场感
            //
            //Approach fast then ease off while sinking toward the overhead
            //anchor; reads far more like an entrance than constant speed
            float approach = MathHelper.Lerp(12f, 6f, Timer / 110f);
            DemoBossAI.Hover(ctx, new Vector2(0f, -235f + Timer * 0.4f), approach, 15f);

            if (!Main.dedServ) {
                float radius = MathHelper.Lerp(90f, 10f, Timer / 110f);
                Vector2 spawn = ctx.Npc.Center + Main.rand.NextVector2Unit() * radius;
                Dust d = Dust.NewDustPerfect(spawn, DustID.Shadowflame, (ctx.Npc.Center - spawn) * 0.08f, 120, default, 1.3f);
                d.noGravity = true;

                if (Timer == 92) {
                    //蓄势完成的一记闪光，提示攻击循环即将开始
                    //
                    //A completion flash hinting that the attack loop is about
                    //to begin
                    for (int i = 0; i < 16; i++) {
                        Vector2 vel = (MathHelper.TwoPi / 16f * i).ToRotationVector2() * 3.5f;
                        Dust burst = Dust.NewDustPerfect(ctx.Npc.Center, DustID.Shadowflame, vel, 100, default, 1.6f);
                        burst.noGravity = true;
                    }
                }
            }
            return null;
        }
    }

    //攻击：每帧 tick 行为树，树 settle（无论 Success/Failure）就举旗，Builder
    //在权威端看到旗子切去休整。客户端也 tick 树——移动和特效要靠它——但选招、
    //发弹这些决策都在叶节点内部用 isClient 挡掉了。
    //
    //Attack: tick the behavior tree every frame and raise a flag once it
    //settles (Success or Failure alike); the Builder transition to Recover
    //picks the flag up on the authoritative side. Clients tick the tree too —
    //movement and effects depend on it — while decisions like picking moves and
    //spawning bolts are gated inside the leaves with isClient.
    [VaultState(1, typeof(DemoBossContext))]
    internal sealed class DemoBossAttackState : DemoBossState
    {
        public override string StateName => "Attack";

        public override void OnEnter(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnEnter(machine, ctx);
            ctx.AttackRoundDone = false;
            //狂暴可能把上一轮打断在半路，进场先清计时保证从头出招
            //
            //Enrage may have cut the previous round mid-move; clear the timer on
            //entry so the round starts clean
            ctx.Npc.ai[DemoBossAI.SlotAttackTimer] = 0f;
            ctx.AttackTree.Reset();
        }

        public override IVaultState<DemoBossContext> OnUpdate(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnUpdate(machine, ctx);
            //传 machine.Blackboard 而不是自建实例，FSM 转移条件与 BT 叶读写的
            //才是同一份数据
            //
            //Pass machine.Blackboard instead of a private instance so FSM
            //conditions and BT leaves share the same data
            BTStatus status = ctx.AttackTree.Tick(ctx, machine.Blackboard);
            ctx.TreeProbe?.MarkTicked(status);
            if (status != BTStatus.Running) {
                ctx.AttackRoundDone = true;
            }
            //一律返回 null：本状态的所有出口都声明在 Builder 里。就算在这里
            //返回新状态，客户端也会丢弃返回值（服务端权威），写在 Builder 里
            //则两端行为一目了然
            //
            //Always return null: every exit of this state is declared in the
            //Builder. Even if we returned a state here, clients would discard it
            //(server authority); the Builder keeps both sides' behavior obvious.
            return null;
        }

        public override void OnExit(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnExit(machine, ctx);
            //被打断也好、正常结束也好，把树复位成"下次进入即全新一轮"
            //
            //Interrupted or finished, reset the tree so the next entry starts a
            //brand-new round
            ctx.AttackTree.Reset();
        }
    }

    //休整：短暂悬停喘口气，给玩家输出窗口。狂暴后由 Builder 的转移条件把
    //时长从 62 帧压到 34 帧。
    //
    //Recover: hover and catch a breath, handing the player a damage window.
    //After enrage the Builder condition shortens it from 62 to 34 ticks.
    [VaultState(2, typeof(DemoBossContext))]
    internal sealed class DemoBossRecoverState : DemoBossState
    {
        public override string StateName => "Recover";

        public override IVaultState<DemoBossContext> OnUpdate(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnUpdate(machine, ctx);
            DemoBossAI.FaceTarget(ctx);
            DemoBossAI.Hover(ctx, new Vector2(0f, -190f), 4.5f, 24f);
            if (!Main.dedServ && Timer % 9 == 0) {
                Dust d = Dust.NewDustPerfect(ctx.Npc.Center + Main.rand.NextVector2Circular(24f, 30f), DustID.Shadowflame, -Vector2.UnitY * 0.8f, 150, default, 0.9f);
                d.noGravity = true;
            }
            return null;
        }
    }

    //狂暴演出：半血 PhaseTrigger 一次性切进来。刹停原地聚能，最后猛地一震，
    //然后回到攻击循环——之后的强化全靠 ctx.IsEnraged 读血量，不需要更多状态。
    //
    //Enrage beat: entered once by the half-HP PhaseTrigger. Brake in place,
    //charge up, jolt at the end, then rejoin the attack loop — all later
    //empowerment reads ctx.IsEnraged off HP, no extra states needed.
    [VaultState(3, typeof(DemoBossContext))]
    internal sealed class DemoBossEnragedState : DemoBossState
    {
        public override string StateName => "Enraged";

        public override void OnEnter(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnEnter(machine, ctx);
            if (!Main.dedServ) {
                SoundEngine.PlaySound(SoundID.ForceRoar with { Volume = 0.8f, Pitch = -0.2f }, ctx.Npc.Center);
            }
        }

        public override IVaultState<DemoBossContext> OnUpdate(VaultStateMachine<DemoBossContext> machine, DemoBossContext ctx) {
            base.OnUpdate(machine, ctx);
            DemoBossAI.FaceTarget(ctx);
            ctx.Npc.velocity *= 0.88f;

            if (!Main.dedServ) {
                //收拢的红色火焰随时间变密，压迫感逐渐拉满
                //
                //Converging red flame gets denser over time to build pressure
                int per = Timer < 36 ? 2 : 4;
                for (int i = 0; i < per; i++) {
                    Vector2 spawn = ctx.Npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(40f, 80f);
                    Dust d = Dust.NewDustPerfect(spawn, DustID.RedTorch, (ctx.Npc.Center - spawn) * 0.1f, 100, default, 1.4f);
                    d.noGravity = true;
                }
            }

            if (Timer == 54) {
                //两端按各自计时同帧上跳；细微偏差交给常规 NPC 同步修正
                //
                //Both sides jolt upward on their own timers; tiny offsets are
                //corrected by regular NPC sync
                ctx.Npc.velocity = new Vector2(0f, -4f);
                if (!Main.dedServ) {
                    for (int i = 0; i < 24; i++) {
                        Vector2 vel = (MathHelper.TwoPi / 24f * i).ToRotationVector2() * Main.rand.NextFloat(4f, 7f);
                        Dust burst = Dust.NewDustPerfect(ctx.Npc.Center, DustID.RedTorch, vel, 80, default, 1.8f);
                        burst.noGravity = true;
                    }
                }
            }
            return null;
        }
    }

    //目录条目：行为树这一课指向本文件——树的构建、Running/Success 推进语义
    //和权威端守卫都在这里
    //
    //Catalog entry: the behavior-tree lesson points at this file — tree
    //assembly, Running/Success semantics and authority guards all live here
    internal sealed class DemoBossTreeEntry : IDemoEntry
    {
        public string Id => "DemoBossTree";
        public string Chapter => "AI";
        public string WikiSlug => "ai/behavior-trees";
        public string SourcePath => "Content/AI/DemoBossAI.cs";
        public int SortIndex => 1;
    }
}
