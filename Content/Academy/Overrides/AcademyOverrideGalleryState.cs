using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.Overrides
{
    // Showcase: Override Gallery shared state (OVERRIDE-401)
    // Demonstrates: keeping broad overrides opt-in instead of always mutating gameplay
    // Wiki: https://innovault.wiki/cn/overrides/
    // Source: InnoVaultExample/Content/Academy/Overrides/AcademyOverrideGalleryState.cs

    public class AcademyOverrideGalleryPlayer : ModPlayer
    {
        public bool GalleryActive { get; private set; }
        public int ActiveTimer { get; private set; }

        public static bool IsLocalGalleryActive
            => Main.LocalPlayer?.active == true
                && Main.LocalPlayer.GetModPlayer<AcademyOverrideGalleryPlayer>().GalleryActive;

        public static float LocalPulse
            => 0.5f + 0.5f * MathF.Sin(Main.GameUpdateCount * 0.08f);

        public void ToggleGallery() {
            GalleryActive = !GalleryActive;
            ActiveTimer = 0;
            CombatText.NewText(Player.Hitbox, GalleryActive ? Color.LightSkyBlue : Color.LightGray, GalleryActive ? "Override Gallery" : "Gallery Off");
        }

        public override void ResetEffects() {
            if (GalleryActive) {
                ActiveTimer++;
            }
        }
    }
}
