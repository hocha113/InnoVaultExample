using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.ExamplePRTs
{
    //现在让我们来做一个更加复杂的粒子，它也会更加的酷，这个粒子将模拟飘散的火星效果
    //它在StarFireSword里面被生成，去拿上这把剑来看看效果
    internal class ExamplePRT_StarFire : ExamplePRT
    {
        // 重写Texture属性，将路径指向星火粒子的纹理资源
        public override string Texture => "InnoVaultExample/Asset/DiffusionCircle";

        // 粒子的颜色数组，分别表示亮色、过渡色和暗色
        public Color[] particleColors;

        // 粒子的存活时间、速度、缩放、透明度等变量
        public int lifeTimer;
        public float horizontalSpeed;
        public float speedMultiplier;
        public int maxLifetime;
        public int minLifetime;
        public float scale;
        private float opacity;
        private float remainingLifeTime;

        // 设置粒子的属性
        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend; // 设定绘制模式为“加法混合”

            // 如果颜色未初始化，则赋值
            if (particleColors == null) {
                particleColors = new Color[3];
                particleColors[0] = new Color(262, 150, 45, 255); // 亮色
                particleColors[1] = new Color(186, 35, 24, 255);  // 过渡色
                particleColors[2] = new Color(122, 24, 36, 255);  // 暗色，最终颜色
            }

            // 设置粒子的最小和最大存活时间
            minLifetime = minLifetime == 0 ? 90 : minLifetime;
            maxLifetime = maxLifetime == 0 ? 121 : maxLifetime;

            // 随机生成粒子的存活时间并初始化相关属性
            remainingLifeTime = lifeTimer = Lifetime = Main.rand.Next(minLifetime, maxLifetime);
            lifeTimer = (int)(lifeTimer * Main.rand.NextFloat(0.6f, 1.1f));  // 随机调整初始计时器
            horizontalSpeed = Main.rand.NextFloat(4f, 9f);  // 横向速度
            speedMultiplier = Main.rand.NextFloat(10f, 31f) / 200f;  // 速度倍率
            scale = Main.rand.NextFloat(5f, 11f) / 10f;  // 缩放比例

            if (ai[1] == 1) {
                Lifetime /= 7;  // 若AI状态为1，减少生存时间
            }
            maxLifetime = Lifetime;  // 设置最大存活时间
        }

        // 粒子的行为逻辑
        public override void AI() {
            if (ai[0] > 0) {
                ai[0]--;  // AI计时器递减
                return;
            }

            // 模拟玩家周围空气流动对粒子的影响
            if (Main.LocalPlayer.DistanceSQ(Position) < 120 * 120) {
                if (--ai[2] <= 0) {
                    Velocity += Main.LocalPlayer.velocity;  // 加入玩家的速度影响
                    ai[2] = 30;  // 每30帧更新一次
                }
            }

            // 根据时间计算粒子的透明度，生存时间过半后逐渐消失
            opacity = MathHelper.Lerp(1f, 0f, (maxLifetime / 2f - remainingLifeTime) / (maxLifetime / 2f));

            // 不同AI状态下的速度处理
            if (ai[1] == 1) {
                return;  // 状态1时，不修改速度
            }
            else if (ai[1] == 2) {
                Velocity *= 0.9f;  // 状态2时，速度逐渐减慢
            }
            else {
                if (lifeTimer == 0) {
                    lifeTimer = Main.rand.Next(50, 100);  // 重置计时器
                    horizontalSpeed = Main.rand.NextFloat(4f, 9f);  // 随机生成新的横向速度
                    speedMultiplier = Main.rand.NextFloat(10f, 31f) / 200f;  // 随机生成速度倍率
                }

                // 计算粒子的横向震荡
                float sineX = (float)Math.Sin(Main.GlobalTimeWrappedHourly * horizontalSpeed);
                Velocity += new Vector2(Main.windSpeedCurrent * (Main.windPhysicsStrength * 3f) * MathHelper.Lerp(1f, 0.1f, Math.Abs(Velocity.X) / 6f), 0f);  // 根据风力调整速度
                Velocity += new Vector2(sineX * speedMultiplier, -Main.rand.NextFloat(1f, 2f) / 100f);  // 横向震荡
                Velocity = new Vector2(MathHelper.Clamp(Velocity.X, -6f, 6f), MathHelper.Clamp(Velocity.Y, -6f, 6f));  // 限制速度范围
            }

            lifeTimer--;
            remainingLifeTime--;
        }

        // 绘制粒子
        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D mainTexture = PRTLoader.PRT_IDToTexture[ID];  // 获取主纹理
            Texture2D starTexture = Mod.Assets.Request<Texture2D>("Asset/StarTexture").Value;  // 获取星星特效的纹理

            // 根据剩余生命时间计算粒子的颜色
            Color emberColor = Color.Lerp(particleColors[0], particleColors[2], (float)(maxLifetime - remainingLifeTime) / maxLifetime) * opacity;

            // 设置缩放比例
            float pixelRatio = 1f / 64f;
            Vector2 drawPos = Position - Main.screenPosition;  // 计算粒子的绘制位置

            // 绘制主粒子
            spriteBatch.Draw(mainTexture, drawPos, new Rectangle(0, 0, 64, 64), emberColor,
                             Rotation, mainTexture.Size() / 2, pixelRatio * 3f * scale * Scale, SpriteEffects.None, 0f);

            // 绘制星星特效（仅在AI状态为0时生效）
            if (ai[1] < 1) {
                spriteBatch.Draw(starTexture, drawPos, null, Color, Rotation, starTexture.Size() / 2, Scale * 0.04f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
