using InnoVault.TileProcessors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;

namespace InnoVaultExample.Content.ExampleTileProcessors
{
    //这个类演示了如何让一个TP实体生成弹幕，它会让世界中的恶魔火把变成炮台
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
            //恶魔火把的子ID是7，所以这里让不是7的火把类型被杀死
            if (TileObjectData.GetTileStyle(Tile) != 7) {
                return true;
            }
            return base.IsDaed();
        }
    }
}
