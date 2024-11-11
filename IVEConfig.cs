using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace InnoVaultExample
{
    internal class IVEConfig : ModConfig
    {
        public static IVEConfig Instance { get; private set; }

        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => Instance = this;

        [BackgroundColor(45, 175, 225, 255)]
        [DefaultValue(true)]
        public bool MenuGame { get; set; }
    }
}
