using InnoVaultExample.Common;

namespace InnoVaultExample.Content.GettingStarted
{
    //入门章的目录条目，展示文案在 Localization/Basics 下按 Id 查本地化键。
    //
    //Entries for the getting-started chapter; their display text lives in
    //Localization/Basics, keyed by Id.

    internal class LoaderLifecycleEntry : IDemoEntry
    {
        public string Id => "LoaderLifecycle";
        public string Chapter => "GettingStarted";
        public string WikiSlug => "getting-started/ivault-loader";
        public string SourcePath => "Content/GettingStarted/LoaderLifecycle.cs";
        public int SortIndex => 0;
    }

    internal class VaultLoadenAssetsEntry : IDemoEntry
    {
        public string Id => "VaultLoadenAssets";
        public string Chapter => "GettingStarted";
        public string WikiSlug => "core/vault-loaden";
        public string SourcePath => "Content/GettingStarted/DemoAssets.cs";
        public int SortIndex => 1;
    }
}
