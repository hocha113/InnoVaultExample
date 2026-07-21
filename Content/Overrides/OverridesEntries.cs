using InnoVaultExample.Common;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Overrides
{
    //覆写章的目录条目。八种 Override 不必每种登记一条：透镜这条代表整个门控画廊，
    //木剑与史莱姆单独成条，因为它们各讲一件独立的事——常开覆写与 SyncVar 同步。
    //
    //Catalog entries for the overrides chapter. The eight override types do not need
    //one entry apiece: the lens entry stands for the whole gated gallery, while the
    //sword and the slime get their own entries because each tells a separate story —
    //an always-on override and SyncVar networking.

    internal class OverrideLensEntry : IDemoEntry
    {
        public string Id => "OverrideLens";
        public string Chapter => "Overrides";
        public string WikiSlug => "overrides";
        public string SourcePath => "Content/Overrides/OverrideLens.cs";
        public int DemoItemType => ModContent.ItemType<OverrideLens>();
        public int SortIndex => 0;
    }

    internal class WoodenSwordDemoEntry : IDemoEntry
    {
        public string Id => "WoodenSwordDemo";
        public string Chapter => "Overrides";
        public string WikiSlug => "overrides/item";
        public string SourcePath => "Content/Overrides/WoodenSwordDemo.cs";

        //演示对象就是原版木剑，直接发原版 type，不需要新物品
        //
        //The demo subject is the vanilla wooden sword, so hand out the vanilla type.
        public int DemoItemType => ItemID.WoodenSword;
        public int SortIndex => 1;
    }

    internal class SlimeMarkEntry : IDemoEntry
    {
        public string Id => "SlimeMark";
        public string Chapter => "Overrides";
        public string WikiSlug => "overrides/npc";
        public string SourcePath => "Content/Overrides/SlimeMarkOverride.cs";
        public int SortIndex => 2;
    }
}
