using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Terraria;

namespace InnoVaultExample.Content.Particles
{
    //GlobalPRT 之于粒子相当于 GlobalNPC 之于 NPC：继承即生效，能旁观或拦截全局系统里
    //所有粒子。钩子一共五个——OnSpawn（逐粒子，入场时）、PreUpdatePRTAll / PostUpdatePRTAll
    //（每帧各一次，Pre 返回 false 可拦下全部更新）、PreDrawPRT / PostDrawPRT（逐粒子，
    //Pre 返回 false 可拦下该粒子的默认绘制）。InnoVault 只把"实际重写了某钩子"的实例
    //挂进对应调用列表（见源码 VaultHookMethodCache），所以只写 OnSpawn 不会给更新和
    //绘制热路径添任何开销。
    //
    //GlobalPRT is to particles what GlobalNPC is to NPCs: inherit and it's live,
    //watching or intercepting every particle in the global system. Five hooks:
    //OnSpawn (per particle, on entry), PreUpdatePRTAll / PostUpdatePRTAll (once
    //per frame; Pre returning false blocks the whole update), and PreDrawPRT /
    //PostDrawPRT (per particle; Pre returning false blocks that particle's
    //default draw). InnoVault only enlists instances that actually override a
    //hook (see VaultHookMethodCache in the source), so overriding just OnSpawn
    //adds zero cost to the update and draw hot paths.
    //
    //https://innovault.wiki/cn/content/prt/
    internal class GlobalSparkTint : GlobalPRT
    {
        public override void OnSpawn(BasePRT prt) {
            //全局钩子看得到所有模组的粒子，动别人家的东西容易造成莫名其妙的联动，
            //所以先用类型过滤把影响范围锁死在本模组的两种粒子上——这也是 GlobalPRT
            //保持低侵入的基本礼貌。
            //
            //A global hook sees every mod's particles, and touching someone
            //else's tends to cause baffling interactions. The type filter locks
            //the effect to this mod's two particle types — basic manners for a
            //low-invasiveness GlobalPRT.
            if (prt is not (BasicSpark or StarBurst)) {
                return;
            }

            //夜里把暖色火花往月光色偏一点。挑 OnSpawn 做这件事是因为它一颗粒子只跑一次，
            //改完颜色就退场，比在逐帧钩子里反复插手便宜得多。
            //
            //At night, nudge the warm sparks toward moonlight. OnSpawn is the
            //right place because it runs once per particle: adjust the color and
            //step aside, far cheaper than meddling in a per-frame hook.
            if (!Main.dayTime) {
                prt.Color = Color.Lerp(prt.Color, new Color(140, 170, 255), 0.4f);
            }

            //注意 OnSpawn 只覆盖全局系统的入场路径（PRTLoader.AddParticle / NewParticle）。
            //火花剑右键那圈粒子走的是 PRTGroup 本地池，不经过这里，所以夜里依旧是原色——
            //拿这个对比正好能看清两条路径的边界。
            //
            //Note that OnSpawn only covers the global entry path
            //(PRTLoader.AddParticle / NewParticle). The ring from the sword's
            //right click lives in a PRTGroup local pool and never passes through
            //here, so it keeps its color at night — a handy side-by-side view of
            //where the two paths part ways.
        }
    }
}
