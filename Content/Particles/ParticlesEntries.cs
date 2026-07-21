using InnoVaultExample.Common;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Particles
{
    //粒子章挂两条演示：火花剑和全局染色。
    //
    //The particles chapter signs up two demos: the spark sword and the global tint.

    internal class SparkSwordEntry : IDemoEntry
    {
        public string Id => "SparkSword";
        public string Chapter => "Particles";
        public string WikiSlug => "content/prt";
        public string SourcePath => "Content/Particles/SparkSword.cs";
        public int DemoItemType => ModContent.ItemType<SparkSword>();
        public int SortIndex => 0;
    }

    internal class GlobalPrtEntry : IDemoEntry
    {
        public string Id => "GlobalPrt";
        public string Chapter => "Particles";
        public string WikiSlug => "content/prt";
        public string SourcePath => "Content/Particles/GlobalSparkTint.cs";
        public int SortIndex => 1;
    }
}
