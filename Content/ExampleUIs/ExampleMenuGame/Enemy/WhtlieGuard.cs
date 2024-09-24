using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace InnoVaultExample.Content.ExampleUIs.ExampleMenuGame.Enemy
{
    internal class WhtlieGuard : MenuEnemy
    {
        public override void SetProperty() {
            damage = 20;
            Life = 100;
            width = height = 24;
            HitSound = SoundID.NPCHit4;
        }
        public override void Update() {
            MenuPlayer player = MenuGame.Instance.menuPlayer;
            Velocity = (player.Position - Position).SafeNormalize(Vector2.Zero);
        }
        public override void Draw(SpriteBatch spriteBatch) {
            base.Draw(spriteBatch);
            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.ItemStack.Value, Life.ToString()
                , Position.X, Position.Y + 25, Color.White, Color.Black, new Vector2(0.3f), 1f);
        }
    }
}
