using InnoVault.Cinematics;
using InnoVault.GameSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.HeldWeapons
{
    //一把普普通通的近战刀，物品类里没有任何姿态代码——挥舞的弧度、缓动、
    //缩放全部由下面的 MeleeSwingAnimation 子类在 IL 层接管。对比原版木剑
    //挥一下就能看出差别。
    //
    //A perfectly ordinary melee blade with zero pose code in the item class —
    //arc, easing and scale of the swing are all taken over at the IL layer by
    //the MeleeSwingAnimation subclass below. Swing a vanilla wooden sword
    //side by side to feel the difference.
    //
    //https://innovault.wiki/cn/overrides/item-use-animation/
    internal class SwingBlade : ModItem
    {
        public override string Texture => "InnoVaultExample/Assets/Items/StarFireSword";

        public override void SetDefaults() {
            Item.damage = 46;
            Item.DamageType = DamageClass.Melee;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(gold: 1, silver: 20);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.useTurn = true;

            //item.scale 决定原版怎么画、怎么算判定，动画的 ItemScale 决定锚点
            //推多远，两边取同一个数值刀尖才不会脱手
            //
            //item.scale drives vanilla drawing and hitbox math while the
            //animation's ItemScale drives how far the anchor is pushed out;
            //keep the two equal or the blade tip drifts off the hand.
            Item.scale = 1.15f;
        }
    }

    //刀的挥舞动画。关键约束：一个物品 ID 只能绑定一个动画——ByID 是一对一的表，
    //后注册的会顶掉先注册的，所以别为同一把武器写两个动画类，把分支逻辑并进
    //一个类里用 ShouldAnimate 区分。动画通过 CanRun（Active 且 ShouldAnimate）
    //生效时，TML 的整条姿态链（ModItem.UseStyle、GlobalItem、包括
    //ItemOverride.UseStyle/UseItemFrame）都被跳过——这是设计出的互斥：多套系统
    //同时写 itemRotation 只会互相打架。需要与 ItemOverride 配合时，用它的
    //On_UseStyle 在动画之前拦截，或者在这里重写 Active 主动让位。
    //
    //The blade's swing animation. The key constraint: one item id binds ONE
    //animation — ByID is a one-to-one table and a later registration evicts
    //the earlier one, so never write two animation classes for one weapon;
    //fold branches into a single class and split them with ShouldAnimate.
    //While the animation runs (CanRun = Active && ShouldAnimate), the whole
    //TML pose chain is skipped — ModItem.UseStyle, GlobalItem, and also
    //ItemOverride.UseStyle/UseItemFrame. That exclusivity is by design:
    //several systems writing itemRotation at once can only fight. To
    //cooperate with an ItemOverride, intercept earlier via its On_UseStyle,
    //or override Active here and step aside.
    internal class SwingBladeAnimation : MeleeSwingAnimation
    {
        //TargetID 大于 0 即自动注册；保持 ItemID.None 则这个类完全不生效
        //
        //Any TargetID above 0 auto-registers; leave it at ItemID.None and the
        //class is inert.
        public override int TargetID => ModContent.ItemType<SwingBlade>();

        //弧度拉到 1.3π，比原版半圈多出一大截，挥起来有抡的感觉
        //
        //Stretch the arc to 1.3π — well past vanilla's half circle, so the
        //swing reads as a real haul.
        public override float SwingArc => MathHelper.Pi * 1.3f;

        //三次方先加速后减速：起手有蓄力感，收尾不生硬。挥砍进度要用
        //GetAnimationProgress（itemAnimation 口径），持握后座才用 GetUseProgress
        //
        //Cubic in-out: a wind-up at the start, no abrupt stop at the end.
        //Swings track GetAnimationProgress (the itemAnimation clock);
        //GetUseProgress is for hold/recoil styles instead.
        public override CutsceneEase SwingEase => CutsceneEase.CubicInOut;

        //与 Item.scale 配对，见物品类里的说明
        //
        //Paired with Item.scale; see the note in the item class.
        public override float ItemScale => 1.15f;

        //FlipAttackEachSwing 默认开启，连击自动左右交替，这里显式写出便于对照。
        //Active/ShouldAnimate 保持默认 true：这把刀任何时候都用这套动画；
        //需要按分支跳过（如右键蓄力）时重写 ShouldAnimate 即可。
        //还有个这里没用的扩展点 AnimateLegs：打开后挥砍期间每帧回调
        //AnimateLegFrame（默认空实现），想让下盘跟着发力就重写它
        //
        //FlipAttackEachSwing defaults to on, alternating combo directions;
        //spelled out here for reference. Active/ShouldAnimate keep their
        //default true — this blade always uses the animation; override
        //ShouldAnimate to skip specific branches (a charged right click, say).
        //One extension point goes unused here: AnimateLegs, which when enabled
        //calls AnimateLegFrame (an empty default) every frame of the swing —
        //override it to put the lower body into the motion.
        public override bool FlipAttackEachSwing => true;
    }
}
