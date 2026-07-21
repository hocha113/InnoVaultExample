using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.AI
{
    //秘库看守者齐射用的弹幕：直线飞行，出膛后的一小段窗口内轻微追踪玩家。
    //视觉完全交给尘埃拖尾，弹体本身不画贴图。ai[0] 由 Boss 在生成时写入，
    //1 表示狂暴配色。
    //
    //The Vault Warden's volley bolt: flies straight with a gentle homing nudge
    //during a short window after launch. Visuals are all dust trail; the body
    //itself draws no sprite. ai[0] is written by the boss at spawn, 1 meaning
    //the enraged tint.
    //
    //https://innovault.wiki/cn/ai/behavior-trees/

    internal class DemoBossBolt : ModProjectile
    {
        //弹体不画贴图，挂一张原版占位图让资源管线闭嘴
        //
        //No sprite gets drawn; the vanilla placeholder keeps the asset
        //pipeline satisfied
        public override string Texture => "Terraria/Images/Projectile_0";

        private bool Enraged => Projectile.ai[0] == 1f;

        public override void SetDefaults() {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            //extraUpdates = 1 让弹幕每帧走两步：出手速度可以写得温和，
            //实际飞行又够快，追踪的角速度也随之翻倍
            //
            //extraUpdates = 1 moves the bolt twice per frame: the muzzle speed
            //stays gentle on paper while actual flight is quick, and the homing
            //turn rate effectively doubles with it
            Projectile.extraUpdates = 1;
        }

        public override void AI() {
            //引导计时放 localAI（不参与网络同步）。敌对弹幕在每个端各自模拟，
            //追踪目标取的是双方都同步持有的玩家位置，同一算式两端结果一致，
            //不需要为它添置任何包
            //
            //The homing timer lives in localAI (never synced). Hostile
            //projectiles simulate independently on each side; the steering
            //target is a player position both sides already share, so the same
            //formula converges without any extra packets.
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] < 80f) {
                int nearest = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
                if (nearest >= 0) {
                    Player target = Main.player[nearest];
                    if (target.active && !target.dead) {
                        float currentAngle = Projectile.velocity.ToRotation();
                        float desiredAngle = (target.Center - Projectile.Center).ToRotation();
                        //每步最多转 0.02 弧度——只够修正瞄偏，逼不死横向走位的玩家
                        //
                        //At most 0.02 radians per step — enough to correct the
                        //aim, never enough to run down a strafing player
                        float turned = MathHelper.WrapAngle(desiredAngle - currentAngle);
                        turned = MathHelper.Clamp(turned, -0.02f, 0.02f);
                        Projectile.velocity = (currentAngle + turned).ToRotationVector2() * Projectile.velocity.Length();
                    }
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (!Main.dedServ) {
                int dustType = Enraged ? DustID.RedTorch : DustID.Shadowflame;
                for (int i = 0; i < 2; i++) {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                        dustType, -Projectile.velocity * 0.25f, 100, default, 1.25f);
                    d.noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, Enraged ? 0.5f : 0.25f, 0.15f, 0.5f);
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 8; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, Enraged ? DustID.RedTorch : DustID.Shadowflame,
                    Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 1.4f);
                d.noGravity = true;
            }
        }

        //贴图是占位符，直接跳过绘制，弹道由拖尾尘埃呈现
        //
        //The texture is a placeholder; skip drawing entirely and let the dust
        //trail carry the visuals
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
