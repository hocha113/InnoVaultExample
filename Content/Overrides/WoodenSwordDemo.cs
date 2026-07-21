using InnoVault.GameSystem;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Overrides
{
    //不新建物品，直接覆写原版木剑：小幅上调伤害，再追加一行提示。
    //这是本章唯一效果常开的覆写，跟透镜门控的其它演示做对照——只改一件具体物品，
    //影响面小，常开无妨；全局性的钩子才需要给玩家留开关。
    //
    //No new item here: we override the vanilla wooden sword directly, bumping its damage
    //a little and appending one tooltip line. It is the only override in this chapter
    //whose effect stays always on, contrasting with the lens-gated demos: reshaping a
    //single item is low-impact enough to stay on, while global hooks should offer an
    //opt-out.
    //
    //https://innovault.wiki/cn/overrides/item/

    internal class WoodenSwordDemo : ItemOverride
    {
        //TargetID 决定注册去向：>0 只挂这一个物品 ID；-1 写进 UniversalInstances，
        //对所有物品生效（通用覆写，慎用）；0（即默认的 ItemID.None）压根不注册。
        //
        //TargetID picks the registration: >0 hooks exactly this item id; -1 goes into
        //UniversalInstances and fires for every item (universal override, use sparingly);
        //0 (ItemID.None, the default) means the class never registers at all.
        public override int TargetID => ItemID.WoodenSword;

        private LocalizedText demoLine;
        private LocalizedText chainCutLine;

        //ItemOverride 实现了 ILocalizedModType，LocalizationCategory 默认是 "RemakeItems"，
        //所以这些键都落在 Mods.InnoVaultExample.RemakeItems.WoodenSwordDemo.* 下面。
        //框架还会自动注册同前缀的 DisplayName / Tooltip 两个键：DisplayName 填上就能
        //给木剑改名（框架钩住了 Item.Name / AffixName）；Tooltip 键虽然注册了出来，
        //但目前没有被应用到提示栏——想改原版描述，走下面的 ModifyTooltips 才作数。
        //
        //ItemOverride is an ILocalizedModType whose LocalizationCategory defaults to
        //"RemakeItems", so these keys live under Mods.InnoVaultExample.RemakeItems.WoodenSwordDemo.*.
        //The framework also auto-registers DisplayName / Tooltip with the same prefix:
        //filling DisplayName renames the sword (the framework hooks Item.Name / AffixName),
        //while the Tooltip key, though registered, is currently never applied to the
        //tooltip panel — to change the vanilla description, ModifyTooltips below is
        //what actually counts.
        public override void SetStaticDefaults() {
            demoLine = this.GetLocalization("DemoLine", () => "ItemOverride: this line comes from WoodenSwordDemo (always on, ignores the lens)");
            chainCutLine = this.GetLocalization("ChainCutLine", () => "On_ModifyTooltips returned false: the tooltip chain below this point was cut");
        }

        //SetDefaults 在每个木剑实例创建时运行，改基础数值的手感跟写 ModItem 一样
        //
        //SetDefaults runs whenever a wooden sword instance is created; tweaking base
        //stats here feels exactly like writing a ModItem.
        public override void SetDefaults(Item item) {
            item.damage = 11;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            //这行经由 InnoVault 自己的 GlobalItem 分发进来，属于"后续链"的一员——
            //所以下面的 On_ 钩子一旦断链，这行会跟着消失，正好拿来观察
            //
            //This line arrives through InnoVault's own GlobalItem dispatch, making it
            //part of "the rest of the chain" — so when the On_ hook below cuts the
            //chain, this line vanishes with it, which is exactly what we want to show.
            tooltips.Add(new TooltipLine(Mod, "WoodenSwordDemoLine", demoLine.Value));
        }

        //On_ 前缀的钩子握着整条钩子链的闸门，返回值读作：
        //null —— 不干预，链原样走完（ModItem 的钩子和所有 GlobalItem 的钩子照跑）；
        //true —— 只放行原 ModItem 的钩子，拦掉全部 GlobalItem 的额外修改；
        //false —— 从这里断链，后面谁都不跑。
        //开着透镜时这里返回 false：上面 ModifyTooltips 加的那行随即消失，
        //因为它正是从 GlobalItem 那条路进来的。
        //
        //On_-prefixed hooks hold the gate of the whole hook chain. Read the result as:
        //null — hands off, the chain runs in full (the ModItem hook plus every GlobalItem);
        //true — let only the original ModItem hook through, block GlobalItem extras;
        //false — cut the chain right here, nothing after us runs.
        //With the lens on we return false: the line added in ModifyTooltips above
        //disappears immediately, since it enters through the GlobalItem route.
        public override bool? On_ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            if (!LensPlayer.LocalLensOn) {
                return null;
            }
            tooltips.Add(new TooltipLine(Mod, "WoodenSwordChainCut", chainCutLine.Value));
            return false;
        }
    }
}
