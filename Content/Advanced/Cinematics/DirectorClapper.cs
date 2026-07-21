using InnoVault;
using InnoVault.Cinematics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Advanced.Cinematics
{
    //DemoCutscene 的触发物品：使用后让导演器播放那段运镜。Cinematics 是
    //纯客户端演出，只动镜头、锁输入，不碰任何游戏状态，所以这里没有网络包，
    //也没有东西需要存档。贴图借用原版望远镜，凑合当摄影器材。
    //https://innovault.wiki/cn/advanced/cinematics/
    //
    //Trigger item for DemoCutscene: using it asks the director to roll the shot.
    //Cinematics is a client-only show — it moves the camera and locks input but
    //touches no game state, so there are no packets here and nothing to save.
    //The vanilla binoculars sprite stands in as camera gear.
    //https://innovault.wiki/en/advanced/cinematics/
    internal sealed class DirectorClapper : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.Binoculars;

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 16;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Camera;
        }

        public override bool? UseItem(Player player) {
            //服务器直接返回：Cinematics 不做任何游戏状态变更，服务器上没有镜头
            //可言。这层守卫与 CutsceneDirector.Play 内部的第一道检查一致——
            //isServer 时 Play 本来就直接返回 false；提前拦一道只是把"演出是
            //纯客户端的事"在调用端写明白
            //
            //Bail out on the server: Cinematics never changes game state, and a
            //server has no camera to speak of. This mirrors the first check inside
            //CutsceneDirector.Play, which returns false outright under isServer —
            //the early return just states the client-only contract at the call site
            if (VaultUtils.isServer) {
                return true;
            }

            //远程客户端会模拟别人的使用动画，但镜头只跟着按下使用键的那个人走。
            //导演器不发网络包，其他玩家看到的只是你举了下望远镜
            //
            //Remote clients simulate other players' use animations, but the camera
            //follows only whoever pressed use. The director sends no packets; all
            //other players see is you raising a pair of binoculars
            if (player.whoAmI != Main.myPlayer) {
                return true;
            }

            //restartSameClip: false——演出还在播时再按一次不会从头重放。
            //Play 在被更高优先级演出压住或 CanPlay 拒绝时会返回 false，
            //这段演示对此无事可做，就不接返回值了
            //
            //restartSameClip: false means pressing again mid-show won't rewind it.
            //Play also returns false when a higher-priority clip is running or
            //CanPlay refuses; this demo has nothing to do about either, so the
            //return value goes unread
            CutsceneDirector.Play<DemoCutscene>(player, restartSameClip: false);
            return true;
        }
    }
}
