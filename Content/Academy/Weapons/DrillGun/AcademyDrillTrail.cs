using InnoVault.Trails;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace InnoVaultExample.Content.Academy.Weapons.DrillGun
{
    // Showcase: Drill bolt ribbon (COMBAT-201)
    // Demonstrates: Trail mesh without bundled Effect (ColoredVertex path)
    // Wiki: https://innovault.wiki/cn/content/trails/
    // Source: InnoVaultExample/Content/Academy/Weapons/DrillGun/AcademyDrillTrail.cs

    /// <summary>
    /// Client-only trail drawer for academy drill bolts. Call from PreDraw behind a dedServ guard.
    /// </summary>
    internal static class AcademyDrillTrail
    {
        public const int PointCount = 14;

        public static void DrawBoltTrail(Projectile projectile, Color coreColor) {
            // Trap · dedServ: Trail builds GPU mesh data; skip entirely on dedicated servers.
            if (Main.dedServ) {
                return;
            }

            Vector2[] points = BuildPositions(projectile);
            if (points == null) {
                return;
            }

            List<Vector2> path = [.. points];

            Trail.GenerateInterleavedMesh(
                path,
                progress => (1f - progress) * 10f * projectile.scale,
                uv => coreColor * (1f - uv.X * 0.85f),
                out ColoredVertex[] vertices,
                out short[] indices);

            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            Trail.DrawUserPrimitives(vertices, indices);
            Main.instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        }

        private static Vector2[] BuildPositions(Projectile projectile) {
            if (projectile.oldPos == null || projectile.oldPos.Length == 0) {
                return null;
            }

            int count = System.Math.Min(PointCount, projectile.oldPos.Length);
            Vector2[] points = new Vector2[count];

            // TrailPositions[^1] = tip (current center), [0] = oldest sample.
            points[^1] = projectile.Center + projectile.Size * 0.5f;
            for (int i = 0; i < count - 1; i++) {
                Vector2 sample = projectile.oldPos[i];
                if (sample == Vector2.Zero) {
                    sample = projectile.Center;
                }
                points[count - 2 - i] = sample + projectile.Size * 0.5f;
            }

            return points;
        }
    }
}
