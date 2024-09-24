using System.Drawing;

namespace InnoVaultExample.Content.ExampleUIs.ExampleMenuGame
{
    internal class MenuProjectile : MenuEntity
    {
        public int Time;
        public bool friend;
        public int hitCoolTime = 60;
        public virtual void SetProperty() {

        }

        public virtual void HitEnemy(MenuEnemy enemy) {

        }

        public virtual void HitPlayer(MenuPlayer player) {

        }

        public static void NewProjectile(MenuProjectile projectile) {
            projectile.Active = true;
            projectile.SetProperty();
            MenuGame.projectiles.Add(projectile);
        }
    }
}
