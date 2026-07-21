using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Overrides
{
    //Override 大多是全局钩子，一直开着会骚扰正常游玩，所以本章的演示统一收在这枚透镜后面：
    //使用一次切换开关，开着时才能看到玩家、弹幕、物块、场景、菜单、尘埃这些覆写效果。
    //木剑的 ItemOverride 是唯一的例外，它保持常开，用来对照"常开覆写"和"门控覆写"两种做法。
    //
    //Most overrides are global hooks, and leaving them always on would intrude on normal
    //play, so this chapter funnels every demo behind this lens: use it to toggle, and the
    //player/projectile/tile/scene/menu/dust overrides only show while it is on. The wooden
    //sword ItemOverride is the deliberate exception that stays always-on for contrast.
    //
    //https://innovault.wiki/cn/overrides/

    internal class OverrideLens : ModItem
    {
        //直接借用原版透镜的贴图，开关物品犯不着单独画一张图
        //
        //Borrow the vanilla Lens sprite; a toggle item does not warrant its own art.
        public override string Texture => $"Terraria/Images/Item_{ItemID.Lens}";

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 10);
        }

        public override bool? UseItem(Player player) {
            //物品使用在多人下会在各端重放，必须锁定 Main.myPlayer，
            //否则别人举一下透镜也会拨动你的开关
            //
            //Item use replays on every end in multiplayer; lock to Main.myPlayer,
            //or someone else raising their lens would flip your toggle too.
            if (player.whoAmI == Main.myPlayer) {
                player.GetModPlayer<LensPlayer>().Toggle();
            }
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.Lens)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    //透镜的开关状态。这是本地玩家的个人偏好，刻意不做任何同步：它门控的全是本端呈现
    //（尘埃染色、音乐、提示、自己的重力手感），别人的游戏不受影响，服务端也没有逻辑读它。
    //状态同样不落盘，每次进世界都从关闭开始。真正需要跨端同步的状态见 SlimeMarkOverride
    //里的 [SyncVar]。
    //
    //Lens state. It is a personal preference of the local player and deliberately never
    //synced: everything it gates is presentation on this end (dust tint, music, hints,
    //your own gravity feel); nobody else's game changes and no server logic reads it.
    //It is not saved either — every world visit starts with the lens off. For state that
    //genuinely must cross the wire, see [SyncVar] in SlimeMarkOverride.
    internal class LensPlayer : ModPlayer
    {
        public bool LensOn;

        //标题界面没有玩家实例，Main.LocalPlayer 在那里不可用，MenuOverride 只能读静态状态。
        //每次切换时在这里镜像一份：开着透镜退回标题，就能看到菜单角标。
        //
        //The title screen has no player instance, so MenuOverride can only read statics.
        //The toggle mirrors itself here: leave the lens on, quit to title, badge appears.
        public static bool MenuBadgeOn;

        //LensOn 不落盘，每次进世界都从关闭开始，静态镜像得跟着回到同一起点——
        //不然上一局留下的角标会在这一局退回标题时冒出来
        //
        //LensOn is never saved and every world visit starts with it off; the static
        //mirror must reset to the same point, or a badge left over from the previous
        //session would pop up when this one quits to title.
        public override void OnEnterWorld() => MenuBadgeOn = false;

        //静态状态在卸载时归位是 tML 的老规矩，哪怕只是一个 bool
        //
        //Resetting static state on unload is standard tML hygiene, even for a lone bool.
        public override void Unload() => MenuBadgeOn = false;

        //游戏内的门控都走这个入口；专用服务器和加载期直接短路成 false
        //
        //All in-game gates read through this; it short-circuits to false on the
        //dedicated server and while nothing is loaded yet.
        public static bool LocalLensOn
            => !Main.dedServ
            && Main.LocalPlayer != null
            && Main.LocalPlayer.active
            && Main.LocalPlayer.TryGetModPlayer(out LensPlayer lens)
            && lens.LensOn;

        public void Toggle() {
            LensOn = !LensOn;
            MenuBadgeOn = LensOn;
            SoundEngine.PlaySound(LensOn ? SoundID.Unlock : SoundID.MenuClose);
            string key = LensOn ? "Mods.InnoVaultExample.Overrides.LensOn" : "Mods.InnoVaultExample.Overrides.LensOff";
            Main.NewText(Language.GetTextValue(key), LensOn ? Color.LightSkyBlue : Color.Gray);
        }
    }
}
