using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace InnoVaultExample.Common
{
    //全模组的示例目录。每条演示线在自己的文件夹里用小型纯数据类实现 IDemoEntry，
    //目录在加载末期反射收集，目录 UI 据此列出全部演示、跳 wiki、发物品。
    //
    //The demo catalog for the whole mod. Each demo line keeps its IDemoEntry
    //implementations as tiny plain data classes inside its own folder; the catalog
    //collects them via reflection late in loading, and the catalog UI uses the
    //entries to list demos, link to the wiki, and hand out demo items.

    //一条演示的元数据。写成普通类就行，千万别挂在 ModItem 之类的 ModType 上——
    //目录只认无参构造的纯数据类。之所以用"接口 + 反射收集"而不是中心注册表，
    //是因为十几条演示线并行开发，谁都不该被迫回到同一个文件里改代码。
    //
    //Metadata for one demo. Implement it on a plain class with a parameterless
    //constructor; never on a ModType like ModItem. We collect via reflection
    //instead of a central registry so a dozen parallel demo lines never have to
    //edit the same file.
    internal interface IDemoEntry
    {
        //本地化键：Mods.InnoVaultExample.DemoCatalog.Entries.<Id>.Name 与 .HowToTry
        //
        //Localization keys: Mods.InnoVaultExample.DemoCatalog.Entries.<Id>.Name and .HowToTry
        string Id { get; }

        //必须是 DemoCatalog.Chapters 里的固定章节名之一，目录按它归组
        //
        //Must be one of the fixed names in DemoCatalog.Chapters; the catalog groups by it
        string Chapter { get; }

        //innovault.wiki 的路径部分，例如 "content/prt"，
        //目录 UI 会拼成 https://innovault.wiki/cn/<slug>/ 或 /en/；
        //留空表示暂无对应页面，目录会隐藏这一条的 wiki 按钮
        //
        //Path part of innovault.wiki, e.g. "content/prt"; the catalog UI builds
        //https://innovault.wiki/cn/<slug>/ or /en/ from it. Leave it empty while
        //no page exists yet and the catalog hides the wiki button for that entry
        string WikiSlug { get; }

        //源码相对路径，例如 "Content/Particles/SparkSword.cs"，方便读者顺藤摸瓜找到代码
        //
        //Source path relative to the repo root, e.g. "Content/Particles/SparkSword.cs",
        //so readers can jump straight to the code
        string SourcePath { get; }

        //演示物品的 item type，目录 UI 用来发放物品；0 表示该演示没有对应物品
        //
        //Item type of the demo item, used by the catalog UI to hand it out; 0 means none
        int DemoItemType => 0;

        //同章节内的排序，小的在前
        //
        //Sort order within the chapter, smaller first
        int SortIndex => 0;
    }

    internal class DemoCatalog : ModSystem
    {
        //固定章节名，也是本地化键 Chapters.<名> 的最后一段；Content/ 下的文件夹
        //基本按它命名（UI 章是例外，代码在 Content/Guide）。数组顺序就是目录里的
        //展示顺序，别随手排序。
        //
        //Fixed chapter names, also the last segment of the Chapters.<name>
        //localization keys; folders under Content/ mostly share these names (UI is
        //the exception, its code sits in Content/Guide). The array order is the
        //display order, so do not re-sort it casually.
        public static readonly string[] Chapters = [
            "GettingStarted", "Particles", "TileEntities", "UI", "HeldWeapons", "Overrides",
            "Persistence", "Networking", "AI", "Narrative", "Advanced", "Utilities"
        ];

        public static IReadOnlyList<IDemoEntry> Entries { get; private set; } = [];

        //挑 PostSetupContent 收集是因为这时所有内容（包括物品 type）都已就绪，
        //DemoItemType 可以安全引用 ModContent.ItemType<T>()。
        //末尾再按 Id 排一道：SortIndex 撞车时顺序不随编译器给的类型顺序漂移，
        //每次构建出来的目录都长一个样。
        //
        //Collecting in PostSetupContent because all content (including item types)
        //is ready by then, so DemoItemType can safely use ModContent.ItemType<T>().
        //The trailing Id sort settles SortIndex ties, so the catalog order never
        //drifts with whatever type order the compiler happened to emit.
        public override void PostSetupContent() {
            List<IDemoEntry> entries = Mod.Code.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IDemoEntry).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (IDemoEntry)Activator.CreateInstance(t))
                .OrderBy(e => ChapterIndex(e.Chapter))
                .ThenBy(e => e.SortIndex)
                .ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();

            foreach (IDemoEntry entry in entries.Where(e => ChapterIndex(e.Chapter) == Chapters.Length)) {
                Mod.Logger.Warn($"IDemoEntry '{entry.Id}' uses unknown chapter '{entry.Chapter}' and will be listed last");
            }

            Entries = entries;
        }

        //写错章节名的条目沉到末尾并在日志里留痕。Array.IndexOf 对未知章节给 -1，
        //直接拿去排序会让写错的条目悄悄浮到目录最顶端，反而最不容易被发现。
        //
        //Entries with a misspelled chapter sink to the end and leave a log line.
        //Array.IndexOf yields -1 for unknown names; sorting on that raw value would
        //float the broken entry to the very top, which is the easiest place to miss.
        private static int ChapterIndex(string chapter) {
            int index = Array.IndexOf(Chapters, chapter);
            return index < 0 ? Chapters.Length : index;
        }

        public override void Unload() {
            Entries = [];
        }
    }
}
