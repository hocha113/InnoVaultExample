using InnoVault;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace InnoVaultExample.Core.Assets
{
    // Showcase: Academy Rank Badges (CORE-003)
    // Demonstrates: VaultLoaden indexed arrays, Badge_0 path suffix convention
    // Wiki: https://innovault.wiki/cn/core/vault-loaden
    // Source: InnoVaultExample/Core/Assets/AcademyBadgeAssets.cs

    /// <summary>
    /// Three rank badge frames loaded as a numbered sequence from Assets/Badges/Badge_0.png, etc.
    /// </summary>
    internal static class AcademyBadgeAssets
    {
        [VaultLoaden("Assets/Badges/Badge_", AssetMode.TextureArray)]
        public static Asset<Texture2D>[] RankBadges = new Asset<Texture2D>[3];
    }
}
