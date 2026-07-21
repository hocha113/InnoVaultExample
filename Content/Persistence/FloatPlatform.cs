using InnoVault.Actors;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Persistence
{
    //缓慢往返漂浮的平台，玩家可以站上去被带着走。SolidActor 把自己的碰撞箱注入到
    //Terraria 的物块碰撞结算里，站立、阻挡、钩爪勾附全部自动生效，一行碰撞代码都不用写。
    //
    //A slowly patrolling platform the player can stand on and ride. SolidActor
    //injects its hitbox into Terraria's tile collision resolution, so standing,
    //blocking and grappling all just work - not a single line of collision code here.
    //https://innovault.wiki/cn/persistence/solid-actor/

    //多人语义：平台位置是服务端权威（Actor 内置 SyncVar 广播、客户端平滑收敛），
    //而碰撞与承载在每个客户端本地解算——玩家位置本来就由原版网络同步，
    //所以完全不需要自己同步"谁站在平台上"，这正好贴合原版的权威模型。
    //
    //Multiplayer semantics: the platform position is server-authoritative (built-in
    //Actor SyncVars broadcast it, clients converge smoothly), while collision and
    //carrying resolve locally on each client - player positions are already synced
    //by vanilla netcode, so "who stands on the platform" needs no syncing at all.
    internal class FloatPlatformActor : SolidActor
    {
        private const float PatrolRange = 160f;
        private const float PatrolSpeed = 1.2f;

        //巡逻原点和方向是纯内部 AI 状态，不是 SyncVar。它们靠 SendExtraData /
        //ReceiveExtraData 进入生成包与晚加入快照：中途连入的玩家直接拿到权威原点，
        //而不是拿"入场时平台恰好在哪"反推一个错的原点，那会让往返区间永久错位。
        //
        //Patrol origin and direction are internal AI state, not SyncVars. They travel
        //in the spawn packet and late-join snapshot via SendExtraData /
        //ReceiveExtraData: a late joiner receives the authoritative origin instead of
        //deriving a wrong one from wherever the platform happened to be, which would
        //skew the patrol range forever.
        private float originX;
        private int moveDir = 1;

        public override void OnSpawn(params object[] args) {
            Width = 120;
            Height = 16;

            //OneWay 只挡顶面：从下方可以跳穿上来，按下加跳可以穿下去（AllowFallThrough
            //控制）。四面固体在"平台横向把玩家挤向墙壁"这类极端情形可能卡人，
            //单向平台没有这个问题，能站的移动物优先选它。
            //
            //OneWay blocks the top face only: jump through from below, press down+jump
            //to drop through (AllowFallThrough controls that). A four-sided solid can
            //trap players in edge cases like squeezing them against a wall; one-way
            //platforms cannot, so prefer them for anything ridable.
            OneWay = true;
            AllowFallThrough = true;
            DrawLayer = ActorDrawLayer.AfterTiles;
            originX = Position.X;
        }

        //移动逻辑写在 SolidAI 而不是 AI：基类把 AI 密封掉了，用来在移动前记录
        //LastPosition、算出本帧位移 FrameVelocity——承载玩家和钩爪跟随全靠这个量。
        //承载本身由框架的 SolidActorPlayer 完成：玩家站上顶面时，
        //平台的 FrameVelocity 会被叠加到玩家身上，"被带着走"不需要这里写任何代码。
        //
        //Movement goes in SolidAI, not AI: the base class seals AI to record
        //LastPosition before moving and derive FrameVelocity, the per-frame
        //displacement that player carrying and grapple following depend on. Carrying
        //itself is done by the framework's SolidActorPlayer: when a player stands on
        //top, the platform's FrameVelocity is applied to them - riding needs zero
        //code here.
        public override void SolidAI() {
            Velocity = new Vector2(moveDir * PatrolSpeed, 0f);

            //换向判断在每个端各自跑，短暂分叉没关系：位置由权威状态平滑收敛，
            //阈值又是确定性的，客户端很快会和服务器重新对齐
            //
            //The turnaround check runs independently on every peer; brief divergence
            //is fine - position reconciles toward the authority and the threshold is
            //deterministic, so clients realign with the server quickly
            if (Position.X > originX + PatrolRange) {
                moveDir = -1;
            }
            else if (Position.X < originX - PatrolRange) {
                moveDir = 1;
            }
        }

        //读写顺序必须与 ReceiveExtraData 完全一致，这一对总是成对维护
        //
        //Write order must match ReceiveExtraData exactly; maintain the pair together
        public override void SendExtraData(BinaryWriter writer) {
            writer.Write(originX);
            writer.Write((sbyte)moveDir);
        }

        public override void ReceiveExtraData(BinaryReader reader) {
            originX = reader.ReadSingle();
            moveDir = reader.ReadSByte();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (Main.dedServ) {
                return false;
            }

            //用纯色像素拼一块板子：主体、顶缘亮条、两端包边，够看清碰撞范围就好
            //
            //A plain-pixel plank: body, a bright top edge and end caps - just enough
            //to read the collision bounds at a glance
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle box = HitBox;
            Vector2 screenTopLeft = Position - Main.screenPosition;

            spriteBatch.Draw(pixel, new Rectangle((int)screenTopLeft.X, (int)screenTopLeft.Y, box.Width, box.Height)
                , new Rectangle(0, 0, 1, 1), new Color(94, 74, 52) * 0.95f);
            spriteBatch.Draw(pixel, new Rectangle((int)screenTopLeft.X, (int)screenTopLeft.Y, box.Width, 4)
                , new Rectangle(0, 0, 1, 1), new Color(214, 186, 120));
            spriteBatch.Draw(pixel, new Rectangle((int)screenTopLeft.X, (int)screenTopLeft.Y, 4, box.Height)
                , new Rectangle(0, 0, 1, 1), new Color(140, 110, 76));
            spriteBatch.Draw(pixel, new Rectangle((int)screenTopLeft.X + box.Width - 4, (int)screenTopLeft.Y, 4, box.Height)
                , new Rectangle(0, 0, 1, 1), new Color(140, 110, 76));

            return false;
        }
    }

    //平台杖：在鼠标位置放一块漂浮平台。生成守卫与信标杖同款——只由操作者本人的
    //那一端调 NewActor，客户端返回 -1、由框架路由到服务器。贴图借用原版木平台。
    //
    //The platform rod: places a floating platform at the cursor. Same spawn guard as
    //the beacon rod - only the operating peer calls NewActor; clients get -1 and the
    //framework routes to the server. Sprite borrowed from the vanilla wood platform.
    internal class PlatformRod : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.WoodPlatform}";

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item8;
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                ActorLoader.NewActor<FloatPlatformActor>(Main.MouseWorld - new Vector2(60f, 8f));
            }

            UseCounterSave.CountLocalUse(player);
            ProgressPlayer.RecordDemoUse(player, "FloatPlatform");
            return true;
        }
    }

    internal class FloatPlatformEntry : IDemoEntry
    {
        public string Id => "FloatPlatform";
        public string Chapter => "Persistence";
        public string WikiSlug => "persistence/solid-actor";
        public string SourcePath => "Content/Persistence/FloatPlatform.cs";
        public int DemoItemType => ModContent.ItemType<PlatformRod>();
        public int SortIndex => 4;
    }
}
