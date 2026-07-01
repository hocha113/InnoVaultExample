using InnoVault;
using InnoVault.GameSystem;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.Weapons.DrillGun
{
    // Showcase: Academy drill bolt (COMBAT-201)
    // Demonstrates: ProjOverride homing + pierce, Trail ribbon on projectile
    // Wiki: https://innovault.wiki/cn/overrides/projectile/
    // Wiki: https://innovault.wiki/cn/content/trails/
    // Source: InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillBolt.cs

    internal class AcademyDrillBolt : ModProjectile
    {
        internal static int TypeId => ModContent.ProjectileType<AcademyDrillBolt>();

        // Placeholder art: cyan laser bolt until custom PNG is added.
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = AcademyDrillTrail.PointCount;
        }

        public override void SetDefaults() {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.35f, 0.75f, 1f);
        }

        public override bool PreDraw(ref Color lightColor) {
            AcademyDrillTrail.DrawBoltTrail(Projectile, new Color(120, 210, 255, 180));

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(160, 230, 255),
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);
            return false;
        }
    }

    /// <summary>
    /// ProjOverride layer: bolt tuning and homing without duplicating the ModProjectile type.
    /// </summary>
    internal class AcademyDrillBoltOverride : ProjOverride
    {
        private const float HomingDelay = 10f;
        private const float HomingRange = 360f;

        public override int TargetID => AcademyDrillBolt.TypeId;

        public override void SetProperty() {
            projectile.penetrate = 4;
            projectile.ai[0] = 0f;
        }

        public override bool AI() {
            projectile.ai[0]++;

            if (projectile.ai[0] < HomingDelay) {
                return false;
            }

            NPC target = projectile.Center.FindClosestNPC(HomingRange, ignoreTiles: true, chasedByNPC: npc => npc.CanBeChasedBy(projectile));
            if (target != null) {
                projectile.velocity = projectile.SmoothHomingBehavior(target.Center, 1.04f, 0.12f);
            }

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (AcademyAuthority.HasAuthority()) {
                Player owner = Main.player[projectile.owner];
                if (owner.active) {
                    AcademyStudentPlayer.Get(owner).Progress.MarkCourseCombat201Complete();
                }
            }

            if (Main.dedServ) {
                return;
            }

            for (int i = 0; i < 4; i++) {
                Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Electric, 0f, 0f, 100, default, 1.1f).noGravity = true;
            }
        }
    }
}
