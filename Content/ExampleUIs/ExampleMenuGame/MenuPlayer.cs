using InnoVaultExample.Content.ExampleUIs.ExampleMenuGame.Projectile;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.ExampleUIs.ExampleMenuGame
{
    internal class MenuPlayer : MenuEntity
    {
        private int fireIndex;
        public int invincibleTime;
        public int Life = 200;
        public int Integral;

        public void SpwanTridentAtk() {
            SoundEngine.PlaySound(SoundID.Item46);
            Trident trident = new Trident();
            trident.Position = Position;
            trident.Velocity = (MenuGame.Instance.MousePosition - Position).SafeNormalize(Vector2.Zero) * 6;
            MenuProjectile.NewProjectile(trident);
        }

        public override void Update() {
            Position = Vector2.Lerp(Position, MenuGame.Instance.MousePosition, 0.02f);

            if (++fireIndex > 30) {
                SpwanTridentAtk();
                fireIndex = 0;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            base.Draw(spriteBatch);

            Asset<Texture2D> asset = ModContent.Request<Texture2D>("InnoVaultExample/Content/ExampleUIs/ExampleMenuGame/PlayerData");
            if (asset != null) {
                spriteBatch.Draw(asset.Value, new Vector2(0, 0), null, Color.White, Rotation, Vector2.Zero, 1, SpriteEffects.None, 0);
            }

            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.ItemStack.Value, Life.ToString()
                , 40, 10, Color.White, Color.Black, new Vector2(0.3f), 1.4f);

            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.ItemStack.Value, Integral.ToString()
                , 40, 60, Color.White, Color.Black, new Vector2(0.3f), 1.4f);
        }
    }
}
