using InnoVault;
using InnoVault.TileProcessors;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace InnoVaultExample.Content.TileEntities
{
    //展示台：手持物品对它右键就能把东西摆上去展示，再右键取回。
    //放置物品、物块、TP 实体三件套都在这一个文件里，从上往下读完，
    //就是 TileProcessor 从挂载、更新、绘制到存档和联机同步的完整生命周期。
    //
    //The display stand: right click with an item in hand to put it on show,
    //right click again to take it back. The placement item, the tile and the TP
    //all live in this file; read it top to bottom for the full TileProcessor
    //lifecycle, from mounting and drawing to saving and multiplayer sync.
    //https://innovault.wiki/cn/content/tile-processor/

    internal class DisplayStandItem : ModItem
    {
        //这条演示全部借用原版贴图，物品直接用工作台（ItemID 36）的
        //
        //This demo borrows vanilla art throughout; the item reuses the workbench sprite (ItemID 36)
        public override string Texture => "Terraria/Images/Item_36";

        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<DisplayStandTile>());
            Item.value = Item.buyPrice(silver: 5);
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.Wood, 8).Register();
        }
    }

    internal class DisplayStandTile : ModTile
    {
        //物块贴图借原版工作台的图集，配合下面 Style2x1 的数据，摆出来就是一张台面
        //
        //The tile sheet is the vanilla workbench one; with the Style2x1 data below
        //it places as a little tabletop
        public override string Texture => "Terraria/Images/Tiles_18";

        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileTable[Type] = true;
            Main.tileSolidTop[Type] = true;
            Main.tileNoAttach[Type] = true;

            //这里刻意不开 TileID.Sets.HasOutlines：模组物块一旦开启，tML 会在加载期
            //强制请求 Texture + "_Highlight" 这张描边贴图，而原版没有 Tiles_18_Highlight，
            //缺图会直接在加载时报错。右键交互和悬停图标都不依赖描边；
            //完整的"描边 + 智能光标"配方见 SupplyLocker
            //
            //Deliberately no TileID.Sets.HasOutlines here: once a modded tile turns it
            //on, tML demands a highlight sheet at Texture + "_Highlight" during loading,
            //and vanilla ships no Tiles_18_Highlight, so the missing asset errors out at
            //load. Neither right clicking nor the hover icon needs an outline; see
            //SupplyLocker for the full outline + smart cursor recipe.

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
            TileObjectData.newTile.CoordinateHeights = [18];
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(168, 132, 92), CreateMapEntryName());
            DustType = DustID.WoodFurniture;
        }
    }

    internal class DisplayStandTP : TileProcessor
    {
        //TargetTileID 指向上面的物块后，世界里每一张展示台都会自动带上一个本实体。
        //千万别让它返回 0：0 是泥土的图格 ID，也是 ModContent.TileType 失败时的返回值
        //
        //Point TargetTileID at the tile above and every stand in the world gets one
        //of these automatically. Never return 0: that is dirt's tile ID, and also
        //what ModContent.TileType returns on failure.
        public override int TargetTileID => ModContent.TileType<DisplayStandTile>();

        //台上正在展示的东西。存完整的 Item 而不是 type+stack 两个数字，
        //是为了让 ItemIO 把改装前缀、模组附加数据也一起带上
        //
        //What is currently on display. A full Item rather than a bare type+stack
        //pair, so ItemIO can carry prefixes and mod data along too.
        private Item displayed = new();

        private float bobSeed;

        //台面正中上方，展示品悬浮在这里
        //
        //Just above the middle of the tabletop, where the display item hovers
        private Vector2 DisplayPos => CenterInWorld - new Vector2(0f, 20f);

        public override void Initialize() {
            //Initialize 在实体加入世界时各端各跑一次，此时 Position 等基础数据已就绪。
            //悬浮动画的相位差从坐标推导而不是取随机数：随机数在各端各不相同，
            //而坐标推导让同一张台子在所有人屏幕上摆动得一致
            //
            //Initialize runs once per peer when the entity enters the world, with
            //Position and friends ready. The bob phase comes from the coordinate
            //instead of a random roll: random numbers differ per peer, coordinates
            //keep the same stand swaying identically on every screen.
            bobSeed = Position.X * 0.7f;
        }

        public override void Update() {
            //Update 在服务器和所有客户端上每帧运行。这里只做纯视觉的事，
            //所以整体拦在"有画面的端"里；真正改状态的逻辑都在 RightClick
            //
            //Update ticks on the server and every client. Only cosmetic work happens
            //here, so gate it all behind "has a screen"; the state changes live in
            //RightClick.
            if (Main.dedServ || !displayed.Alives()) {
                return;
            }

            Lighting.AddLight(DisplayPos, new Vector3(0.24f, 0.2f, 0.1f));

            if (Main.rand.NextBool(40)) {
                Dust sparkle = Dust.NewDustPerfect(DisplayPos + Main.rand.NextVector2Circular(10f, 8f), DustID.TreasureSparkle);
                sparkle.velocity = Vector2.Zero;
                sparkle.noGravity = true;
            }
        }

        public override bool? RightClick(int i, int j, Tile tile, Player player) {
            //框架会把物块右键转发给服务器和其他客户端，所以这个钩子在每个端都会各跑一遍。
            //先划清所有权：台面状态归权威端（服务器/单人）改，玩家手里的物品归它的
            //所有者客户端改，各改各的，然后靠网络把大家对齐
            //
            //The framework relays tile right clicks to the server and the other
            //clients, so this hook runs once on every peer. Split the ownership
            //first: the stand's state belongs to the authority (server / single
            //player), the held item belongs to its owner client; each side mutates
            //only its part and the network reconciles everyone.
            bool authority = VaultUtils.isServer || VaultUtils.isSinglePlayer;
            bool ownerClient = player.whoAmI == Main.myPlayer;

            //读物品栏选中格，而不是 Main.mouseItem：鼠标上拎着的物品只存在于本地客户端，
            //服务器没有这份数据；选中格则由原版持续同步，每个端读到的都是同一格。
            //player.HeldItem 就是这个下标的别名，这里写全只为点明数据来源
            //
            //Read the selected inventory slot, not Main.mouseItem: an item dangling on
            //the cursor exists only on the local client, the server has no such thing.
            //The selected slot is kept in sync by vanilla, so every peer reads the same
            //cell. player.HeldItem aliases exactly this indexing; it is spelled out here
            //to make the data source obvious.
            Item held = player.inventory[player.selectedItem];

            if (displayed.Alives()) {
                //台上有东西，这一下是"取回"。物品只能由权威端生成一次：
                //服务器生成的掉落物自带网络同步，客户端如果也各生成一份就会凭空复制
                //
                //Something is on display, so this click means "take it back". Only
                //the authority may spawn the item, exactly once: server-spawned drops
                //sync on their own, and a client spawning its own copy would dupe.
                if (authority) {
                    player.QuickSpawnItem(player.GetSource_TileInteraction(i, j), displayed, displayed.stack);
                    displayed.TurnToAir();

                    //何时该发包：每处改动了 displayed 的地方都要跟一次 SendData()，
                    //它会路由到下面的 SendData(ModPacket)，把新状态推给所有客户端
                    //
                    //When to send: every spot that mutates "displayed" follows up with
                    //SendData(), which routes into SendData(ModPacket) below and pushes
                    //the new state to every client.
                    SendData();
                }
                return true;
            }

            if (!held.Alives()) {
                //空手点空台子，没什么可做的
                //
                //Empty hand on an empty stand, nothing to do
                return true;
            }

            //音效只该在有画面的端响，专用服务器跳过
            //
            //Sound only makes sense on peers with a screen, skip the dedicated server
            if (!Main.dedServ) {
                SoundEngine.PlaySound(SoundID.Grab, CenterInWorld);
            }

            if (authority) {
                //权威端只"记账"：克隆一份手持物品记为展示品并广播。真正从手里拿走那份
                //发生在物品所有者的客户端上；单人模式下两个身份都在本端，两个分支都会走
                //
                //The authority only does the bookkeeping: clone the held item as the
                //display piece and broadcast. Actually removing it from the hand
                //happens on the owner client; in single player both roles live on this
                //peer and both branches run.
                displayed = held.Clone();
                SendData();
            }
            if (ownerClient) {
                //清掉手上的那份。原版本来就会把手持格的变化同步给服务器，这里不用自己发包。
                //两端各自根据同步后的状态走同一套分支，理论上存在极小的错位窗口，示例不做补偿
                //
                //Clear the held copy. Vanilla already syncs held-slot changes up to the
                //server, no manual packet needed. Both peers branch on synced state, so
                //the mismatch window is tiny; this example does not compensate for it.
                held.TurnToAir();
            }
            return true;
        }

        public override void HoverTile() {
            //只在悬停的那个客户端上运行，适合做光标图标这类纯本地的界面反馈
            //
            //Runs only on the hovering client, the right place for local-only UI
            //feedback like the cursor icon
            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconID = displayed.Alives() ? displayed.type : ModContent.ItemType<DisplayStandItem>();
        }

        public override void SaveData(TagCompound tag) {
            //SaveData/LoadData 只在服务器（含单人）上调用：世界存档是服务端的资产。
            //客户端进世界时拿到的不是这份数据，而是下面 SendData 发来的网络包
            //
            //SaveData/LoadData run only on the server (and in single player): the
            //world save belongs to the server. A joining client never reads this; it
            //gets the state from the SendData packet below instead.
            tag["displayed"] = ItemIO.Save(displayed);
        }

        public override void LoadData(TagCompound tag) {
            if (tag.TryGet("displayed", out TagCompound itemTag)) {
                displayed = ItemIO.Load(itemTag);
            }
        }

        public override void SendData(ModPacket data) {
            //发包时机有两类：进世界时框架自动替新客户端要一次（LoadenWorldSendData 默认开），
            //运行中则由我们在每次改动后手动调用无参 SendData()。
            //写与读必须逐字段一一对应，顺序错一位整个包就废了
            //
            //Packets go out in two situations: the framework requests one for each
            //joining client automatically (LoadenWorldSendData defaults to on), and we
            //call the parameterless SendData() after every mutation. Writes and reads
            //must match field for field; one slot out of order corrupts the packet.
            ItemIO.Send(displayed, data, writeStack: true);
        }

        public override void ReceiveData(BinaryReader reader, int whoAmI) {
            displayed = ItemIO.Receive(reader, readStack: true);
        }

        public override void OnKill() {
            //台子被挖掉时把展示品还给世界。死亡由服务器判定后广播，OnKill 各端都会跑，
            //掉落物依旧只让权威端生成那一次
            //
            //When the stand is mined, hand the display item back to the world. Death
            //is decided by the server and broadcast, so OnKill runs everywhere; the
            //drop is still spawned by the authority alone.
            if (!displayed.Alives() || (!VaultUtils.isServer && !VaultUtils.isSinglePlayer)) {
                return;
            }
            Item.NewItem(new EntitySource_TileBreak(Position.X, Position.Y), HitBox, displayed);
            displayed.TurnToAir();
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //Draw 这一族钩子只会在客户端被调用，不需要再防御 dedServ。
            //客户端手里的 displayed 来自 ReceiveData——你在这里画出来的，就是同步的成果
            //
            //The draw hooks only ever run on clients, no dedServ guard needed. The
            //"displayed" a client holds came in through ReceiveData; what you draw
            //here is the sync made visible.
            if (!displayed.Alives()) {
                return;
            }

            //物品贴图按需加载，画之前必须先请求一次
            //
            //Item textures load on demand; request before drawing
            Main.instance.LoadItem(displayed.type);
            Texture2D texture = TextureAssets.Item[displayed.type].Value;

            //有些物品（灵魂、火把等）的贴图是多帧动画，直接整张画会把所有帧叠在一起
            //
            //Some items (souls, torches...) have animated sheets; drawing the whole
            //texture would stack every frame on top of each other
            Rectangle frame = Main.itemAnimations[displayed.type]?.GetFrame(texture) ?? texture.Frame();

            //大件武器缩到台面摆得下的尺寸，小东西保持原大
            //
            //Shrink oversized weapons to fit the tabletop, keep small things at scale
            float scale = Math.Min(1f, 26f / Math.Max(frame.Width, frame.Height));
            float bob = MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f + bobSeed) * 3f;
            Vector2 drawPos = DisplayPos - Main.screenPosition + new Vector2(0f, bob);
            Color lightColor = Lighting.GetColor(Position.X, Position.Y - 1);

            spriteBatch.Draw(texture, drawPos, frame, lightColor, 0f, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            if (displayed.stack > 1) {
                Utils.DrawBorderString(spriteBatch, displayed.stack.ToString(), drawPos + new Vector2(6f, 2f), Color.White, 0.8f);
            }
        }
    }

    //登记进手册目录
    //
    //Registers into the guide catalog
    internal sealed class DisplayStandEntry : IDemoEntry
    {
        public string Id => "DisplayStand";
        public string Chapter => "TileEntities";
        public string WikiSlug => "content/tile-processor";
        public string SourcePath => "Content/TileEntities/DisplayStand.cs";
        public int DemoItemType => ModContent.ItemType<DisplayStandItem>();
        public int SortIndex => 0;
    }
}
