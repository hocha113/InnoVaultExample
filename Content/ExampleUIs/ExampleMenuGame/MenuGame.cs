using InnoVault;
using InnoVault.UIHandles;
using InnoVaultExample.Content.ExampleUIs.ExampleMenuGame.Enemy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InnoVaultExample.Content.ExampleUIs.ExampleMenuGame
{
    //这是一个较为复杂的演示案例，它演示了如何修改主页内容并制作一个小小的游戏
    internal class MenuGame : UIHandle
    {
        public override LayersModeEnum LayersMode => LayersModeEnum.Mod_MenuLoad;
        public static MenuGame Instance => (MenuGame)UIHandleLoader.GetUIHandleInstance<MenuGame>();
        public static List<MenuEnemy> enemies = [];
        public static List<MenuProjectile> projectiles = [];
        public override bool Active => InnoVaultExample.Setup;
        public MenuPlayer menuPlayer;
        private int GameTime;
        public override void Load() {
            enemies = VaultUtils.GetSubclassInstances<MenuEnemy>();
            projectiles = VaultUtils.GetSubclassInstances<MenuProjectile>();
            menuPlayer = new MenuPlayer();
            menuPlayer.width = menuPlayer.height = 24;
            menuPlayer.Position = new Vector2(Main.screenWidth, Main.screenHeight) / 2;
        }

        public static void ProjUpdate() {
            foreach (var proj in projectiles) {
                if (!proj.Active) {
                    continue;
                }
                proj.Update();
                proj.Position += proj.Velocity;

                if (proj.friend) {
                    foreach (var npc in enemies) {
                        if (!npc.Active) {
                            continue;
                        }
                        if (--npc.invincibleTime > 0) {
                            continue;
                        }
                        if (npc.HitBox.Intersects(proj.HitBox)) {
                            proj.HitEnemy(npc);
                            if (npc.HitSound.HasValue) {
                                SoundEngine.PlaySound(npc.HitSound.Value);
                            }
                            npc.invincibleTime = proj.hitCoolTime;
                        }
                    }
                }
                else {
                    if (Instance.menuPlayer.HitBox.Intersects(proj.HitBox)) {
                        proj.HitPlayer(Instance.menuPlayer);
                    }
                }

                if (--proj.Time <= 0) {
                    proj.Active = false;
                }
            }

            projectiles.RemoveAll(proj => !proj.Active);
        }

        public static void EnemiesUpdate() {
            foreach (var enemie in enemies) {
                if (!enemie.Active) {
                    continue;
                }

                enemie.Update();
                enemie.Position += enemie.Velocity;

                if (Instance.menuPlayer.invincibleTime <= 0) {
                    if (Instance.menuPlayer.HitBox.Intersects(enemie.HitBox)) {
                        enemie.HitPlayer(Instance.menuPlayer);
                        SoundEngine.PlaySound(SoundID.PlayerHit);
                        Instance.menuPlayer.Life -= enemie.damage;
                        if (Instance.menuPlayer.Life < 0) {
                            Instance.menuPlayer.Integral = 0;
                            Instance.menuPlayer.Life = 0;
                        }
                        Instance.menuPlayer.invincibleTime = 60;
                    }
                }

                if (enemie.Life <= 0) {
                    Instance.menuPlayer.Integral++;
                    Instance.menuPlayer.Life += 10;
                    if (Instance.menuPlayer.Life > 500) {
                        Instance.menuPlayer.Life = 500;
                    }
                    enemie.Dead();
                    enemie.Active = false;
                }
            }

            enemies.RemoveAll(enemie => !enemie.Active);
        }

        public override void Update() {
            if (!IVEConfig.Instance.MenuGame) {
                return;
            }
            if (Main.rand.NextBool(120) && enemies.Count < 40) {
                WhtlieGuard guard = new WhtlieGuard();
                guard.Active = true;
                guard.Position = new Vector2(Main.rand.Next(0, Main.screenWidth), -30);
                guard.Velocity = new Vector2(0, 2);
                guard.SetProperty();
                enemies.Add(guard);
            }
            menuPlayer.Update();
            ProjUpdate();
            EnemiesUpdate();
            if (Instance.menuPlayer.invincibleTime > 0) {
                Instance.menuPlayer.invincibleTime--;
            }
            GameTime++;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IVEConfig.Instance.MenuGame) {
                return;
            }
            menuPlayer.Draw(spriteBatch);
            foreach (var proj in projectiles) {
                if (!proj.Active) {
                    continue;
                }
                proj.Draw(spriteBatch);
            }

            foreach (var enemie in enemies) {
                if (!enemie.Active) {
                    continue;
                }
                enemie.Draw(spriteBatch);
            }
        }
    }
}
