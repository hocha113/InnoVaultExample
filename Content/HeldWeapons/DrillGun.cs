using InnoVault.GameSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.HeldWeapons
{
    //本章的枪械物品。物品本体被刻意做"空"：不画枪、不出子弹，Shoot 只负责把
    //DrillHeld 手持弹幕放出来，之后开火、换弹、姿态全部由手持弹幕接管。
    //搭配的 DrillGunHoldAnimation 演示 AimedHoldAnimation 的持握与后座参数。
    //
    //The gun item for this chapter, deliberately hollow: it draws nothing and
    //fires nothing itself. Shoot only releases the DrillHeld projectile, which
    //then owns firing, reloading and pose. The companion DrillGunHoldAnimation
    //shows the AimedHoldAnimation hold/recoil knobs.
    //
    //https://innovault.wiki/cn/content/base-held-proj/
    internal class DrillGun : ModItem
    {
        //贴图借用原版米尼鲨，这一章的重点在行为不在美术
        //
        //Borrowing the vanilla Minishark sprite; this chapter is about behavior,
        //not art.
        public override string Texture => "Terraria/Images/Item_98";

        public override void SetDefaults() {
            Item.damage = 16;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 40;
            Item.height = 24;
            Item.useTime = Item.useAnimation = 13;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(silver: 80);
            Item.rare = ItemRarityID.Green;
            Item.autoReuse = true;
            Item.noMelee = true;

            //noUseGraphic 藏掉使用中的物品贴图——枪身改由 DrillHeld 在弹幕层绘制，
            //这样旋转、后座、枪口位置都归手持弹幕一处管
            //
            //noUseGraphic hides the in-use item sprite; DrillHeld draws the gun
            //on the projectile layer instead, so rotation, recoil and muzzle
            //position live in one place.
            Item.noUseGraphic = true;

            //shoot/shootSpeed 在这里只当"配置数据"用：Shoot 生成的是手持弹幕，
            //真正的钻头镖由 DrillHeld 开火时读取 Item.shootSpeed 手动生成
            //
            //shoot/shootSpeed act as configuration only: Shoot spawns the held
            //projectile, and DrillHeld reads Item.shootSpeed when it fires the
            //actual bolts.
            Item.shoot = ModContent.ProjectileType<DrillHeld>();
            Item.shootSpeed = 11f;
        }

        //手持弹幕活着的时候禁止再次使用物品，否则每次点击都会叠一把新枪。
        //NewProjectile 会立刻累加 ownedProjectileCounts，所以本 tick 内就能挡住连点。
        //
        //Refuse further item use while the held projectile is alive, or every
        //click would stack another gun. NewProjectile bumps
        //ownedProjectileCounts immediately, so even same-tick double clicks
        //are caught.
        public override bool CanUseItem(Player player)
            => player.ownedProjectileCounts[ModContent.ProjectileType<DrillHeld>()] <= 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            //Shoot 只在物品主人的客户端执行，所以这里的 NewProjectile 天然带
            //正确的 owner；再查一次计数是对 CanUseItem 的兜底
            //
            //Shoot runs only on the item owner's client, so NewProjectile here
            //naturally carries the right owner; the extra count check backs up
            //CanUseItem.
            int heldType = ModContent.ProjectileType<DrillHeld>();
            if (player.ownedProjectileCounts[heldType] <= 0) {
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, heldType, damage, knockback, player.whoAmI);
            }

            //返回 false 拦掉原版发射——否则原版还会按 Item.shoot 再生成一份
            //
            //Return false to cut vanilla shooting, which would otherwise spawn
            //Item.shoot a second time.
            return false;
        }
    }

    //钻头枪的使用动画：瞄准持握 + 开火后座。一个物品 ID 只能绑一个动画
    //（ByID 是一对一映射，不像 ItemOverride 那样可以多个实例叠加），动画激活时
    //ModItem/GlobalItem/ItemOverride 的 UseStyle 与 UseItemFrame 整链被跳过，
    //所以别再在物品类里写姿态逻辑，要共存就用门控或 ItemOverride.On_UseStyle。
    //
    //Use animation for the drill gun: aimed hold plus firing recoil. One item
    //id binds exactly one animation (ByID is a one-to-one map, unlike the
    //stacking ItemOverride registry), and while the animation is active the
    //whole UseStyle/UseItemFrame chain of ModItem, GlobalItem and ItemOverride
    //is skipped — don't write pose logic in the item class too; coexist via
    //the gates below or ItemOverride.On_UseStyle.
    //
    //https://innovault.wiki/cn/overrides/item-use-animation/
    internal class DrillGunHoldAnimation : AimedHoldAnimation
    {
        public override int TargetID => ModContent.ItemType<DrillGun>();

        //持握距离会被 DrillHeld 读走用来摆枪身，改这一个值，手臂和枪一起动。
        //由于物品设了 noUseGraphic，HoldOrigin/HoldSpriteSize 这类只作用于物品
        //贴图锚点的参数在这里看不出效果，就不覆写了——画物品本体的武器才需要它们。
        //
        //DrillHeld reads HoldDistance back to place the gun body, so tuning
        //this one value moves arm and gun together. Because the item uses
        //noUseGraphic, knobs that only affect the item-sprite anchor
        //(HoldOrigin/HoldSpriteSize) would be invisible here, so they stay at
        //their defaults — weapons that draw the item itself are what need them.
        public override float HoldDistance => 20f;

        //后座：开火瞬间沿瞄准方向回退 7 像素，前 40% 使用进度内弹回
        //
        //Recoil: kick 7 pixels back along the aim on firing, recovering within
        //the first 40% of the use progress.
        public override float RecoilStrength => 7f;
        public override float RecoilPhase => 0.4f;

        //起手摆动作用在前臂旋转上，给每一发一个小小的抬枪动作
        //
        //The swing rides on the forearm rotation, giving each shot a tiny
        //muzzle-lift gesture.
        public override float SwingStrength => 0.1f;
        public override float SwingPhase => 0.35f;

        //Active 是总开关（比如给其他模组让位时整个动画退场），ShouldAnimate 管
        //使用分支——这里只在手持弹幕活着时接管姿态，避免枪已消失手臂还端着
        //
        //Active is the master switch (e.g. yielding to another mod entirely);
        //ShouldAnimate gates per use branch — here the pose is only taken over
        //while the held projectile is alive, so the arm doesn't keep aiming
        //after the gun is gone.
        public override bool ShouldAnimate(Item item, Player player)
            => player.ownedProjectileCounts[ModContent.ProjectileType<DrillHeld>()] > 0;

        //换弹期间使用进度会重新走一遍，若不关掉后座，枪会在换弹开始时莫名后跳
        //
        //Reloading restarts the use progress; without turning recoil off the
        //gun would inexplicably kick when the reload begins.
        public override bool RecoilActive(Item item, Player player)
            => !DrillHeld.IsReloading(player);
    }
}
