using InnoVaultExample.Common;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.HeldWeapons
{
    //手持武器章的三条演示在这里报到。
    //
    //The held-weapons chapter's three demos check in here.

    internal class DrillGunEntry : IDemoEntry
    {
        public string Id => "DrillGun";
        public string Chapter => "HeldWeapons";
        public string WikiSlug => "content/base-held-proj";
        public string SourcePath => "Content/HeldWeapons/DrillHeld.cs";
        public int DemoItemType => ModContent.ItemType<DrillGun>();
        public int SortIndex => 0;
    }

    internal class BoltTrailEntry : IDemoEntry
    {
        public string Id => "BoltTrail";
        public string Chapter => "HeldWeapons";
        public string WikiSlug => "content/trails";
        public string SourcePath => "Content/HeldWeapons/DrillBolt.cs";
        public int SortIndex => 1;
    }

    internal class SwingBladeEntry : IDemoEntry
    {
        public string Id => "SwingBlade";
        public string Chapter => "HeldWeapons";
        public string WikiSlug => "overrides/item-use-animation";
        public string SourcePath => "Content/HeldWeapons/SwingBlade.cs";
        public int DemoItemType => ModContent.ItemType<SwingBlade>();
        public int SortIndex => 2;
    }
}
