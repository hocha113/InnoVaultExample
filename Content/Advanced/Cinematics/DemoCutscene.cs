using InnoVault;
using InnoVault.Cinematics;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Advanced.Cinematics
{
    //一段五秒的纯运镜过场：镜头缓动飞向玩家前上方的远点，拉近端详，震一下，
    //再平滑飞回来，全程锁输入、途中掉一句本地聊天字幕。时间轴在 VaultSetup
    //构建一次后缓存，轨道写的全是相对玩家的坐标，所以在任何地图任何位置都成立。
    //https://innovault.wiki/cn/advanced/cinematics/
    //
    //A five-second camera-only cutscene: the view eases out to a vista ahead of
    //the player, zooms in for a look, shudders once, then glides home — inputs
    //locked throughout, one local chat subtitle on the way. The timeline is built
    //once during VaultSetup and cached; every track uses player-relative
    //coordinates, so the shot works anywhere on any map.
    //https://innovault.wiki/en/advanced/cinematics/
    internal sealed class DemoCutscene : CutsceneClip
    {
        //正在播放的演出优先级严格大于新演出时，新的会被拒绝；同级或更低则被抢占。
        //给个不高不低的值，日后真正的 Boss 入场演出可以名正言顺地打断这段演示
        //
        //A playing clip strictly higher in priority rejects newcomers; equal or
        //lower gets preempted. A middling value here lets a real boss-intro clip
        //legitimately interrupt this demo someday
        public override int Priority => 10;

        //基类只查玩家存在且 active；死亡时镜头飞走只会让人错过复活点，直接拒演
        //
        //The base check only wants an existing, active player; flying the camera
        //away while dead just hides the respawn, so refuse to play
        public override bool CanPlay(Player player) => base.CanPlay(player) && !player.dead;

        //"远点"每帧都从上下文现算：跟着玩家朝向，翻个身重播，镜头就往另一边飞
        //
        //The vista is recomputed from context every frame: it follows the facing
        //direction, so replaying after turning around sends the camera the other way
        private static Vector2 Vista(CutsceneContext context)
            => context.PlayerCenter + new Vector2(context.Player.direction * 460f, -240f);

        protected override void BuildTimeline(CutsceneTimeline timeline) {
            //Add 会按轨道结束帧自动扩展 Duration，显式写 300 只是把总长挑明：
            //五秒，300 帧，下面所有帧号都以此为标尺
            //
            //Add() already grows Duration to the last track's end tick; stating
            //300 up front simply makes the five-second ruler explicit
            timeline.Duration = 300;

            timeline
                //输入锁贯穿全程。锁的是移动、跳跃、用物品这类主动操作，
                //玩家仍会正常受重力和击退——演出不是无敌帧
                //
                //The input lock spans the whole clip. It blocks deliberate actions
                //like moving, jumping and item use; gravity and knockback still
                //apply — a cutscene is not an invincibility frame
                .Add(new InputLockTrack(0, 300, CutsceneInputLockFlags.All))
                //第一幕：从玩家中心缓动平移到远点。Lerp 轨道在激活首帧采样起点，
                //之后按 CubicOut 先快后慢地贴向目标
                //
                //Act one: ease from the player's center out to the vista. A Lerp
                //track samples its start on the first active frame, then closes in
                //with a fast-then-gentle CubicOut
                .Add(CameraFocusTrack.Lerp(0, 90, context => context.PlayerCenter, Vista,
                    lerpSpeed: 0.08f, ease: CutsceneEase.CubicOut))
                //第二幕：停在远点端详。Follow 每帧重新取目标，玩家被击飞了镜头也跟得上
                //
                //Act two: hold on the vista. Follow re-reads its target every
                //frame, so the shot keeps up even if the player gets launched
                .Add(CameraFocusTrack.Follow(90, 150, Vista, lerpSpeed: 0.06f))
                //平移到位后再拉近，两段运动错开，观感比同时开跑干净得多
                //
                //Zoom only after the pan has settled; staggering the two motions
                //reads much cleaner than running them at once
                .Add(new CameraZoomTrack(90, 60, 1f, 1.55f, 0.05f, CutsceneEase.QuadInOut))
                //震屏轨道是一次性的：到帧触发后自行衰减。方向传零向量表示随机
                //
                //The shake track is one-shot: it fires at its tick and decays on
                //its own. A zero direction means "pick one at random"
                .Add(new CameraShakeTrack(155, Vector2.Zero, 12f, 0.9f, 20))
                //字幕走本地聊天栏。过场只在本机播放，这行字自然也只有自己看得见
                //
                //The subtitle goes to the local chat line. The cutscene plays on
                //this machine only, so naturally the line is yours alone
                .AddEvent(170, _ => VaultUtils.Text(
                    Language.GetTextValue("Mods.InnoVaultExample.Cinematics.DemoCutscene.Subtitle"),
                    Color.LightGoldenrodYellow))
                //终幕：缩放与镜头一起回家。就算这两段没铺满结尾，演出结束时
                //导演器也会调用 Camera.End() 把残余偏差平滑抹掉
                //
                //Finale: zoom and focus travel home together. Even if these tracks
                //undershoot, the director calls Camera.End() at the finish to
                //smooth away whatever offset remains
                .Add(new CameraZoomTrack(240, 60, 1.55f, 1f, 0.06f, CutsceneEase.QuadInOut))
                .Add(CameraFocusTrack.Lerp(240, 60, Vista, context => context.PlayerCenter,
                    lerpSpeed: 0.1f, ease: CutsceneEase.CubicOut));
        }
    }

    internal sealed class DemoCutsceneEntry : IDemoEntry
    {
        public string Id => "DemoCutscene";
        public string Chapter => "Advanced";
        public string WikiSlug => "advanced/cinematics";
        public string SourcePath => "Content/Advanced/Cinematics/DemoCutscene.cs";
        public int DemoItemType => ModContent.ItemType<DirectorClapper>();
        public int SortIndex => 0;
    }
}
