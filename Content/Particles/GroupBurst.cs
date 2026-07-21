using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Particles
{
    //PRTGroup 是全局粒子系统之外的本地粒子池：不占全局槽位、不受全局上限约束、
    //GlobalPRT 的 OnSpawn 也管不到它。代价是 Update 和 Draw 的时机全得宿主自己安排——
    //这个 ModSystem 就是最小的宿主形态。火花剑右键会往 Sparks 里塞一圈粒子。
    //
    //PRTGroup is a local particle pool outside the global system: it takes no
    //global slots, ignores the global cap, and GlobalPRT.OnSpawn never sees it.
    //The price is that the host must schedule Update and Draw itself — this
    //ModSystem is the smallest possible host. The spark sword's right click
    //feeds a ring of particles into Sparks.
    //
    //https://innovault.wiki/cn/content/prt/
    internal class GroupBurst : ModSystem
    {
        //PRTGroup 不是 ModType，就是个普通对象，谁想拥有几个都行；
        //挂在 ModSystem 上是为了让它的生命周期跟着模组走，卸载时随实例一起消失。
        //
        //A PRTGroup is not a ModType, just a plain object anyone can own; hanging
        //it on a ModSystem ties its lifetime to the mod so it vanishes with the
        //instance on unload.
        public PRTGroup Sparks { get; } = new();

        //每逻辑帧喂一次。Group.Update 内部自带 dedServ 拦截和死亡粒子清理，
        //宿主不用再包一层守卫。
        //
        //Feed it once per logic tick. Group.Update guards dedServ and prunes
        //dead particles internally, so the host needs no extra wrapper.
        public override void PostUpdateEverything() {
            Sparks.Update();
        }

        //绘制时机也归宿主。全局系统会按每颗粒子的 PRTDrawMode 自动分桶开批次，
        //本地池不会——混合模式由这里的 Begin 一锤定音。BasicSpark 按加法混合设计，
        //所以整组用 Additive；矩阵用 GameViewMatrix，Group.Draw 内部按世界坐标算屏幕位置。
        //
        //Drawing is the host's job too. The global system buckets particles by
        //their PRTDrawMode; a local pool does not — whatever this Begin says is
        //the blend mode for the whole group. BasicSpark is designed for additive
        //blending, so Additive it is. The matrix is GameViewMatrix because
        //Group.Draw converts world positions to screen space itself.
        public override void PostDrawTiles() {
            if (!Sparks.Any()) {
                return;
            }
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Sparks.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
        }

        //粒子位置是世界坐标，换世界不清空的话，旧粒子会在新世界的同一坐标处显形
        //
        //Positions are world coordinates: without this, leftover particles from
        //the previous world would pop up at the same spot in the next one.
        public override void OnWorldUnload() {
            Sparks.Clear();
        }
    }
}
