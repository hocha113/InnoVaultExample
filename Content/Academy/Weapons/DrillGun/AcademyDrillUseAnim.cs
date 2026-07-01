using InnoVault.GameSystem;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.Weapons.DrillGun
{
    // Showcase: Academy Drill Gun pose (COMBAT-201)
    // Demonstrates: AimedHoldAnimation recoil + MP aim via PlayerNetwork
    // Wiki: https://innovault.wiki/cn/overrides/item-use-animation/
    // Source: InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillUseAnim.cs

    /// <summary>
    /// Replaces vanilla shoot pose while the drill is firing or reloading (itemTime &gt; 0).
    /// </summary>
    internal class AcademyDrillUseAnim : AimedHoldAnimation
    {
        public override int TargetID => AcademyDrillGun.TypeId;

        public override float HoldDistance => 24f;

        public override float RecoilStrength => 7f;

        public override float RecoilPhase => 0.35f;

        // Skip pose takeover while the held projectile handles idle aiming.
        public override bool ShouldAnimate(Item item, Player player) => player.itemTime > 0 || player.itemAnimation > 0;

        // Reload ticks keep the muzzle steady instead of kicking every frame.
        public override bool RecoilActive(Item item, Player player) => player.itemAnimation > 0;
    }
}
