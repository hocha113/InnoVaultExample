using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace InnoVaultExample.Core.Assets
{
    // Showcase: Academy Emblem Sprites (CORE-002)
    // Demonstrates: class-level VaultLoaden batch, path suffix member names
    // Wiki: https://innovault.wiki/cn/core/vault-loaden
    // Source: InnoVaultExample/Core/Assets/AcademyEmblemAssets.cs

    /// <summary>
    /// Shared academy emblem textures for Catalog UI and future PRT courses.
    /// </summary>
    [VaultLoaden("Assets/Emblem/")]
    internal static class AcademyEmblemAssets
    {
        public static Asset<Texture2D> Emblem = null;
        public static Asset<Texture2D> Seal = null;
        public static Asset<Texture2D> Crest = null;
    }
}
