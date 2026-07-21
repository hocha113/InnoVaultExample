using InnoVault.BehaviorTrees;
using InnoVault.StateMachines;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.AI
{
    //秘库看守者的 ModNPC 本体：属性、贴图、目标选择和每帧驱动状态机，
    //脑子全在 DemoBossAI.cs。用 BossEmblem 召唤，半血狂暴，打死掉一把坠落之星。
    //
    //The Vault Warden's ModNPC shell: stats, sprite, targeting, and the per-tick
    //FSM pump. All the thinking lives in DemoBossAI.cs. Summon it with the
    //BossEmblem; it enrages at half HP and drops fallen stars on death.
    //
    //https://innovault.wiki/cn/ai/state-machines/

    internal class DemoBoss : ModNPC
    {
        private DemoBossContext _context;
        private VaultStateMachine<DemoBossContext> _machine;

        //这只 Boss 直接借原版幽灵（Wraith）的贴图，不单独画图
        //
        //The boss borrows the vanilla Wraith sheet rather than bringing its own
        public override string Texture => "Terraria/Images/NPC_" + NPCID.Wraith;

        public override void SetStaticDefaults() {
            //帧数直接抄原版 Wraith 的登记值，不在这里硬编码一个手数出来的数字
            //
            //Copy the vanilla Wraith frame count instead of hardcoding a
            //hand-counted number here
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Wraith];
            //BossEmblem 的多人召唤走 SpawnBossUseLicenseStartEvent 请求，服务器
            //只替这份白名单里的 NPC 代为生成——漏掉这行，多人下的请求会被静默丢弃
            //
            //The BossEmblem's multiplayer summon goes through a
            //SpawnBossUseLicenseStartEvent request, and the server only spawns
            //NPCs on this whitelist — drop this line and the request is
            //silently discarded in multiplayer.
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults() {
            NPC.width = 46;
            NPC.height = 62;
            NPC.scale = 1.35f;
            NPC.lifeMax = 3000;
            NPC.defense = 10;
            NPC.damage = 32;
            NPC.knockBackResist = 0f;
            //aiStyle = -1 是整套玩法的前提：原版 AI 不再碰 ai[]，四个槽位全归
            //我们支配——ai[0] 招式 id、ai[1] 招式计时、ai[3] 给 AiSlotNetSync
            //同步 StateId，ai[2] 空闲备用
            //
            //aiStyle = -1 underpins everything: vanilla AI never touches ai[],
            //so all four slots are ours — ai[0] attack id, ai[1] attack timer,
            //ai[3] reserved by AiSlotNetSync for the StateId, ai[2] spare.
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.HitSound = SoundID.NPCHit54;
            NPC.DeathSound = SoundID.NPCDeath52;
            NPC.value = Item.buyPrice(gold: 2);
            Music = MusicID.Boss1;
        }

        public override void AI() {
            //惰性构建而不是放 OnSpawn：OnSpawn 只在生成端（服务器/单机）执行，
            //多人客户端是通过网络收到这只 NPC 的，同样需要一台本地状态机去
            //跑移动与特效。AI() 在两端每帧都会进来，第一帧建机对谁都成立。
            //
            //Lazy init instead of OnSpawn: OnSpawn only runs on the spawning
            //side, yet multiplayer clients that receive this NPC over the wire
            //need a local machine for movement and effects too. AI() runs on
            //every side, so building on the first tick works everywhere.
            if (_machine == null) {
                _context = new DemoBossContext(NPC);
                _machine = DemoBossAI.BuildMachine(_context);
            }

            if (!NPC.HasValidTarget || _context.Target.dead) {
                NPC.TargetClosest();
            }
            if (!NPC.HasValidTarget || _context.Target.dead) {
                //没有活着的目标：向上飘走并请求消失。状态机这几帧不再驱动，
                //探针也先摘掉，免得叠层里留着一台永远停在旧状态的机器
                //
                //No living target: drift upward and ask to despawn. The FSM
                //stops ticking during these frames, and the probes come off so
                //the overlay doesn't keep a machine frozen in its last state.
                NPC.velocity = new Vector2(NPC.velocity.X * 0.96f, NPC.velocity.Y - 0.25f);
                NPC.EncourageDespawn(30);
                DetachProbes();
                return;
            }

            //整台 FSM 每帧就推这一下。服务端在这里评估转移并经 ai[3] 下发，
            //客户端在同一调用里读 ai[3] 被动对齐，然后各自执行 OnUpdate 的演出
            //
            //One pump per tick. The server evaluates transitions here and ships
            //them via ai[3]; clients read ai[3] back inside the same call, then
            //both sides run the OnUpdate performance locally.
            _machine.Update();

            if (!Main.dedServ) {
                Lighting.AddLight(NPC.Center, 0.5f, 0.35f, 0.8f);
            }
        }

        public override void FindFrame(int frameHeight) {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 7) {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type]) {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override void HitEffect(NPC.HitInfo hit) {
            //HitEffect 在所有有画面的端都会被调，是死亡尘埃的正确位置；
            //OnKill 只在服务端/单机跑，放那儿客户端就看不见了
            //
            //HitEffect runs on every side with a screen, which makes it the
            //right home for death dust; OnKill is server-only, so effects
            //placed there would be invisible to clients.
            if (Main.dedServ) {
                return;
            }
            if (NPC.life <= 0) {
                for (int i = 0; i < 36; i++) {
                    Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, hit.HitDirection, -1.5f, 100, default, 1.6f);
                    d.noGravity = true;
                }
                DetachProbes();
            }
        }

        public override void OnKill() {
            DetachProbes();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ItemID.FallenStar, minimumDropped: 5, maximumDropped: 10));
        }

        //调试探针注册在全局静态表上，Boss 离场必须解注册，否则 /vaultdebug
        //叠层会一直显示这台已经没有宿主的状态机，直到模组卸载才被清掉
        //
        //The debug probes live on a global static registry, so they must be
        //detached when the boss leaves; otherwise the /vaultdebug overlay keeps
        //showing an orphaned machine until the mod unloads.
        private void DetachProbes() {
            if (_context == null) {
                return;
            }
            if (_context.FsmProbe != null) {
                StateMachineDebugger.Detach(_context.FsmProbe);
                _context.FsmProbe = null;
            }
            if (_context.TreeProbe != null) {
                BehaviorTreeDebugger.Detach(_context.TreeProbe);
                _context.TreeProbe = null;
            }
        }
    }

    //目录条目：状态机这一课从 Boss 本体入手，召唤物由目录 UI 直接发放
    //
    //Catalog entry: the state-machine lesson starts from the boss shell, and
    //the catalog UI hands out the summon item directly
    internal sealed class DemoBossEntry : IDemoEntry
    {
        public string Id => "DemoBoss";
        public string Chapter => "AI";
        public string WikiSlug => "ai/state-machines";
        public string SourcePath => "Content/AI/DemoBoss.cs";
        public int DemoItemType => ModContent.ItemType<BossEmblem>();
        public int SortIndex => 0;
    }
}
