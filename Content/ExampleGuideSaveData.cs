using InnoVault.GameSystem;
using Terraria.ModLoader.IO;

namespace InnoVaultExample.Content
{
    // SaveMod stores mod-level data, not world data; here it keeps usage stats for the demo guide.
    // Trap · SaveMod: DoLoad/DoSave timing lives in AcademyAssetLoader (IVaultLoader), not ModSystem.
    internal class ExampleGuideSaveData : SaveMod
    {
        // 外部只读属性让 UI 可以展示数据，同时避免其他示例随手改内部字段。
        // Read-only access keeps the UI simple without letting other samples mutate fields directly.
        public static int TotalGuideUses => Instance.totalGuideUses;
        public static int TotalActorsSpawned => Instance.totalActorsSpawned;
        public static int PanelOpenCount => Instance.panelOpenCount;

        private static ExampleGuideSaveData Instance => SaveContent<SaveMod>.GetInstance<ExampleGuideSaveData>();

        private int totalGuideUses;
        private int totalActorsSpawned;
        private int panelOpenCount;

        // 这些小方法是有意保留的样板：调用者不需要知道 SaveContent 的实例字典。
        // These tiny methods are intentional; callers do not need to know about SaveContent's instance map.
        public static void RecordGuideUse() {
            Instance.totalGuideUses++;
        }

        public static void RecordActorSpawn() {
            Instance.totalActorsSpawned++;
        }

        public static void RecordPanelOpen() {
            Instance.panelOpenCount++;
        }

        public override void SaveData(TagCompound tag) {
            // SaveData / LoadData 的键名要成对维护；改名会影响已有存档。
            // Keep SaveData / LoadData keys paired; renaming them affects existing saves.
            tag["totalGuideUses"] = totalGuideUses;
            tag["totalActorsSpawned"] = totalActorsSpawned;
            tag["panelOpenCount"] = panelOpenCount;
        }

        public override void LoadData(TagCompound tag) {
            totalGuideUses = tag.GetInt("totalGuideUses");
            totalActorsSpawned = tag.GetInt("totalActorsSpawned");
            panelOpenCount = tag.GetInt("panelOpenCount");
        }
    }
}
