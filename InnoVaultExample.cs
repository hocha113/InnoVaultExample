using InnoVaultExample.Core;
using Terraria.ModLoader;

namespace InnoVaultExample
{
    public class InnoVaultExample : Mod
    {
        /// <summary>True after PostSetupContent; gates MenuGame and other late loaders.</summary>
        public static bool Setup;

        public override void PostSetupContent() {
            Setup = true;
            AcademyRegistry.Initialize();
        }

        public override void Unload() {
            Setup = false;
        }
    }
}
