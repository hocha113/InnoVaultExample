using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;

namespace InnoVaultExample.Content.GettingStarted
{
    //[VaultLoaden] 把"手写 ModContent.Request 塞进某个加载钩子"换成声明式的一行标签：
    //给静态成员标上路径，InnoVault 在客户端 PostSetupContent 阶段统一扫描赋值。
    //下面三个成员分别演示显式路径、{@namespace} 占位符，以及按成员声明类型自动推断资源种类。
    //
    //[VaultLoaden] replaces hand-written ModContent.Request calls with one
    //declarative attribute: tag a static member with a path and InnoVault fills
    //it in during the client-side PostSetupContent scan. The three members below
    //show an explicit path, the {@namespace} placeholder, and how the asset kind
    //is inferred from the member's declared type.
    //
    //https://innovault.wiki/cn/core/vault-loaden/
    internal static class DemoAssets
    {
        //成员写成静态字段或带 setter 的静态属性都能被扫描到。这里统一用自动属性，
        //和 InnoVault 自带的 VaultAsset 一个路子：赋值发生在反射里，编译器看不见，
        //裸字段会招来"从未赋值"的 CS0649 警告，属性则不会。
        //
        //Both static fields and static properties with a setter get scanned.
        //Auto-properties are used throughout here, same as InnoVault's own
        //VaultAsset: the assignment happens via reflection where the compiler
        //cannot see it, so bare fields draw CS0649 "never assigned" warnings
        //while properties stay quiet.

        //最直白的写法：从模组根目录起的显式路径，不带扩展名。
        //Asset<Texture2D> 是延迟句柄，扫描时不会立刻把纹理推上 GPU。
        //
        //The plainest form: an explicit path from the mod root, no extension.
        //Asset<Texture2D> is a lazy handle, so the scan does not force the
        //texture onto the GPU immediately.
        [VaultLoaden("Assets/Effects/StarTexture")]
        public static Asset<Texture2D> Star { get; set; }

        //占位符写法：{@namespace} 会被替换成字段所在类的命名空间路径
        //（这里是 InnoVaultExample/Content/GettingStarted，开头的模组名段会被自动剥掉），
        //于是这条路径指向同文件夹里的 DemoStar.png。代码挪文件夹时路径跟着走，
        //适合"素材就放在代码旁边"的组织方式。类似的还有 {@classPath} 与跨模组的 @ModName/ 前缀。
        //
        //Placeholder form: {@namespace} expands to the namespace path of the
        //containing class (InnoVaultExample/Content/GettingStarted here; the
        //leading mod-name segment is stripped automatically), so this points at
        //DemoStar.png sitting next to this file. Move the folder and the path
        //follows — handy when art lives beside the code. {@classPath} and the
        //cross-mod @ModName/ prefix work the same way.
        [VaultLoaden("{@namespace}/DemoStar")]
        public static Asset<Texture2D> DemoStar { get; set; }

        //字段声明为 SoundStyle 时自动按音效加载，wav/ogg 同样不写扩展名。
        //
        //A SoundStyle field is loaded as a sound automatically; again no
        //extension on the path.
        [VaultLoaden("Assets/Sounds/CatalogOpen")]
        public static SoundStyle CatalogOpen { get; set; }

        //这些成员什么时候才能用：VaultLoad.LoadAsset 在客户端 PostSetupContent 里扫描，
        //且排在所有 IVaultLoader.SetupData 之后。也就是说 Mod.Load、ModSystem.OnModLoad、
        //SetStaticDefaults 这些更早的钩子里它们还是 null / default；
        //从 IVaultLoader.LoadAsset、我们自己的 PostSetupContent（InnoVault 按依赖顺序先跑）
        //以及一切进入游戏后的逻辑开始才有值。专用服务器整个跳过扫描，字段恒为默认值，
        //所以任何用到它们的代码本身就该是纯客户端代码。
        //
        //When these members become usable: VaultLoad.LoadAsset scans during
        //client PostSetupContent, after every IVaultLoader.SetupData. That means
        //they are still null/default in earlier hooks like Mod.Load,
        //ModSystem.OnModLoad or SetStaticDefaults; they hold values from
        //IVaultLoader.LoadAsset onward, in our own PostSetupContent (InnoVault
        //runs first in dependency order), and in all in-game code. A dedicated
        //server skips the scan entirely and the fields stay default — code that
        //touches them should be client-only anyway.
    }
}
