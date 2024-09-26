using Terraria.Audio;

namespace InnoVaultExample.Content.ExampleUIs.ExampleMenuGame
{
    internal abstract class MenuEnemy : MenuEntity
    {
        public int Life;
        public int invincibleTime;
        public int damage;
        public SoundStyle? HitSound = null;
        public virtual void SetProperty() {

        }

        public virtual void HitPlayer(MenuPlayer player) {

        }

        public virtual void Dead() {

        }
    }
}
