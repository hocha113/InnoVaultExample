using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Particles
{
    //本章的演示物品，把三条粒子路径拴在一把剑上：持握时全局系统里飘 BasicSpark，
    //左键在光标处炸 StarBurst（命中敌怪时也炸），右键往 PRTGroup 本地池里放一圈蓝火花。
    //
    //The demo item for this chapter, tying all three particle paths to one
    //sword: BasicSpark drifts through the global system while held, StarBurst
    //pops at the cursor on left click (and on the target when a hit lands),
    //and right click drops a ring of blue sparks into the PRTGroup local pool.
    //
    //https://innovault.wiki/cn/content/prt/
    internal class SparkSword : ModItem
    {
        public override string Texture => "InnoVaultExample/Assets/Items/VaultSword";

        public override void SetDefaults() {
            Item.damage = 42;
            Item.DamageType = DamageClass.Melee;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.useTurn = true;
        }

        //放行右键，这样 UseItem 里能按 altFunctionUse 分流出第二套行为
        //
        //Allow right click so UseItem can branch on altFunctionUse for the
        //second behavior.
        public override bool AltFunctionUse(Player player) => true;

        public override void HoldItem(Player player) {
            //PRT 是纯客户端系统，必须在生成前挡掉专用服务器。这里有个反直觉的坑：
            //dedServ 上 NewParticle 返回的不是 null，而是一个没进系统、没跑 SetProperty
            //的孤儿对象，靠判空根本查不出问题，只能靠这句守卫。
            //
            //PRT is client-only, so block the dedicated server before spawning.
            //There is a counter-intuitive trap here: on dedServ, NewParticle
            //returns not null but an orphan that never entered the system and
            //never ran SetProperty — a null check catches nothing, only this
            //guard does.
            if (Main.dedServ) {
                return;
            }

            //HoldItem 每帧都跑，每帧一颗太密，掷个骰子稀释到平均每秒二十颗
            //
            //HoldItem runs every tick; one spark per tick is a blizzard, so dice
            //it down to about twenty per second.
            if (!Main.rand.NextBool(3)) {
                return;
            }

            Vector2 pos = player.Center + Main.rand.NextVector2Circular(26f, 26f);
            Vector2 vel = -Vector2.UnitY.RotatedByRandom(0.7) * Main.rand.NextFloat(0.5f, 2f);
            PRTLoader.NewParticle<BasicSpark>(pos, vel, new Color(255, 190, 90), Main.rand.NextFloat(0.6f, 1.1f));
        }

        public override bool? UseItem(Player player) {
            //UseItem 和 HoldItem 一样会为每个客户端上的每个玩家执行，但 HoldItem 用的
            //player.Center 对远程玩家也是对的，而这里要用的 Main.MouseWorld 只有本机
            //玩家的才是真的——不加这层过滤，联机时别人挥剑会在"你的"鼠标位置炸出星光。
            //
            //Like HoldItem, UseItem executes for every player on every client.
            //HoldItem gets away with it because player.Center is correct for
            //remote players too; Main.MouseWorld, needed here, is only real for
            //the local player. Without this filter, someone else swinging their
            //sword would burst stars at *your* cursor in multiplayer.
            if (Main.dedServ || player.whoAmI != Main.myPlayer) {
                return null;
            }

            if (player.altFunctionUse == 2) {
                //右键：粒子进的是 GroupBurst 的本地池而不是全局系统。
                //Group.NewParticle 在 dedServ 上返回 null（和 PRTLoader 的孤儿行为不同），
                //但守卫仍然是上面那句 dedServ 判断，别依赖返回值。
                //
                //Right click: these go into GroupBurst's local pool, not the
                //global system. Group.NewParticle does return null on dedServ
                //(unlike PRTLoader's orphan behavior), but the guard is still
                //the dedServ check above — don't lean on the return value.
                PRTGroup group = ModContent.GetInstance<GroupBurst>().Sparks;
                for (int i = 0; i < 12; i++) {
                    Vector2 vel = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(2f, 4f);
                    group.NewParticle<BasicSpark>(Main.MouseWorld, vel, new Color(120, 200, 255), Main.rand.NextFloat(0.7f, 1.2f));
                }
            }
            else {
                PRTLoader.NewParticle<StarBurst>(Main.MouseWorld, Vector2.Zero, new Color(255, 170, 60), Main.rand.NextFloat(1.2f, 1.8f));
            }
            return null;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
            if (Main.dedServ) {
                return;
            }

            //近战命中只在攻击者本机判定，这撮星光别的玩家看不到。
            //纯装饰特效不值得为此发包广播，演示就到本机为止。
            //
            //Melee hits are detected on the attacker's client only, so other
            //players never see this burst. A cosmetic effect isn't worth a
            //network packet; the demo stops at the local screen.
            PRTLoader.NewParticle<StarBurst>(target.Center, -target.velocity * 0.2f, new Color(255, 120, 60), Main.rand.NextFloat(0.9f, 1.4f));
        }
    }
}
