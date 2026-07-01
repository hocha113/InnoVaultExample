using InnoVault.GameSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Academy.Overrides
{
    // Showcase: Override Gallery hooks (OVERRIDE-401)
    // Demonstrates: PlayerOverride, TileOverride, SceneOverride, MenuOverride, and GlobalDust
    // Wiki: https://innovault.wiki/cn/overrides/player/
    // Wiki: https://innovault.wiki/cn/overrides/tile/
    // Wiki: https://innovault.wiki/cn/overrides/scene/
    // Wiki: https://innovault.wiki/cn/overrides/menu/
    // Wiki: https://innovault.wiki/cn/overrides/global-dust/
    // Source: InnoVaultExample/Content/Academy/Overrides/AcademyOverrideGallery.cs

    internal class AcademyGalleryPlayerOverride : PlayerOverride
    {
        public override void ModifyGravity(ref float gravity) {
            if (Player.whoAmI == Main.myPlayer && Player.GetModPlayer<AcademyOverrideGalleryPlayer>().GalleryActive) {
                gravity *= 0.82f;
            }
        }

        public override bool On_OnHitNPC(NPC target, in NPC.HitInfo hit, int damageDone) {
            if (Player.whoAmI == Main.myPlayer && Player.GetModPlayer<AcademyOverrideGalleryPlayer>().GalleryActive) {
                for (int i = 0; i < 8; i++) {
                    Dust.NewDustDirect(target.position, target.width, target.height, DustID.BlueTorch, 0f, -1.5f, 120, default, 1.15f).noGravity = true;
                }
            }
            return true;
        }
    }

    internal class AcademyGalleryWorkbenchTileOverride : TileOverride
    {
        public override int TargetID => TileID.WorkBenches;

        public override void MouseOver(int i, int j) {
            if (!AcademyOverrideGalleryPlayer.IsLocalGalleryActive) {
                return;
            }

            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<AcademyOverrideLens>();
        }

        public override bool? RightClick(int i, int j, Tile tile) {
            if (!AcademyOverrideGalleryPlayer.IsLocalGalleryActive) {
                return null;
            }

            CombatText.NewText(Main.LocalPlayer.Hitbox, Color.LightSkyBlue, "TileOverride");
            for (int k = 0; k < 10; k++) {
                Dust.NewDustPerfect(new Vector2(i * 16 + 8, j * 16 + 4), DustID.BlueTorch, Main.rand.NextVector2Circular(1.8f, 1.8f), 120, default, 1.1f).noGravity = true;
            }
            return null;
        }
    }

    internal class AcademyGallerySceneOverride : SceneOverride
    {
        public override void PostUpdateAudio() {
            if (Main.gameMenu || !AcademyOverrideGalleryPlayer.IsLocalGalleryActive) {
                return;
            }

            // Keep this sample non-invasive: observe the audio hook cadence without replacing the selected music.
            if (Main.GameUpdateCount % 180 == 0) {
                Main.LocalPlayer.GetModPlayer<AcademyOverrideGalleryPlayer>();
            }
        }
    }

    internal class AcademyGalleryMenuOverride : MenuOverride
    {
        private int _logicTicks;

        public override void MenuLogicUpdate() {
            _logicTicks++;
        }

        public override void PostDrawMenu(GameTime gameTime) {
            DrawMenuBadge("MenuOverride", _logicTicks);
        }

        public override void PostDrawModMenu(GameTime gameTime, Color color, float logoRotation, float logoScale) {
            DrawMenuBadge("ModMenu hook", _logicTicks);
        }

        private static void DrawMenuBadge(string label, int ticks) {
            SpriteBatch spriteBatch = Main.spriteBatch;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 position = new(24f, Main.screenHeight - 54f);
            string text = $"{label} · {ticks / 60}s";
            Utils.DrawBorderString(spriteBatch, text, position, Color.LightSkyBlue, 0.82f);
        }
    }

    internal class AcademyGalleryDustOverride : GlobalDust
    {
        public override void OnSpawn(Dust dust) {
            if (!AcademyOverrideGalleryPlayer.IsLocalGalleryActive || dust.type != DustID.BlueTorch) {
                return;
            }

            float pulse = AcademyOverrideGalleryPlayer.LocalPulse;
            dust.color = Color.Lerp(Color.CornflowerBlue, Color.LightGoldenrodYellow, pulse);
            dust.scale *= 1.15f;
            dust.noGravity = true;
        }
    }
}
