using InnoVault;
using InnoVault.GameSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace InnoVaultExample.Content.Overrides
{
    //覆写原版蓝史莱姆：挨打后短暂进入"被标记"状态，透镜开着时能看到金色粒子和浮字。
    //重点不在效果，在数据流——[SyncVar] 字段只在权威端写入，由框架自动送达每个客户端。
    //
    //Overrides the vanilla blue slime: after taking a hit it is briefly "marked", and
    //with the lens on you see gold sparkles and a combat text. The point is the data
    //flow rather than the effect — the [SyncVar] field is written on the authority only,
    //and the framework carries it to every client.
    //
    //https://innovault.wiki/cn/overrides/npc/

    internal class SlimeMarkOverride : NPCOverride
    {
        public override int TargetID => NPCID.BlueSlime;

        //[SyncVar] 字段走 NPCOverride 的 Event 通道：服务端调用 RequestSync(SyncVar) 后，
        //SyncVarManager 把打了标记的字段序列化广播，各客户端把值写回自己端上绑定
        //同一只 NPC 的克隆实例；中途加入的玩家由框架的 BulkRequest 自动补发，
        //不需要额外处理。
        //
        //[SyncVar] fields ride NPCOverride's event channel: once the server calls
        //RequestSync(SyncVar), SyncVarManager serializes the attributed fields into the
        //broadcast and each client writes the values back onto its own clone bound to
        //the same NPC; late joiners receive a BulkRequest refill from the framework
        //with no extra work here.
        [SyncVar]
        public bool Marked;

        //倒计时和血量水印只对权威端有意义，所以不挂 [SyncVar]——只同步别人看得见的状态
        //
        //The countdown and the life watermark only matter to the authority, so they get
        //no [SyncVar]: sync only what other ends can actually observe.
        private int markTimer;
        private int lastLife;
        private bool markedLastFrame;

        //NPCOverride 按 NPC 克隆实例，SetProperty 在克隆绑定后跑一次，适合做字段初始化
        //
        //NPCOverride clones one instance per NPC; SetProperty runs once after the clone
        //binds, the right place to seed instance fields.
        public override void SetProperty() {
            lastLife = npc.life;
        }

        public override bool AI() {
            //受击类钩子（On_OnHitByItem 等）在多人下跑在攻击方客户端，不适合当权威写入点；
            //改在 AI 里盯血量变化——NPC 掉血一定会反映到服务端。isServer || isSinglePlayer
            //的内联判断就是"我是权威端"，只有权威端才允许改 Marked。
            //
            //Hit hooks (On_OnHitByItem and friends) run on the attacker's client in
            //multiplayer, so they are the wrong place for authoritative writes; instead
            //we watch the life value inside AI, since damage always reaches the server.
            //The inline isServer || isSinglePlayer check reads as "I am the authority",
            //and only the authority may touch Marked.
            if (VaultUtils.isServer || VaultUtils.isSinglePlayer) {
                if (npc.life < lastLife && npc.life > 0) {
                    markTimer = 180;
                    if (!Marked) {
                        Marked = true;
                        RequestSync(NPCOverrideSyncField.SyncVar);
                    }
                }
                lastLife = npc.life;

                if (Marked && --markTimer <= 0) {
                    Marked = false;
                    RequestSync(NPCOverrideSyncField.SyncVar);
                }
            }

            //视觉留在客户端，并且吃透镜门控：标记状态照样同步，只是不开透镜就不画。
            //多人下客户端同样会本地模拟 NPC 的 AI，所以这段每帧都有机会执行。
            //
            //Visuals stay client-side and respect the lens gate: the mark still syncs,
            //it just is not drawn without the lens. Multiplayer clients simulate NPC AI
            //locally too, so this block gets its chance every tick on each client.
            if (!Main.dedServ && LensPlayer.LocalLensOn) {
                if (Marked && !markedLastFrame) {
                    CombatText.NewText(npc.Hitbox, Color.Gold, Language.GetTextValue("Mods.InnoVaultExample.Overrides.SlimeMarked"));
                }
                if (Marked && Main.rand.NextBool(2)) {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.GoldCoin, 0f, -1f);
                    dust.noGravity = true;
                }
            }
            markedLastFrame = Marked;

            //返回 true 继续原版史莱姆 AI；返回 false 会把原版 AI 一并拦掉，那就得自己写跳跃了
            //
            //Return true to keep the vanilla slime AI; false would block it outright and
            //leave us reimplementing the hop by hand.
            return true;
        }
    }
}
