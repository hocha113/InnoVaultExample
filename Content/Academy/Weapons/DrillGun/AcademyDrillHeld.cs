using InnoVault;
using InnoVault.GameContent.BaseEntity;
using InnoVault.PRT;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Content.ExamplePRTs;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.Weapons.DrillGun
{
    // Showcase: Academy Drill Gun held body (COMBAT-201)
    // Demonstrates: BaseHeldProj lifecycle, magazine/reload, muzzle PRT with dedServ guard
    // Wiki: https://innovault.wiki/cn/content/base-held-proj/
    // Source: InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillHeld.cs

    internal class AcademyDrillHeld : BaseHeldProj
    {
        internal const int MagazineSize = 8;
        internal const int ReloadTicks = 48;
        internal const int MuzzleForward = 34;

        // Placeholder art: clockwork rifle held sheet until academy PNG is bundled.
        public override string Texture => "Terraria/Images/Item_98";

        private ref float FireCooldown => ref Projectile.ai[0];
        private ref float AmmoCount => ref Projectile.ai[1];
        private ref float ReloadTimer => ref Projectile.ai[2];

        public override void SetDefaults() {
            Projectile.width = 44;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void Initialize() {
            AmmoCount = MagazineSize;
            ReloadTimer = 0f;
            FireCooldown = 0f;
        }

        // Keep mouse packets flowing while the player aims, even between shots.
        public override bool CanMouseNet => true;

        public override bool ExtraPreSet() {
            if (Owner.dead || !Owner.active || Item.type != AcademyDrillGun.TypeId) {
                Projectile.Kill();
                NetUpdate();
                return false;
            }
            return true;
        }

        public override bool PreUpdate() {
            if (!DownLeft && !DownRight && ReloadTimer <= 0f) {
                Projectile.Kill();
                return false;
            }

            Projectile.timeLeft = 2;
            return true;
        }

        public override void AI() {
            SetHeld();
            SetDirection();
            UpdatePose();

            if (FireCooldown > 0f) {
                FireCooldown--;
            }

            if (ReloadTimer > 0f) {
                TickReload();
                return;
            }

            if (DownRight && AmmoCount < MagazineSize) {
                BeginReload();
                return;
            }

            if (DownLeft) {
                if (AmmoCount <= 0f) {
                    BeginReload();
                    return;
                }

                TryFire();
            }
        }

        private void UpdatePose() {
            float recoil = FireCooldown > 0f ? MathHelper.Lerp(4f, 0f, 1f - FireCooldown / Item.useTime) : 0f;
            Vector2 forward = UnitToMouseV;
            Projectile.rotation = forward.ToRotation();
            Projectile.Center = Owner.GetPlayerStabilityCenter() + forward * (20f - recoil);
            Owner.itemRotation = Projectile.rotation;
        }

        private void TryFire() {
            if (FireCooldown > 0f) {
                return;
            }

            int useDelay = (int)(Item.useTime / Owner.GetAttackSpeed(Item.DamageType));
            FireCooldown = useDelay;
            AmmoCount--;
            Owner.itemTime = Owner.itemAnimation = useDelay;
            Owner.SetDummyItemTime(useDelay);

            if (!Projectile.IsOwnedByLocalPlayer()) {
                return;
            }

            int boltType = ModContent.ProjectileType<AcademyDrillBolt>();
            Vector2 muzzle = Projectile.Center + UnitToMouseV * MuzzleForward;
            int damage = Owner.GetWeaponDamage(Item);
            float knockback = Owner.GetWeaponKnockback(Item);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), muzzle, UnitToMouseV * Item.shootSpeed, boltType, damage, knockback, Owner.whoAmI);

            if (AcademyAuthority.HasAuthority()) {
                AcademyStudentPlayer.Get(Owner).Progress.MarkCourseCombat201Complete();
            }

            SpawnMuzzleFx(muzzle);
        }

        private void BeginReload() {
            ReloadTimer = ReloadTicks;
            Owner.itemTime = Owner.itemAnimation = ReloadTicks;
            Owner.SetDummyItemTime(ReloadTicks);
            if (Projectile.IsOwnedByLocalPlayer()) {
                SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.65f, Pitch = 0.15f }, Projectile.Center);
            }
        }

        private void TickReload() {
            ReloadTimer--;
            Owner.itemTime = Math.Max(Owner.itemTime, (int)ReloadTimer);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, (int)ReloadTimer);

            if (ReloadTimer > 0f) {
                return;
            }

            AmmoCount = MagazineSize;
            if (Projectile.IsOwnedByLocalPlayer()) {
                SoundEngine.PlaySound(SoundID.Item40 with { Volume = 0.5f, Pitch = -0.1f }, Projectile.Center);
            }
        }

        private void SpawnMuzzleFx(Vector2 muzzle) {
            // Trap · dedServ: PRTLoader still returns an orphan on dedServ; never call it there.
            if (Main.dedServ) {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item41 with { Volume = 0.55f, Pitch = Main.rand.NextFloat(-0.1f, 0.15f) }, muzzle);
            for (int i = 0; i < 5; i++) {
                PRTLoader.NewParticle<ExampleGuideSpark>(
                    muzzle,
                    UnitToMouseV.RotatedByRandom(0.35f) * Main.rand.NextFloat(2f, 5f),
                    new Color(140, 220, 255),
                    Main.rand.NextFloat(0.55f, 0.9f));
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(tex, drawPos, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, flip, 0);
            return false;
        }
    }
}
