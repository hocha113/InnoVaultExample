# InnoVault Example

[InnoVault](https://github.com/hocha113/InnoVault) 框架的官方示例模组。InnoVault 是开源的 tModLoader 开发库，完整教程在 [innovault.wiki](https://innovault.wiki)，本仓库的每一份代码都对应 wiki 的一个章节，边读文档边翻源码是预期的用法。

（English version below.）

## 这是什么、怎么用

这个模组把 InnoVault 的各个子系统各做了一小份可以进游戏摸到的演示。推荐的体验路径：

1. 启用本模组进入游戏，新角色开局会拿到一本示例手册；
2. 使用手册打开演示目录面板；
3. 在目录里按章节浏览全部演示，每一条都标注了源码文件路径，并且可以领取演示物品、跳转对应的 wiki 页面（如果有）。

不想进游戏也可以直接读代码：`Content/` 下每个文件夹对应一个章节，有对应 wiki 页面的演示都在文件头注释里放了链接。

## 章节地图

| 章节 | 代码位置 | wiki（中文为主，附英文） |
|------|----------|--------------------------|
| 入门与资源加载 | `Content/GettingStarted/` | [cn/getting-started](https://innovault.wiki/cn/getting-started/) · [en](https://innovault.wiki/en/getting-started/) |
| 粒子 PRT | `Content/Particles/` | [cn/content/prt](https://innovault.wiki/cn/content/prt/) · [en](https://innovault.wiki/en/content/prt/) |
| 物块实体 TP | `Content/TileEntities/` | [cn/content/tile-processor](https://innovault.wiki/cn/content/tile-processor/) · [en](https://innovault.wiki/en/content/tile-processor/) |
| 界面 UIHandle | `Content/Guide/` | [cn/content/ui-handle](https://innovault.wiki/cn/content/ui-handle/) · [en](https://innovault.wiki/en/content/ui-handle/) |
| 手持武器 | `Content/HeldWeapons/` | [cn/content/base-held-proj](https://innovault.wiki/cn/content/base-held-proj/) · [en](https://innovault.wiki/en/content/base-held-proj/) |
| 行为覆写 | `Content/Overrides/` | [cn/overrides](https://innovault.wiki/cn/overrides/) · [en](https://innovault.wiki/en/overrides/) |
| 数据与存档 | `Content/Persistence/` | [cn/persistence](https://innovault.wiki/cn/persistence/) · [en](https://innovault.wiki/en/persistence/) |
| 网络同步 | `Content/Networking/` | [cn/persistence/sync-var](https://innovault.wiki/cn/persistence/sync-var/) · [en](https://innovault.wiki/en/persistence/sync-var/)（PlayerNetwork 暂无专页） |
| 状态机与行为树 | `Content/AI/` | [cn/ai](https://innovault.wiki/cn/ai/) · [en](https://innovault.wiki/en/ai/) |
| 叙事对话 | `Content/Narrative/` | [cn/narrative](https://innovault.wiki/cn/narrative/) · [en](https://innovault.wiki/en/narrative/) |
| 高级系统 | `Content/Advanced/` | [cn/advanced](https://innovault.wiki/cn/advanced/) · [en](https://innovault.wiki/en/advanced/) |
| 工具函数 | `Content/Utilities/` | [cn/utilities](https://innovault.wiki/cn/utilities/) · [en](https://innovault.wiki/en/utilities/) |

章节的机器名（`GettingStarted`、`Particles` 等）定义在 `Common/DemoCatalog.cs`，目录分组和章节标题的本地化键都以它为准；`Content/` 下的文件夹大体按章节命名，UI 章是例外——它的代码在 `Content/Guide/`。

## 注释规范

本仓库是拿来读的，注释质量和代码质量同样重要。所有示例代码遵守：

- 中文注释块在上，空一行注释（`//`）分隔，英文注释块在下；英文达意即可，不逐字对译。
- 解释"为什么"，不复述代码在做什么；语气自然，像人和人交流，可以口语化。
- 文件头用两三行自然语言说清该文件演示什么就够，不要模板化的字段头或装饰性排版；有对应 wiki 页面就加一行裸链接（如 `//https://innovault.wiki/cn/content/ui-handle/`）。
- 命名用直白技术名（`DemoCatalog`、`GuideBook` 这种一眼懂的）。

一组对照示例。这样写：

```csharp
//粒子生成必须挡掉服务器：PRT 是纯客户端系统，dedServ 上 NewParticle
//只会返回一个不进系统的孤儿实例，白跑还容易误导人。
//
//Guard against the server: PRT is client-only. On dedServ, NewParticle
//returns an orphan that never enters the system.
if (Main.dedServ) return;
```

不要这样写：

```csharp
//判断 Main.dedServ 是否为真，为真则返回
//
//Check whether Main.dedServ is true, return if so
if (Main.dedServ) return;
```

## 相关链接

- InnoVault 源码（开源）：https://github.com/hocha113/InnoVault
- 教程 wiki：https://innovault.wiki
- 本示例仓库：https://github.com/hocha113/InnoVaultExample

---

# InnoVault Example (English)

The official example mod for the [InnoVault](https://github.com/hocha113/InnoVault) framework. InnoVault is an open-source tModLoader library; the full tutorials live at [innovault.wiki](https://innovault.wiki), and every piece of code in this repo maps to one wiki chapter. Reading the wiki with the source open side by side is the intended way to use it.

## What it is and how to use it

This mod ships one small in-game demo for each InnoVault subsystem. The recommended path:

1. Enable the mod and enter a world; a new character starts with a field guide.
2. Use the guide to open the demo catalog panel.
3. Browse all demos by chapter. Every entry shows its source file path, and hands out a demo item and links to a wiki page when it has one.

You can also skip the game entirely and just read the code: each folder under `Content/` is one chapter, and demos with a wiki page carry the link in their file header.

## Chapter map

| Chapter | Code | wiki |
|---------|------|------|
| Getting Started & Asset Loading | `Content/GettingStarted/` | [en/getting-started](https://innovault.wiki/en/getting-started/) |
| PRT Particles | `Content/Particles/` | [en/content/prt](https://innovault.wiki/en/content/prt/) |
| Tile Entities (TP) | `Content/TileEntities/` | [en/content/tile-processor](https://innovault.wiki/en/content/tile-processor/) |
| UI with UIHandle | `Content/Guide/` | [en/content/ui-handle](https://innovault.wiki/en/content/ui-handle/) |
| Held Weapons | `Content/HeldWeapons/` | [en/content/base-held-proj](https://innovault.wiki/en/content/base-held-proj/) |
| Behavior Overrides | `Content/Overrides/` | [en/overrides](https://innovault.wiki/en/overrides/) |
| Data & Saving | `Content/Persistence/` | [en/persistence](https://innovault.wiki/en/persistence/) |
| Network Sync | `Content/Networking/` | [en/persistence/sync-var](https://innovault.wiki/en/persistence/sync-var/) (no dedicated PlayerNetwork page yet) |
| State Machines & Behavior Trees | `Content/AI/` | [en/ai](https://innovault.wiki/en/ai/) |
| Narrative Dialogue | `Content/Narrative/` | [en/narrative](https://innovault.wiki/en/narrative/) |
| Advanced Systems | `Content/Advanced/` | [en/advanced](https://innovault.wiki/en/advanced/) |
| Utility Helpers | `Content/Utilities/` | [en/utilities](https://innovault.wiki/en/utilities/) |

The machine names of the chapters (`GettingStarted`, `Particles`, ...) are defined in `Common/DemoCatalog.cs`; catalog grouping and the chapter-title localization keys follow it. Folders under `Content/` mostly share these names, with one exception: the UI chapter's code lives in `Content/Guide/`.

## Comment style

This repo exists to be read, so comment quality matters as much as code quality. All example code follows these rules:

- Chinese comment block on top, a blank `//` line as separator, English block below. The English should carry the meaning; it does not have to be a word-for-word translation.
- Explain the "why", never narrate what the code does. Keep the tone natural and conversational.
- A file header is just two or three plain sentences on what the file demonstrates — no boilerplate field headers or decorative layout — plus one bare wiki link line when a page exists (e.g. `//https://innovault.wiki/en/content/ui-handle/`).
- Use plain technical names (`DemoCatalog`, `GuideBook` — obvious at a glance).

Do this:

```csharp
//粒子生成必须挡掉服务器：PRT 是纯客户端系统，dedServ 上 NewParticle
//只会返回一个不进系统的孤儿实例，白跑还容易误导人。
//
//Guard against the server: PRT is client-only. On dedServ, NewParticle
//returns an orphan that never enters the system.
if (Main.dedServ) return;
```

Not this:

```csharp
//判断 Main.dedServ 是否为真，为真则返回
//
//Check whether Main.dedServ is true, return if so
if (Main.dedServ) return;
```

## Links

- InnoVault source (open source): https://github.com/hocha113/InnoVault
- Tutorial wiki: https://innovault.wiki
- This example repo: https://github.com/hocha113/InnoVaultExample
