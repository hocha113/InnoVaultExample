using InnoVault;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.GettingStarted
{
    //这个类什么内容都不注册，只在 IVaultLoader 的五个钩子里各写一条日志，
    //把它们相对 tModLoader 生命周期的真实触发位置钉死在日志文件里。
    //开一次游戏后在 tModLoader-Logs/client.log（或 server.log）里搜 LoaderLifecycle
    //就能看到顺序；UnLoadData 那条要等到重载或禁用模组时才会出现。
    //
    //This class registers nothing. It just logs once from each of the five
    //IVaultLoader hooks so their real position in the tModLoader lifecycle is
    //pinned down in the log file. Run the game once and search client.log
    //(or server.log) for "LoaderLifecycle" to see the order; the UnLoadData
    //line only shows up once you reload or disable the mod.
    //
    //https://innovault.wiki/cn/getting-started/ivault-loader/
    internal class LoaderLifecycle : IVaultLoader
    {
        //IVaultLoader 靠反射发现：VaultMod.Load 会扫描所有模组程序集，找到实现类就实例化，
        //不需要也没有手动注册入口，唯一要求是有无参构造函数。
        //
        //IVaultLoader is discovered by reflection: VaultMod.Load scans every mod
        //assembly and instantiates each implementor. There is no manual
        //registration; the only requirement is a parameterless constructor.
        private static Mod exampleMod;

        //触发点在 VaultMod.Load 的开头。InnoVault 是我们的前置，按依赖顺序它先加载，
        //所以这一刻我们自己的 Mod.Load 乃至所有 ModSystem.Load 都还没跑——
        //在这里引用自己模组的内容为时过早。取 Mod 实例也别赌 ModContent.GetInstance
        //这时是否就绪，走 VaultUtils.FindModByType（遍历 ModLoader.Mods，此时已填充）。
        //
        //Fires at the start of VaultMod.Load. InnoVault is our dependency and
        //loads first, so at this point our own Mod.Load and all of our
        //ModSystem.Load hooks have not run yet — touching our own content here
        //is too early. Do not bet on ModContent.GetInstance being ready either;
        //VaultUtils.FindModByType walks ModLoader.Mods, which is already populated.
        public void LoadData() {
            exampleMod = VaultUtils.FindModByType(typeof(LoaderLifecycle));
            exampleMod.Logger.Info("[LoaderLifecycle] LoadData: start of VaultMod.Load, before InnoVaultExample's own Mod.Load.");
        }

        //触发点在 VaultMod.PostSetupContent。tML 会先把所有模组的 Load/SetupContent
        //全部跑完才开始 PostSetupContent 阶段，所以此时全部内容（含物品 type）都已注册好；
        //又因为 InnoVault 排在依赖链前面，这里比我们自己的 PostSetupContent
        //（比如 DemoCatalog 收集条目）更早。服务端也会执行，适合放注册表接线类共享逻辑。
        //
        //Fires inside VaultMod.PostSetupContent. tML finishes Load/SetupContent
        //for every mod before the PostSetupContent phase begins, so all content
        //(including item types) is registered by now; and because InnoVault sits
        //earlier in the dependency chain, this runs before our own
        //PostSetupContent (e.g. before DemoCatalog collects entries). Also runs
        //on the server — the right place for shared wiring logic.
        public void SetupData() {
            exampleMod.Logger.Info("[LoaderLifecycle] SetupData: VaultMod.PostSetupContent, all content registered, runs on both client and server.");
        }

        //仍在 VaultMod.PostSetupContent 里，但整段被 !Main.dedServ 包住：专用服务器上
        //这个方法永远不会被调用，日志里也不会出现这一条。它排在 VaultLoad.LoadAsset
        //（[VaultLoaden] 扫描）之后，所以 DemoAssets 那些字段在这里已经填好了。
        //注意接口的 XML 注释写的是"在 Load 中的最后调用"，那是过时说法——
        //真实时机以这里和源码 VaultMod.cs 为准。
        //
        //Still inside VaultMod.PostSetupContent, but wrapped in !Main.dedServ:
        //on a dedicated server this method is never called and this log line
        //never appears. It runs after VaultLoad.LoadAsset (the [VaultLoaden]
        //scan), so the members in DemoAssets are already populated here. The
        //interface's XML doc claims "called at the end of Load" — that wording
        //is stale; trust this file and VaultMod.cs for the real timing.
        public void LoadAsset() {
            exampleMod.Logger.Info($"[LoaderLifecycle] LoadAsset: client only, after the [VaultLoaden] scan. DemoAssets.Star loaded: {DemoAssets.Star != null}");
        }

        //触发点在 VaultSystem.PostAddRecipes，此时所有模组的 AddRecipes 都跑完了，
        //适合做跨模组的配方调整；只改自己物品的配方就直接写在 ModItem.AddRecipes 里。
        //
        //Fires in VaultSystem.PostAddRecipes, after every mod's AddRecipes pass.
        //Good for cross-mod recipe tweaks; recipes for your own items belong in
        //ModItem.AddRecipes as usual.
        public void AddRecipesData() {
            exampleMod.Logger.Info("[LoaderLifecycle] AddRecipesData: VaultSystem.PostAddRecipes, all recipes exist by now.");
        }

        //触发点在 VaultMod.Unload。卸载顺序与加载相反，我们自己的 Unload 已经跑过了，
        //这里是最后的清理点——静态字段在这里清空，防止重载后残留上一次的引用。
        //
        //Fires in VaultMod.Unload. Unload order is the reverse of load order, so
        //our own Unload has already run; this is the final cleanup point. Clear
        //static fields here so nothing leaks across a mod reload.
        public void UnLoadData() {
            exampleMod.Logger.Info("[LoaderLifecycle] UnLoadData: VaultMod.Unload, after InnoVaultExample's own Unload.");
            exampleMod = null;
        }
    }
}
