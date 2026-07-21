using InnoVault;
using InnoVault.GameSystem;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InnoVaultExample.Content.Persistence
{
    //世界级标记：这个世界有没有举行过"演示仪式"、办了几次。用仪式旗触发一次，
    //退出重进世界数据还在——SaveWorld 挂在 InnoVault 的世界保存管线上，随世界自动存取。
    //
    //A world-scoped flag: has this world ever held the demo ritual, and how many
    //times. Trigger it with the ritual flag item, leave and re-enter, and the data
    //is still there - SaveWorld rides InnoVault's world save pipeline automatically.
    //https://innovault.wiki/cn/persistence/save-content/

    //数据文件是 VaultSave/WorldDatas/world_<世界名>.nbt，按世界隔离。和 SaveMod 不同，
    //SaveWorld 不需要手动 DoSave / DoLoad：保存世界时框架异步写盘，加载世界时读回。
    //多人下这份文件只存在于服务器，客户端的这两个 static 字段一直是默认值——
    //别在客户端拿它们做玩法判断，要给玩家看就让服务器广播（下面的物品就是这么做的）。
    //
    //The file is VaultSave/WorldDatas/world_<name>.nbt, isolated per world. Unlike
    //SaveMod there is no manual DoSave / DoLoad: the framework writes asynchronously
    //on world save and reads back on world load. In multiplayer the file exists only
    //on the server; these statics stay at defaults on clients - never branch gameplay
    //on them client-side. To show players the value, broadcast it (the item below does).
    internal class WorldFlagSave : SaveWorld
    {
        public static bool RitualHeld { get; private set; }
        public static int RitualCount { get; private set; }

        //世界数据只允许权威端写。注意单人下 isServer 是 false——只判 isServer 的话
        //整个演示在单人里就静默失效了，isSinglePlayer 必须一起判
        //
        //Only the authority may write world data. Note that isServer is false in
        //single player - guard with isServer alone and the whole demo silently
        //does nothing solo, so isSinglePlayer must be part of the check
        public static void MarkRitual() {
            if (!VaultUtils.isServer && !VaultUtils.isSinglePlayer) {
                return;
            }
            RitualHeld = true;
            RitualCount++;
        }

        internal static void ResetToDefault() {
            RitualHeld = false;
            RitualCount = 0;
        }

        public override void SaveData(TagCompound tag) {
            tag["ritualHeld"] = RitualHeld;
            tag["ritualCount"] = RitualCount;
        }

        public override void LoadData(TagCompound tag) {
            RitualHeld = tag.GetBool("ritualHeld");
            RitualCount = tag.GetInt("ritualCount");
        }
    }

    //静态字段配世界数据有个必须堵上的坑：LoadData 只在存档里确实有数据时才被调用。
    //从办过仪式的世界 A 退到从没办过的世界 B，B 没数据、LoadData 不跑，
    //A 的值就原封不动地漏进了 B。所以退出世界时手动清回默认值。
    //InnoVault 比本模组先收到 OnWorldUnload，并在里面等后台写盘完成，这里清零不会截断保存。
    //
    //Statics plus world data has one trap you must plug: LoadData only runs when the
    //save actually contains data. Hop from world A (ritual held) to a fresh world B,
    //B has no data, LoadData never fires, and A's values leak into B untouched. So
    //reset to defaults on world exit. InnoVault receives OnWorldUnload before this
    //mod and waits for the background save there, so clearing here cannot truncate it.
    internal class WorldFlagResetSystem : ModSystem
    {
        public override void OnWorldUnload() {
            WorldFlagSave.ResetToDefault();
        }
    }

    //触发物品：使用后在权威端置位并向所有人播报当前值。贴图借了原版哥布林战旗，
    //演示模组就不为每件道具都画一张图了。
    //
    //The trigger item: on use the authority sets the flag and announces the current
    //value to everyone. The sprite borrows the vanilla Goblin Battle Standard so the
    //demo mod does not need custom art for every gadget.
    internal class RitualFlag : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.GoblinBattleStandard}";

        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item4;
        }

        //UseItem 跑的端比直觉多：单人一遍，多人时 owner 客户端、服务器、其他客户端
        //各跑一遍——远端在模拟使用动画时也会调进来。世界数据的写入和播报只该发生一次，
        //所以圈进权威判断里；VaultUtils.Text 在服务器上走全员广播、本地端走聊天栏，
        //正好让每个客户端都看到结果——客户端自己读不到这份世界数据。
        //
        //UseItem runs on more peers than intuition suggests: once in single player,
        //and in multiplayer on the owner client, the server, and every remote client
        //(they call in while simulating the use animation). The world-data write and
        //the announcement must happen exactly once, hence the authority guard;
        //VaultUtils.Text broadcasts on the server and prints locally otherwise, which
        //is how clients get to see a value they cannot read from the world file themselves.
        public override bool? UseItem(Player player) {
            if (VaultUtils.isServer || VaultUtils.isSinglePlayer) {
                WorldFlagSave.MarkRitual();
                VaultUtils.Text(Language.GetTextValue("Mods.InnoVaultExample.Messages.RitualReport", WorldFlagSave.RitualCount), Color.Gold);
            }

            UseCounterSave.CountLocalUse(player);
            ProgressPlayer.RecordDemoUse(player, "WorldFlag");
            return true;
        }
    }

    internal class WorldFlagEntry : IDemoEntry
    {
        public string Id => "WorldFlag";
        public string Chapter => "Persistence";
        public string WikiSlug => "persistence/save-content";
        public string SourcePath => "Content/Persistence/WorldFlagSave.cs";
        public int DemoItemType => ModContent.ItemType<RitualFlag>();
        public int SortIndex => 1;
    }
}
