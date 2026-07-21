using InnoVaultExample.Common;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Guide
{
    //引导线在示例目录里的登记。目录 UI 自己也是一条演示——玩家在面板里
    //就能看到"这个面板怎么做"的源码入口和 wiki 页。
    //
    //The guide line's registration in the demo catalog. The catalog UI is itself a
    //demo - the panel lists its own source path and wiki page.
    internal class GuideBookEntry : IDemoEntry
    {
        public string Id => "GuideBook";
        public string Chapter => "UI";
        public string WikiSlug => "content/ui-handle";
        public string SourcePath => "Content/Guide/GuideCatalogUI.cs";

        //DemoCatalog 在 PostSetupContent 才实例化这些登记类，
        //这时物品 type 已经分配完毕，可以放心取
        //
        //DemoCatalog instantiates these registrations in PostSetupContent, after
        //item types are assigned, so this lookup is safe
        public int DemoItemType => ModContent.ItemType<GuideBook>();
    }
}
