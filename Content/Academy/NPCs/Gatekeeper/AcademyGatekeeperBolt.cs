using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.NPCs.Gatekeeper
{
    // Showcase: Gatekeeper boss projectile (BOSS-201)
    // Demonstrates: lightweight hostile projectile used by the FSM/BT boss sample
    // Wiki: https://innovault.wiki/cn/ai/behavior-trees/
    // Source: InnoVaultExample/Content/Academy/NPCs/Gatekeeper/AcademyGatekeeperBolt.cs

    internal class AcademyGatekeeperBolt : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_687";

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.netMode != NetmodeID.Server) {
                Lighting.AddLight(Projectile.Center, 0.25f, 0.35f, 0.75f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float opacity = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, drawPos, null, new Color(90, 150, 255, 120) * opacity, Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(170, 220, 255), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
