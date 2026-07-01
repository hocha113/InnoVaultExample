using InnoVault.DataModules;
using InnoVaultExample.Core;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InnoVaultExample.Content.Academy.Persistence
{
    // Showcase: Academy Student Player Save (WORLD-301)
    // Demonstrates: DataModuleStore on ModPlayer, SavePlayer hook, server-guarded flags
    // Wiki: https://innovault.wiki/cn/persistence/data-modules/
    // Source: InnoVaultExample/Content/Academy/Persistence/AcademyStudentPlayer.cs

    public class AcademyStudentPlayer : ModPlayer
    {
        private const string StoreKey = "academyModules";
        private const string PersonalClearKey = "personalGroundCleared";

        private readonly DataModuleStore _modules = new();

        public AcademyProgressModule Progress => _modules.Get<AcademyProgressModule>();
        public bool PersonalGroundCleared { get; private set; }

        public static AcademyStudentPlayer Get(Player player) => player.GetModPlayer<AcademyStudentPlayer>();

        public void MarkPersonalGroundCleared() {
            // Trap · isServer alone skips SinglePlayer; use AcademyAuthority.HasAuthority().
            if (!AcademyAuthority.HasAuthority()) {
                return;
            }

            PersonalGroundCleared = true;
        }

        public override void SaveData(TagCompound tag) {
            TagCompound storeTag = [];
            _modules.SaveData(storeTag);
            tag[StoreKey] = storeTag;
            tag[PersonalClearKey] = PersonalGroundCleared;
        }

        public override void LoadData(TagCompound tag) {
            if (tag.TryGet(StoreKey, out TagCompound storeTag)) {
                _modules.LoadData(storeTag);
            }

            PersonalGroundCleared = tag.GetBool(PersonalClearKey);
        }

        //public override void CopyClientState(ModPlayer clientClone) {
        //    if (clientClone is not AcademyStudentPlayer other) {
        //        return;
        //    }

        //    other._modules.Clear();
        //    foreach (DataModule module in _modules.Modules) {
        //        other._modules.Add(module.Clone());
        //    }

        //    other.PersonalGroundCleared = PersonalGroundCleared;
        //}
    }
}
