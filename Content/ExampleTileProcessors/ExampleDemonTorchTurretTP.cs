using InnoVault.TileProcessors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;

namespace InnoVaultExample.Content.ExampleTileProcessors
{
    // This class demonstrates how to make a TP entity generate a barrage that turns demon torches in the world into turrets
    internal class ExampleDemonTorchTurretTP : TileProcessor
    {
        public override int TargetTileID => TileID.Torches;
        private int Time;
        public override void Update() {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                Vector2 Center = PosInWorld + new Vector2(8, 8);
                if (Main.LocalPlayer.DistanceSQ(Center) > 2000 * 2000) {
                    return;
                }
                if (Time <= 0) {
                    NPC target = null;
                    float lengs = 2000;
                    
                    foreach (var npc in Main.ActiveNPCs) {
                        if (npc.friendly) {
                            continue;
                        }
                        
                        float newLengs = npc.Distance(Center);
                        if (newLengs < lengs) {
                            target = npc;
                            lengs = newLengs;
                        }
                    }

                    if (target != null) {
                        IEntitySource source = new EntitySource_WorldEvent(nameof(ExampleDemonTorchTurretTP));
                        Vector2 shootVect = (target.Center - Center).SafeNormalize(Vector2.One) * 16;
                        Projectile.NewProjectile(source, Center, shootVect, ProjectileID.DemonScythe, 132, 2f, -1);
                        Time = 60;
                    }
                }
                if (Time > 0) {
                    Time--;
                }
            }
        }

        public override bool IsDaed() {
            // Demon Torches have a child ID of 7, so let torches that are not 7 be killed here
            if (TileObjectData.GetTileStyle(Tile) != 7) {
                return true;
            }
            return base.IsDaed();
        }
    }
}
