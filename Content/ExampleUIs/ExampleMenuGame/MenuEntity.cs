using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.ExampleUIs.ExampleMenuGame
{
    internal abstract class MenuEntity
    {
        public Vector2 Position;

        public Vector2 Velocity;

        public float Rotation;

        public bool Active;

        public int width;

        public int height;

        public Rectangle HitBox => new Rectangle((int)Position.X, (int)Position.Y, width, height);

        public virtual string Texture => GetType().Namespace.Replace('.', '/') + "/" + GetType().Name;

        public virtual void Update() {

        }

        public virtual void Draw(SpriteBatch spriteBatch) {
            if (!ModContent.HasAsset(Texture)) {
                Rectangle fallback = new Rectangle((int)Position.X - width / 2, (int)Position.Y - height / 2, width, height);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, fallback, Color.White * 0.75f);
                return;
            }

            Asset<Texture2D> asset = ModContent.Request<Texture2D>(Texture);
            if (asset != null) {
                spriteBatch.Draw(asset.Value, Position, null, Color.White, Rotation, asset.Size() / 2, 1, SpriteEffects.None, 0);
            }
        }
    }
}
