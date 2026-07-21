using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Terraria;

namespace InnoVaultExample.Content.Particles
{
    //一颗能用的最小粒子：SetProperty 配一次性属性，AI 每帧推运动和淡出，绘制全交给默认管线。
    //继承 BasePRT 就算注册完成，PRTLoader 加载时会反射收集所有子类，不需要任何注册代码。
    //
    //The smallest particle that still feels alive: SetProperty does the one-off
    //setup, AI drives motion and fade each tick, and drawing is left entirely
    //to the default pipeline. Inheriting BasePRT is all the registration there
    //is — PRTLoader collects every subclass by reflection at load time.
    //
    //https://innovault.wiki/cn/content/prt/
    internal class BasicSpark : BasePRT
    {
        //没有重写 Texture，加载器就按"命名空间路径 + 类名"找贴图，
        //也就是 InnoVaultExample/Content/Particles/BasicSpark——同目录同名 png 自动加载。
        //找不到时 InnoVault 会打一条警告并用占位贴图，不会崩。
        //
        //With Texture left alone, the loader looks up "namespace path + class
        //name", i.e. InnoVaultExample/Content/Particles/BasicSpark — the
        //same-name png sitting next to this file is picked up automatically.
        //If it were missing, InnoVault would log a warning and substitute a
        //placeholder instead of crashing.

        //生成时执行一次，角色相当于弹幕的 SetDefaults。混合模式、RenderLayer 这类
        //配置必须放这里：RenderLayer 在 AI 里改要到下一帧才生效。
        //
        //Runs once on spawn, playing the role of a projectile's SetDefaults.
        //One-off config like the blend mode and RenderLayer belongs here:
        //changing RenderLayer inside AI only takes effect a frame later.
        public override void SetProperty() {
            //加法混合让火花在暗处发亮、重叠处自然增亮，最适合火光类特效
            //
            //Additive blending makes sparks glow in the dark and brighten where
            //they overlap — the natural fit for ember effects.
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            Lifetime = Main.rand.Next(24, 60);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        //每帧调用。进到这里时 Position 已经按 Velocity 挪过、Time 已经加过一，
        //寿命到点（Time >= Lifetime）也由系统自动回收，AI 里只需要管"往哪飘、怎么淡"。
        //
        //Called every tick. By the time we get here Position has already moved
        //by Velocity and Time has already incremented; expiry (Time >= Lifetime)
        //is handled by the system too, so AI only decides drift and fade.
        public override void AI() {
            Velocity *= 0.95f;
            Velocity.Y += 0.06f;
            Rotation += Velocity.X * 0.1f;

            //加法混合下 alpha 直接缩放亮度，拿寿命进度当淡出曲线，省一个字段
            //
            //Under additive blending alpha scales brightness directly, so the
            //lifetime ratio doubles as the fade-out curve for free.
            Color.A = (byte)(255 * (1f - LifetimeCompletion));
        }
    }
}
