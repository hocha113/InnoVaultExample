using Microsoft.Xna.Framework;
using Terraria;

namespace InnoVaultExample.Content.ExampleUIs.ExampleMenuGame.Projectile
{
    internal class Trident : MenuProjectile
    {
        public override void SetProperty() {
            Time = 120;
            width = height = 24;
            friend = true;
        }
        public override void Update() {
            Rotation = Velocity.ToRotation() + MathHelper.PiOver4;
        }

        public override void HitEnemy(MenuEnemy enemy) {
            enemy.Life -= 25;
            enemy.Velocity += Velocity;
        }
    }
}
