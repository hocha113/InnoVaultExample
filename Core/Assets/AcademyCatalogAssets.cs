using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;

namespace InnoVaultExample.Core.Assets
{
    // Showcase: Academy Catalog Chrome (CORE-004)
    // Demonstrates: mixed texture and sound members under one class-level VaultLoaden root
    // Wiki: https://innovault.wiki/cn/core/vault-loaden
    // Source: InnoVaultExample/Core/Assets/AcademyCatalogAssets.cs

    /// <summary>
    /// Catalog panel chrome and open sting; textures share Assets/Catalog/ with member-name paths.
    /// </summary>
    [VaultLoaden("Assets/Catalog/")]
    internal static class AcademyCatalogAssets
    {
        public static Asset<Texture2D> Header = null;
        public static Asset<Texture2D> Tab = null;

        // Member-level tag overrides the class root for this sound file.
        [VaultLoaden("Assets/Sounds/CatalogOpen")]
        public static SoundStyle CatalogOpen = new();
    }
}
