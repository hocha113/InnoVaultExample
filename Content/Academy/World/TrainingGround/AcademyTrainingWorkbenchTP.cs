using InnoVault.TileProcessors;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Training Workbench TP (WORLD-301)
    // Demonstrates: SaveData/LoadData, SendData/ReceiveData, hover display, progress hook
    // Wiki: https://innovault.wiki/cn/content/tile-processor/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingWorkbenchTP.cs

    internal class AcademyTrainingWorkbenchTP : TileProcessor
    {
        public override int TargetTileID => ModContent.TileType<AcademyTrainingWorkbenchTile>();

        private Vector2 Center => PosInWorld + new Vector2(Width * 0.5f, Height * 0.35f);
        private Color glowColor = Color.Gold;
        private float pulse;
        private float hoverScore;
        private bool dirtyNet;

        public override void SetProperty() {
            glowColor = Color.Lerp(Color.Gold, Color.CornflowerBlue, Main.rand.NextFloat(0.25f, 0.55f));
            pulse = Main.rand.NextFloat(0.8f, 1.2f);
        }

        public override void SaveData(TagCompound tag) {
            tag["hoverScore"] = hoverScore;
        }

        public override void LoadData(TagCompound tag) {
            hoverScore = tag.GetFloat("hoverScore");
        }

        public override void SendData(ModPacket data) {
            data.Write(hoverScore);
        }

        public override void ReceiveData(BinaryReader reader, int whoAmI) {
            hoverScore = reader.ReadSingle();
        }

        public override void Update() {
            Lighting.AddLight(Center, glowColor.ToVector3() * (0.45f + pulse * 0.15f));

            if ((Main.MouseWorld - Center).LengthSquared() < 48f * 48f) {
                hoverScore += 0.05f;
                dirtyNet = true;
            }

            if (dirtyNet && AcademyAuthority.HasAuthority()) {
                dirtyNet = false;
                SendData();
            }
        }

        public override bool? RightClick(int i, int j, Tile tile, Player player) {
            if (AcademyAuthority.HasAuthority()) {
                AcademyStudentPlayer.Get(player).Progress.MarkWorkbenchVisited();
                if (player.controlDown) {
                    // Trap · sneak-clear flags mutate only on authority peer (SP is not isServer).
                    AcademyTrainingWorldSave.MarkGroundCleared();
                    player.GetModPlayer<AcademyStudentPlayer>().MarkPersonalGroundCleared();
                }
            }
            return true;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (Main.dedServ) {
                return;
            }

            int itemType = Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem].type;
            if (itemType <= ItemID.None) {
                itemType = ItemID.Book;
            }

            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            float bob = MathF.Sin(Main.GameUpdateCount * 0.08f) * 4f;
            Vector2 drawPos = Center - Main.screenPosition + new Vector2(0f, -20f + bob);

            spriteBatch.Draw(texture, drawPos, frame, Color.White * 0.95f, 0f, origin, 0.9f, SpriteEffects.None, 0f);
            Utils.DrawBorderStringFourWay(
                spriteBatch,
                FontAssets.ItemStack.Value,
                hoverScore.ToString("0"),
                drawPos.X,
                drawPos.Y - 28f,
                Color.White,
                Color.Black,
                Vector2.One * 0.25f,
                1.1f);
        }
    }
}
