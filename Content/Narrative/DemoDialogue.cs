using InnoVault.Narrative.Composition;
using InnoVault.Narrative.Core;
using InnoVault.Narrative.Portraits;
using InnoVault.Narrative.Progress;
using InnoVault.Narrative.Services;
using InnoVaultExample.Common;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Narrative
{
    //一段一分钟不到的短剧本：三句开场白、一个三选一、一句收尾，最后由功能弹窗谢幕。
    //对话框、选项按钮、弹窗全是框架自带的默认视图，这个文件里没有一行 UI 代码；
    //台词只写本地化键，改词去 hjson，代码保持干净。
    //https://innovault.wiki/cn/narrative/
    //
    //A sub-minute script: three opening lines, one three-way choice, a short
    //farewell, then a functional popup takes the bow. The dialogue box, choice
    //buttons and popup are all stock views shipped with the framework, so this
    //file contains zero UI code; every line goes through localization keys.
    //https://innovault.wiki/en/narrative/
    internal sealed class DemoDialogue : NarrativeScenario
    {
        //说话人的短名。Speaker() 会自动补上模组前缀，避免跨模组撞名
        //
        //Short speaker name; Speaker() prefixes it with the mod name so it can't
        //collide with other mods' characters
        private const string Docent = "Docent";

        private static string T(string key) => Language.GetTextValue("Mods.InnoVaultExample.Narrative.DemoDialogue." + key);

        //"至少完整看过一次"的粘性标记。框架自己有两阶段完成记录
        //（Triggered → Completed，播完由 Runner 写入），但那个记录在每次重播
        //开场都会被覆写回 Started，拿来判断"看没看过"并不牢靠；自定义 flag
        //一旦置真框架就不会再碰。默认进度存储是内存实现，退出游戏即忘，
        //要跨存档记住需换成持久化实现，见 wiki 的 DataModules 一节
        //
        //A sticky "watched it once in full" flag. The framework keeps its own
        //two-phase record (Triggered then Completed, written by the runner), but
        //that record gets overwritten back to Started whenever a replay begins,
        //so it is a poor "have I seen this" test; a custom flag stays true once
        //set. The default progress store is in-memory and forgets on exit — swap
        //in a persistent store (see the DataModules wiki page) to remember
        private NarrativeProgressKey SeenOnceKey => new(Mod.Name, Key, "seen_once");

        //SetStaticDefaults 由 VaultSetup 调用，在这里给说话人挂一个本地化显示名。
        //传解析器而不是现成字符串，切换语言后名字牌能跟着变
        //
        //SetStaticDefaults runs during VaultSetup; register a localized display
        //name for the speaker here. Passing a resolver instead of a plain string
        //keeps the name tag in sync when the language changes
        public override void SetStaticDefaults() {
            PortraitRegistry.Register(Mod.Name, Docent).Name(() => T("SpeakerName"));
        }

        //OnCompleted 只在场景完整播完时回调（框架先写 ScenarioProgress.Completed，
        //再轮到这里），中途退场不算，正适合落"看过一遍"的标记
        //
        //OnCompleted fires only when the scenario finishes in full (the framework
        //writes ScenarioProgress.Completed first, then calls this); bailing out
        //midway doesn't count, which is exactly what the flag wants
        protected override void OnCompleted() => NarrativeServices.Progress.SetFlag(SeenOnceKey, true);

        //每次 Begin() 都会重新执行 Build，所以 When 读到的永远是当下状态：
        //首播没有那句"又来了"，完整看过一遍之后重播才会多出这句
        //
        //Build reruns on every Begin(), so When() always reads the current state:
        //the "back again" line is absent on the first showing and appears on
        //replays after one full watch
        protected override void Build(NarrativeComposer n) {
            n.When(() => NarrativeServices.Progress.GetFlag(SeenOnceKey),
                 replay => replay.Say(Speaker(Docent), T("OpeningRerun")))
             .Say(Speaker(Docent), T("Opening1"))
             .Say(Speaker(Docent), T("Opening2"))
             .Say(Speaker(Docent), T("Opening3"))
             //这三个选项都显式指定了跳转目标，选完直接跳对应标签；注意不传
             //target 的选项默认 Continue，会顺序滑落到 Choice 后面的下一个节点
             //
             //All three options carry an explicit jump target, so picking one goes
             //straight to its label; note that an option without a target defaults
             //to Continue and falls through to the node right after the Choice
             .Choice(Speaker(Docent), T("ChoicePrompt"), c => c
                 .Option("how", T("OptionHow"), NarrativeTarget.Goto("how"))
                 .Option("who", T("OptionWho"), NarrativeTarget.Goto("who"))
                 .Option("skip", T("OptionSkip"), NarrativeTarget.Goto("skip")))
             .Label("how")
             .Say(Speaker(Docent), T("ReplyHow"))
             .Goto("outro")
             .Label("who")
             .Say(Speaker(Docent), T("ReplyWho"))
             .Goto("outro")
             .Label("skip")
             .Say(Speaker(Docent), T("ReplySkip"))
             //skip 分支本来就贴着 outro，这个 Goto 可以省；补上是防日后有人在
             //中间插台词时忘了分支会顺序滑落
             //
             //The skip branch sits right before outro so this Goto is redundant;
             //it stays as a guard for whoever inserts lines here later
             .Goto("outro")
             .Label("outro")
             .Say(Speaker(Docent), T("Farewell"))
             //功能弹窗收尾：Claimable(false) 加 Hold 表示无需点击、停留几秒自动散场；
             //blocking: true 让场景等弹窗解析完才真正 End
             //
             //A functional popup wraps up: Claimable(false) plus Hold means no
             //click required, it lingers a few seconds and leaves; blocking keeps
             //the scenario alive until the popup resolves
             .Popup(Popups.Message(T("PopupTitle"), T("PopupBody")).Claimable(false).Hold(4.5f), blocking: true)
             .End();
        }
    }

    //叙事章在手册里的那一行
    //
    //The narrative chapter's row in the guide
    internal sealed class DemoDialogueEntry : IDemoEntry
    {
        public string Id => "DemoDialogue";
        public string Chapter => "Narrative";
        public string WikiSlug => "narrative";
        public string SourcePath => "Content/Narrative/DemoDialogue.cs";
        public int DemoItemType => ModContent.ItemType<StoryScript>();
        public int SortIndex => 0;
    }
}
