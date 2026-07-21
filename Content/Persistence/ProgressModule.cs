using InnoVault.DataModules;
using InnoVaultExample.Common;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InnoVaultExample.Content.Persistence
{
    //玩家进度演示：记录这个角色试过哪些演示、总共触发过多少次，随玩家存档走。
    //DataModule 把数据拆成带版本号的小模块，DataModuleStore 负责聚合与存取，
    //挂载方（这里是 ModPlayer）只管把 Store 接到 tML 的 SaveData / LoadData 上。
    //
    //Per-player progress: which demos this character has tried and how many times
    //in total, persisted with the player save. DataModule splits data into small
    //versioned modules, DataModuleStore aggregates them, and the host (a ModPlayer
    //here) just wires the store into tML's SaveData / LoadData hooks.
    //https://innovault.wiki/cn/persistence/data-modules/

    //模块类型被 InnoVault 自动发现并注册（它是 VaultType，同时也走 tML 的自动加载），
    //SaveKey 默认是 "InnoVaultExample/ProgressModule"——带模组名前缀，跨模组不会撞键。
    //如果类型的 CanLoad() 返回 false，模块不会注册：Store.Get 仍能临时建一个实例并给出警告，
    //但读档时不会被自动恢复，这是排查"读档后字段全默认"最先要看的地方。
    //
    //The module type is auto-discovered and registered by InnoVault (it is a
    //VaultType and also goes through tML autoloading); SaveKey defaults to
    //"InnoVaultExample/ProgressModule" - mod-prefixed, so mods never collide.
    //If CanLoad() returns false the module is not registered: Store.Get can still
    //create a throwaway instance (with a logged warning), but nothing restores it
    //on load - the first thing to check when fields come back all-default.
    internal sealed class ProgressModule : DataModule
    {
        //默认反射序列化直接吃公共可写字段：标量、枚举、字符串列表都在支持范围内。
        //DataModuleName 演示自定义持久化键：存档里写 "tried"，读取时还会依次尝试
        //旧键 "TriedDemoList"——字段改名之后老存档就靠别名接住
        //
        //Default reflection serialization handles public writable fields: scalars,
        //enums and string lists are all supported. DataModuleName shows a custom
        //persistence key: saved as "tried", with the legacy key "TriedDemoList"
        //tried as a fallback on load - aliases catch old saves after a rename
        [DataModuleName("tried", "TriedDemoList")]
        public List<string> TriedDemos = [];

        public int UseCount;

        //运行时缓存不该进存档：标了 DataModuleIgnore 的成员会被反射序列化整个跳过
        //
        //Runtime caches do not belong in saves: members marked DataModuleIgnore are
        //skipped entirely by the reflection serializer
        [DataModuleIgnore]
        public int SessionUses;

        //版本号会写进模块自己的 "__v" 子键，读档时以 loadedVersion 传回来
        //
        //The version is written into the module's own "__v" subkey and handed back
        //as loadedVersion on load
        public override int Version => 2;

        //版本迁移的样板：先让 base 按当前字段布局读，再对旧版本补一段搬运。
        //这个模组没有真正的 v1 存档，这条分支是给读者看套路的——
        //假设 v1 里计数存在 "LegacyUseCount" 键下，升到 v2 时把旧值搬进新字段。
        //
        //The migration boilerplate: let base read the current layout first, then
        //patch up older versions. This mod has no real v1 saves - the branch exists
        //to show the pattern: pretend v1 kept the counter under "LegacyUseCount",
        //and v2 moves the old value into the new field.
        public override void LoadData(TagCompound tag, int loadedVersion) {
            base.LoadData(tag, loadedVersion);

            if (loadedVersion < 2 && tag.TryGet("LegacyUseCount", out int legacyCount)) {
                UseCount = legacyCount;
            }
        }

        public bool HasTried(string demoId) => TriedDemos.Contains(demoId);
    }

    //挂载方：Store 不绑定任何持久化位置，写进玩家档、世界档还是 SaveContent 文件，
    //完全由这里的两行钩子决定。本演示选玩家档——数据随 .plr 文件走，跟着角色跨世界。
    //
    //The host: a store binds to no particular storage location - player save, world
    //save or a SaveContent file, decided entirely by these two hook lines. This demo
    //picks the player save, so the data travels with the .plr file across worlds.
    internal class ProgressPlayer : ModPlayer
    {
        public DataModuleStore Modules { get; private set; } = new();

        public ProgressModule Progress => Modules.Get<ProgressModule>();

        //玩家档由玩家自己的客户端落盘：服务器上的 ModPlayer 只是镜像，在那边改了
        //也进不了 .plr 文件。所以写进度一律圈在"本机玩家"判断里；
        //dedServ 上 Main.myPlayer 指向占位槽、不等于任何真实玩家的 whoAmI，
        //服务器端的重复执行被顺带排除。
        //
        //Player saves are written by the owning client: the server's ModPlayer is a
        //mirror, and changes made there never reach the .plr file. So progress writes
        //are gated to the local player; on a dedicated server Main.myPlayer points at
        //the dummy slot and never equals a real player's whoAmI, which filters the
        //server-side duplicate for free.
        public static void RecordDemoUse(Player player, string demoId) {
            if (player.whoAmI != Main.myPlayer) {
                return;
            }

            ProgressModule progress = player.GetModPlayer<ProgressPlayer>().Progress;
            if (!progress.TriedDemos.Contains(demoId)) {
                progress.TriedDemos.Add(demoId);
            }
            progress.UseCount++;
            progress.SessionUses++;
        }

        public override void Initialize() {
            Modules = new DataModuleStore();
        }

        public override void SaveData(TagCompound tag) {
            Modules.SaveData(tag);
        }

        //LoadData 先重置已有模块再按 SaveKey 读入，存档里有、内存里还没有的模块
        //会根据注册表自动补齐——所以这里不需要任何"先 Get 一下"的预热代码
        //
        //LoadData resets existing modules and reads them back by SaveKey; modules
        //present in the save but not yet in memory are recreated from the registry,
        //so no "Get it once to warm up" code is needed here
        public override void LoadData(TagCompound tag) {
            Modules.LoadData(tag);
        }

        //这份进度纯本地（.plr），不需要联机同步，所以不覆写 CopyClientState /
        //SendClientChanges。tML 要求二者要么都覆写、要么都不覆写；若以后要同步，
        //CopyClientState 里对 Modules 做 Clone()（避免共享引用），再在
        //SendClientChanges 里对比克隆体与本体并发包。
        //
        //This progress is local-only (.plr) and needs no multiplayer sync, so
        //neither CopyClientState nor SendClientChanges is overridden. tML requires
        //both or neither; if you later need sync, Clone() Modules in
        //CopyClientState (avoid a shared reference), then diff clone vs original
        //and send the delta in SendClientChanges.
    }

    internal class ProgressModuleEntry : IDemoEntry
    {
        public string Id => "ProgressModule";
        public string Chapter => "Persistence";
        public string WikiSlug => "persistence/data-modules";
        public string SourcePath => "Content/Persistence/ProgressModule.cs";
        public int SortIndex => 2;
    }
}
