using InnoVault.Narrative.Core;
using InnoVault.Narrative.Runtime;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Narrative
{
    //DemoDialogue 的触发物品：使用后请求播放那段短剧本。物品本身不改任何
    //世界状态，全部工作就是在正确的一端、且没在重播时调一次 Begin()。
    //借用原版书本贴图，示例不为一件道具攒美术资源。
    //https://innovault.wiki/cn/narrative/
    //
    //Trigger item for DemoDialogue: using it requests the short script. The item
    //changes no world state at all; its whole job is calling Begin() once, on the
    //right side, and only when the scenario isn't already running. It borrows the
    //vanilla book sprite so the demo doesn't need its own art.
    //https://innovault.wiki/en/narrative/
    internal sealed class StoryScript : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.Book;

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.MenuOpen;
        }

        //多人语义：叙事默认是本地客户端的演出。NarrativeRunner 在 dedServ 上
        //不会推进，UI、进度、选项记录也全部只落在本机；框架唯一的联机出口是
        //可选注入的 INarrativeSyncService——场景完整播完时它会收到一次
        //SyncProgress(key, Completed) 回调，要不要发网络包由消费模组自己决定。
        //本示例不注入该服务，所以远程玩家对这段对话一无所知，这正是默认行为
        //
        //Multiplayer semantics: narrative playback is a local-client show by
        //default. NarrativeRunner never ticks on a dedicated server, and UI,
        //progress and choice records stay on this machine; the framework's only
        //network hook is the optional INarrativeSyncService, which gets a single
        //SyncProgress(key, Completed) callback when a scenario finishes in full —
        //whether to send a packet is the consumer's call. This demo injects no
        //such service, so remote players never hear about the dialogue, which is
        //exactly the default behavior
        public override bool? UseItem(Player player) {
            //远程客户端会模拟别人挥动物品的动画，但戏只演给使用者自己看
            //
            //Remote clients simulate other players' use animations, but the show
            //belongs to the player who actually used the item
            if (Main.dedServ || player.whoAmI != Main.myPlayer) {
                return true;
            }

            DemoDialogue scenario = NarrativeScenario.Find<DemoDialogue>();
            if (scenario == null) {
                return true;
            }

            //防重复触发：Begin() 在忙碌时会把场景排进队列，连点两下就会连播两场。
            //先问一句"这场戏是否已在演或已在排队"，比事后清队列体面得多
            //
            //Anti-repeat guard: Begin() enqueues when busy, so double-tapping the
            //item would queue a second showing. Asking "is this scenario already
            //active or pending" up front beats cleaning the queue afterwards
            if (NarrativeRunner.IsScenarioActiveOrPending(scenario.Key)) {
                return true;
            }

            scenario.Begin();
            return true;
        }
    }
}
