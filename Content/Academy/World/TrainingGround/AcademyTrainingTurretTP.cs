using InnoVault.TileProcessors;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Training Torch Turret TP (WORLD-301)
    // Demonstrates: server-side projectile spawn, idle distance culling, dedServ-safe draw
    // Wiki: https://innovault.wiki/cn/utilities/targeting/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingTurretTP.cs

    internal class AcademyTrainingTurretTP : TileProcessor
    {
        public override int TargetTileID => ModContent.TileType<AcademyTrainingTurretTile>();

        private int cooldown;

        public override void SetProperty() {
            IdleDistance = 2400;
        }

        public override void Update() {
            // Trap · dedServ: Update runs on server; never spawn client FX here.
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }

            Vector2 center = CenterInWorld;
            if (Main.LocalPlayer.DistanceSQ(center) > 2000 * 2000) {
                return;
            }

            if (cooldown > 0) {
                cooldown--;
                return;
            }

            NPC target = FindTarget(center, 640f);
            if (target == null) {
                return;
            }

            IEntitySource source = new EntitySource_WorldEvent(nameof(AcademyTrainingTurretTP));
            Vector2 velocity = (target.Center - center).SafeNormalize(Vector2.UnitX) * 14f;
            Projectile.NewProjectile(source, center, velocity, ProjectileID.DemonScythe, 28, 1.5f, Main.myPlayer);
            cooldown = 45;
        }

        public override bool? RightClick(int i, int j, Tile tile, Player player) {
            if (AcademyAuthority.HasAuthority()) {
                AcademyStudentPlayer.Get(player).Progress.MarkTurretVisited();
            }
            return true;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (Main.dedServ) {
                return;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 drawPos = CenterInWorld - Main.screenPosition;
            float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.12f;
            Rectangle rect = new Rectangle((int)(drawPos.X - 6f * pulse), (int)(drawPos.Y - 6f * pulse), (int)(12f * pulse), (int)(12f * pulse));
            spriteBatch.Draw(pixel, rect, null, Color.OrangeRed * 0.55f);
        }

        private static NPC FindTarget(Vector2 center, float maxRange) {
            NPC best = null;
            float bestDist = maxRange * maxRange;

            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.friendly || npc.dontTakeDamage) {
                    continue;
                }

                float dist = npc.DistanceSQ(center);
                if (dist < bestDist) {
                    bestDist = dist;
                    best = npc;
                }
            }

            return best;
        }
    }
}
