using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InnoVaultExample.Content.ExamplePRTs
{
    //我们的第一个PRT粒子，很酷不是吗，它在VaultSword里面被生成，去拿上这把剑来看看效果
    internal class ExamplePRT : BasePRT
    {
        //Texture属性可以不用重写，BasePRT拥有自动加载机制，自动加载同路径下的同名.png，这一点和ModProjectile类似
        //所以，让我们准备一张叫"ExamplePRT"的png文件，也就是一张文件名和类名相同的图片
        //public override string Texture => base.Texture;

        //重写这个函数，它会在粒子被生成的时候调用一次，PRT实体都是独立的实例
        //因此这个函数的设置也可以独立作用到每个实例上，在作用上和ModProjectile.SetDefaults类似
        public override void SetProperty() {
            //PRTDrawMode决定了这个实例会被合批到那个绘制模式中，这个决定了粒子的绘制颜色模式
            //在这里我们将其设置为加法混合模式，这个字段所带来的更新效果是实时的，每次绘制都会重写对所有PRT实例进行合批
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            SetLifetime = true;//将这个值设置为true，粒子将受到寿命限制
            Lifetime = Main.rand.Next(220, 360);//220到360tick的寿命
        }

        public override void AI() {
            //一个简单的随机变化方向的AI
            if (--ai[0] <= 0) {
                Velocity = Velocity.RotatedByRandom(0.3f);
                ai[0] = Main.rand.Next(10);
            }
            //随移动方向进行角度选择
            Rotation += Velocity.X * 0.02f;

            ////相对坐标变化
            //Position += Main.LocalPlayer.velocity;

            //进行一个有趣的亮度变换
            Color.A = (byte)(1 - LifetimeCompletion * 255f);
            //在生命末期，进行淡化处理
            if (LifetimeCompletion > 0.8f) {
                Color *= 0.9f;
            }
        }

        //重写这个绘制函数，如果你希望自定义绘制，让这个函数返回false，默认的绘制就不会出现
        public override bool PreDraw(SpriteBatch spriteBatch) => true;
    }
}
