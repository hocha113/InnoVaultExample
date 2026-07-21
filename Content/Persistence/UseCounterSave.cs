using InnoVault;
using InnoVault.GameSystem;
using InnoVaultExample.Common;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InnoVaultExample.Content.Persistence
{
    //跨世界的模组级使用计数：本章的演示物品每用一次就 +1，换世界、换角色都不清零。
    //重点是 SaveMod 的手动契约——它不挂任何自动保存管线，读写时机全得自己挑。
    //
    //A mod-scoped use counter that survives world and character switches: every use
    //of this chapter's demo items bumps it by one. The point is SaveMod's manual
    //contract - nothing loads or saves unless you call it yourself.
    //https://innovault.wiki/cn/persistence/save-content/

    //数据落在 VaultSave/ModDatas/mod_InnoVaultExample.nbt，一个独立的 NBT 文件，
    //和世界存档、玩家存档都不共享。适合放"这台机器上的全局数据"：设置、统计、主菜单进度。
    //多人时每个端各写各的文件，互不同步——想要跨端一致的数据别用它，交给世界存档或网络包。
    //
    //The data lives in VaultSave/ModDatas/mod_InnoVaultExample.nbt, a standalone NBT
    //file unrelated to world or player saves. Good for per-machine global data:
    //settings, statistics, main-menu progression. In multiplayer every peer writes
    //its own file with no syncing - cross-peer state belongs in world saves or packets.
    internal class UseCounterSave : SaveMod
    {
        //计数放实例字段上：每个 SaveContent 子类在框架里只有一个单例，
        //DoSave / DoLoad 找的就是它，不需要再自己搞 static 存储
        //
        //The counter sits on an instance field: each SaveContent subclass is a
        //framework-managed singleton, and DoSave / DoLoad operate on exactly that
        //instance, so no extra static storage is needed
        public int TotalUses { get; private set; }

        public static UseCounterSave Instance => SaveContent<SaveMod>.GetInstance<UseCounterSave>();

        //只统计"这台机器的玩家"：SaveMod 文件本来就是本机数据。专用服务器上
        //Main.myPlayer 指向占位槽，不等于任何真实玩家的 whoAmI，
        //这个判断顺带把服务器端的重复执行也排除掉了
        //
        //Count only the local machine's player - the SaveMod file is per-machine
        //data anyway. On a dedicated server Main.myPlayer points at the dummy slot
        //and never equals a real player's whoAmI, so this check also filters out
        //the server-side duplicate execution for free
        public static void CountLocalUse(Player player) {
            if (player.whoAmI != Main.myPlayer) {
                return;
            }
            Instance.TotalUses++;
        }

        public override void SaveData(TagCompound tag) {
            tag["totalUses"] = TotalUses;
        }

        public override void LoadData(TagCompound tag) {
            TotalUses = tag.GetInt("totalUses");
        }
    }

    //SaveMod 最大的坑就在这：它不会随世界保存自动执行，DoLoad / DoSave 一次都不调，
    //数据就一次都不会动。读档放在 IVaultLoader.SetupData——此时所有内容已注册完毕、
    //又远早于任何世界加载，模组一进主菜单计数就已经就位。
    //一个模组里可以有任意多个 IVaultLoader 实现，框架挨个调用，互不干扰。
    //
    //Here is SaveMod's biggest trap: it never runs with the world save pipeline.
    //If you never call DoLoad / DoSave, the data never moves. Loading goes in
    //IVaultLoader.SetupData - all content is registered by then, and it runs long
    //before any world loads, so the counter is ready at the main menu. A mod may
    //contain any number of IVaultLoader implementations; the framework calls each.
    internal class UseCounterLoader : IVaultLoader
    {
        void IVaultLoader.SetupData() {
            SaveContent<SaveMod>.DoLoad<UseCounterSave>();
        }
    }

    //写盘挑在退出世界时：比起模组卸载，OnWorldUnload 每次退出都会触发，崩溃丢失面更小；
    //多人客户端也会走到这里，把自己那份本机计数存好。DoSave<T> 只重写自己的条目，
    //同文件里其他 SaveMod 子类的数据不会被覆盖。
    //
    //Saving happens on world exit: unlike mod unload, OnWorldUnload fires on every
    //exit, so a crash loses less. Multiplayer clients reach this too and persist
    //their own per-machine count. DoSave<T> rewrites only this type's entry and
    //leaves other SaveMod subclasses in the same file untouched.
    internal class UseCounterSaver : ModSystem
    {
        public override void OnWorldUnload() {
            SaveContent<SaveMod>.DoSave<UseCounterSave>();
        }
    }

    internal class UseCounterEntry : IDemoEntry
    {
        public string Id => "UseCounter";
        public string Chapter => "Persistence";
        public string WikiSlug => "persistence/save-content";
        public string SourcePath => "Content/Persistence/UseCounterSave.cs";
        public int SortIndex => 0;
    }
}
