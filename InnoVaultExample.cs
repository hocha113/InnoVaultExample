using Terraria.ModLoader;

namespace InnoVaultExample
{
    public class InnoVaultExample : Mod
	{
		public static bool Setup;
        public override void PostSetupContent() {
            Setup = true;
        }
        public override void Unload() {
            Setup = false;
        }
    }
}
