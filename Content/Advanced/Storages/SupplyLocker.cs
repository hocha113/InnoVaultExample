using InnoVault;
using InnoVault.Storages;
using InnoVaultExample.Common;
using InnoVault.TileProcessors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace InnoVaultExample.Content.Advanced.Storages
{
    //补给柜：手持物品右键整组存入，空手右键取出一份。柜子本体是一个带槽位的
    //TP 实体，真正的看点在旁边的 SupplyLockerStorageProvider.cs——它把柜子接进
    //InnoVault 的 Storages 统一存取体系，右键存入走的就是 StorageLoader 门面。
    //
    //The supply locker: right click with an item to deposit the whole stack, right
    //click empty-handed to take one stack out. The locker itself is a TP entity
    //with slots; the interesting part lives next door in
    //SupplyLockerStorageProvider.cs, which plugs it into InnoVault's unified
    //Storages system — the deposit below goes through the StorageLoader facade.
    //https://innovault.wiki/cn/advanced/storages/

    internal class SupplyLockerItem : ModItem
    {
        //借原版箱子的物品贴图（ItemID 48），和下面的柜体外观配套
        //
        //Borrow the vanilla chest sprite (ItemID 48) to match the tile below
        public override string Texture => "Terraria/Images/Item_48";

        public override void SetDefaults() {
            Item.DefaultToPlaceableTile(ModContent.TileType<SupplyLockerTile>());
            Item.value = Item.buyPrice(silver: 10);
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).AddIngredient(ItemID.IronBar, 2).Register();
        }
    }

    internal class SupplyLockerTile : ModTile
    {
        //物块贴图借原版箱子图集，2x2 摆出来就是一只木箱。注意我们不设置
        //Main.tileContainer：柜子的物品全由 TP 管理，不让原版的箱子逻辑介入
        //
        //The tile sheet is the vanilla chest one; a 2x2 wooden chest when placed.
        //Deliberately no Main.tileContainer here: the TP owns the inventory and the
        //vanilla chest machinery must stay out of it.
        public override string Texture => "Terraria/Images/Tiles_21";

        //开了 HasOutlines 的模组物块必须真的有一张描边贴图，默认路径是
        //Texture + "_Highlight"，那张图不存在，加载时就会报缺资源。
        //既然图集借的是箱子，描边也指向原版给箱子准备的那张
        //
        //A modded tile with HasOutlines on must actually own a highlight sheet;
        //the default path is Texture + "_Highlight", which does not exist and would
        //raise a missing-asset error at load. Since the tile sheet is the chest's,
        //point the outline at the one vanilla drew for chests.
        public override string HighlightTexture => "Terraria/Images/Misc/TileOutlines/Tiles_21";

        //让智能光标能像对待箱子一样锁定柜子。原版只对进了智能交互候选表的物块
        //绘制描边，模组物块得自己举手；这个开关同时喂给 TP 的 HoverTP 智能光标分支
        //
        //Let smart cursor lock onto the locker the way it does chests. Vanilla only
        //draws outlines for tiles in the smart-interact candidate list, and a modded
        //tile has to raise its own hand; this switch also feeds the smart cursor
        //branch of the TP's HoverTP.
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;

            //箱子图集每格行高是 16/18 而不是 Style2x2 默认的 16/16，
            //照抄原版箱子的行高与放置原点，贴图才不会被裁掉底边
            //
            //The chest sheet uses row heights 16/18, not Style2x2's default 16/16;
            //copy the vanilla chest's heights and placement origin so the sprite's
            //bottom edge does not get clipped.
            TileObjectData.newTile.CoordinateHeights = [16, 18];
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(120, 140, 180), CreateMapEntryName());
            DustType = DustID.Iron;
        }
    }

    internal class SupplyLockerTP : TileProcessor
    {
        internal const int SlotCount = 8;

        public override int TargetTileID => ModContent.TileType<SupplyLockerTile>();

        //槽位在构造器里填充：世界里的实例由注册模板 Clone() 而来，默认克隆走
        //Activator.CreateInstance，构造器会重新执行，每个柜子拿到的都是自己的数组
        //
        //Slots are filled in the constructor: world instances come from Clone() on
        //the registered template, and the default clone goes through
        //Activator.CreateInstance, so the constructor reruns and every locker gets
        //its own array.
        internal readonly Item[] Slots = new Item[SlotCount];

        public SupplyLockerTP() {
            for (int i = 0; i < Slots.Length; i++) {
                Slots[i] = new Item();
            }
        }

        public override void SetProperty() {
            //放置广播开关：开启后，玩家放置柜子时会立刻向其他端 SendData 一次。
            //新柜子本来就是空的，这里打开主要为了让放置路径也走一遍完整同步
            //
            //The placement broadcast switch: with it on, placing the locker fires an
            //immediate SendData to the other peers. A fresh locker is empty anyway;
            //it is enabled here so the placement path also exercises a full sync.
            PlaceNet = true;
        }

        public override bool? RightClick(int i, int j, Tile tile, Player player) {
            //右键会被框架转发到服务器和其他客户端，每个端都跑一遍这个钩子。
            //所有权划分和展示台一样：柜子内容归权威端，手上的物品归所有者客户端
            //
            //The framework relays the right click to the server and the other
            //clients, so every peer runs this hook. Ownership splits like the display
            //stand: locker contents belong to the authority, the held item to its
            //owner client.
            bool authority = VaultUtils.isServer || VaultUtils.isSinglePlayer;
            bool ownerClient = player.whoAmI == Main.myPlayer;
            Item held = player.inventory[player.selectedItem];

            if (held.Alives()) {
                //存入设计成"整组放下或干脆不存"：客户端手里的槽位表是服务器同步来的副本，
                //两端按同一条规则判断就能得出一致结论。若允许存半组，客户端将无法可靠地
                //知道该从手里扣掉多少
                //
                //Deposits are all-or-nothing on purpose: the client's slot table is a
                //synced copy of the server's, so both peers reach the same verdict
                //from the same rule. Allowing partial deposits would leave the client
                //unable to tell how much to remove from the hand.
                if (!HasRoomFor(held)) {
                    if (ownerClient) {
                        Main.NewText(Language.GetTextValue("Mods.InnoVaultExample.Misc.LockerFull"), Color.OrangeRed);
                    }
                    return true;
                }

                //音效留给有画面的端，专用服务器不播
                //
                //Sounds are for peers with a screen; the dedicated server plays none
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Grab, CenterInWorld);
                }

                if (authority) {
                    //特意绕道 Storages 门面而不是直接改槽位：TryGetStorageTargetByPoint
                    //会按工厂优先级找到我们注册的 provider，TryDepositItem 存入并触发
                    //存入动画。这正是柜子接入统一存取体系后，任何模组都能走的路径
                    //
                    //Deliberately routed through the Storages facade instead of poking
                    //the slots: TryGetStorageTargetByPoint resolves our registered
                    //provider by factory priority, and TryDepositItem stores the item
                    //and plays the deposit animation. This is the exact path any other
                    //mod can take once the locker joins the unified storage system.
                    if (StorageLoader.TryGetStorageTargetByPoint(Position, held, out IStorageProvider provider)) {
                        //DepositItem 会消耗传入的物品实例（这里是权威端的手持副本），
                        //provider 内部在变更后自己负责 SendData
                        //
                        //DepositItem consumes the item instance it is given (the
                        //authority's copy of the held item), and the provider takes care
                        //of SendData after the mutation.
                        StorageLoader.TryDepositItem(provider, held);
                    }
                }
                else if (ownerClient) {
                    //纯客户端只清掉手上的那份，原版会把手持格的变化同步给服务器。
                    //单人时权威分支已经消耗了手上的物品，所以这里是 else if
                    //
                    //A pure client only clears its own hand; vanilla syncs held-slot
                    //changes to the server. In single player the authority branch has
                    //already consumed the item, hence the else-if.
                    held.TurnToAir();
                }
                return true;
            }

            //空手：取出第一份非空堆叠。物品由权威端生成，服务器生成的掉落物自带网络同步
            //
            //Empty hand: take the first non-empty stack out. The authority spawns the
            //item; server-spawned drops sync on their own.
            if (authority) {
                Item taken = TakeFirstStack();
                if (taken.Alives()) {
                    player.QuickSpawnItem(player.GetSource_TileInteraction(i, j), taken, taken.stack);
                    SendData();
                }
            }
            return true;
        }

        public override void HoverTile() {
            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<SupplyLockerItem>();
        }

        //"整组放得下吗"：有空槽，或者存在同类型同前缀且并入后不超上限的堆叠。
        //这是存入判定的唯一规则，provider 和右键两条路径共用
        //
        //"Does the whole stack fit": a free slot, or an existing stack of the same
        //type and prefix that stays under the cap after merging. This is the single
        //deposit rule shared by the provider and the right-click path.
        internal bool HasRoomFor(Item item) {
            if (!item.Alives()) {
                return false;
            }
            foreach (Item slot in Slots) {
                if (slot.IsAir) {
                    return true;
                }
                if (slot.type == item.type && slot.prefix == item.prefix && slot.stack + item.stack <= slot.maxStack) {
                    return true;
                }
            }
            return false;
        }

        internal bool HasAnyFreeSlot => Slots.Any(slot => slot.IsAir);

        //整组存入并消耗传入实例。克隆进槽位而不是记 type+stack，前缀和模组数据才不会丢
        //
        //Deposit the whole stack and consume the given instance. Cloning into the
        //slot instead of noting type+stack keeps prefixes and mod data intact.
        internal bool DepositWhole(Item item) {
            if (!HasRoomFor(item)) {
                return false;
            }

            for (int i = 0; i < Slots.Length; i++) {
                Item slot = Slots[i];
                if (!slot.IsAir && slot.type == item.type && slot.prefix == item.prefix && slot.stack + item.stack <= slot.maxStack) {
                    slot.stack += item.stack;
                    item.TurnToAir();
                    return true;
                }
            }

            for (int i = 0; i < Slots.Length; i++) {
                if (Slots[i].IsAir) {
                    Slots[i] = item.Clone();
                    item.TurnToAir();
                    return true;
                }
            }
            return false;
        }

        internal Item TakeFirstStack() {
            foreach (Item slot in Slots) {
                if (slot.IsAir) {
                    continue;
                }
                Item taken = slot.Clone();
                slot.TurnToAir();
                return taken;
            }
            return new Item();
        }

        public override void SaveData(TagCompound tag) {
            //世界存档只发生在服务端（含单人）。客户端进世界时靠 SendData 拿状态，
            //这份 NBT 数据它永远读不到
            //
            //World saves happen on the server (and in single player) only. A joining
            //client never reads this NBT; it gets the state through SendData instead.
            tag["slots"] = Slots.Select(ItemIO.Save).ToList();
        }

        public override void LoadData(TagCompound tag) {
            if (!tag.TryGet("slots", out System.Collections.Generic.List<TagCompound> saved)) {
                return;
            }
            for (int i = 0; i < Slots.Length; i++) {
                Slots[i].TurnToAir();
                if (i < saved.Count) {
                    ItemIO.Load(Slots[i], saved[i]);
                }
            }
        }

        public override void SendData(ModPacket data) {
            //ItemIO.Send/Receive 按槽位一一配对，连模组物品和前缀都能正确过网。
            //读写顺序必须严格一致：这里先写槽位数再逐槽写，对面就先读数再逐槽读
            //
            //ItemIO.Send/Receive pair up slot by slot and carry modded items and
            //prefixes across the wire correctly. Write and read order must match
            //exactly: slot count first, then each slot, mirrored on the other side.
            data.Write((byte)Slots.Length);
            foreach (Item slot in Slots) {
                ItemIO.Send(slot, data, writeStack: true);
            }
        }

        public override void ReceiveData(BinaryReader reader, int whoAmI) {
            int count = reader.ReadByte();
            for (int i = 0; i < count; i++) {
                Item incoming = ItemIO.Receive(reader, readStack: true);
                if (i < Slots.Length) {
                    Slots[i] = incoming;
                }
            }
        }

        public override void OnKill() {
            //柜子被挖掉时把库存倒出来。OnKill 在死亡广播后各端都会跑，
            //掉落物仍然只由权威端生成，客户端只负责把自己的槽位表清干净
            //
            //Dump the inventory when the locker is mined. OnKill runs on every peer
            //after the death broadcast; drops are still authority-only, clients just
            //tidy their own slot tables.
            bool authority = VaultUtils.isServer || VaultUtils.isSinglePlayer;
            foreach (Item slot in Slots) {
                if (!slot.Alives()) {
                    continue;
                }
                if (authority) {
                    Item.NewItem(new EntitySource_TileBreak(Position.X, Position.Y), HitBox, slot);
                }
                slot.TurnToAir();
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //柜顶的占用计数。客户端画的是 ReceiveData 收到的槽位表——
            //联机时看到数字变化，就说明同步链路是通的
            //
            //The occupancy counter above the locker. Clients draw the slot table that
            //arrived through ReceiveData — in multiplayer, seeing the number move is
            //proof the sync loop works.
            int used = Slots.Count(slot => !slot.IsAir);
            Vector2 drawPos = new(CenterInWorld.X - Main.screenPosition.X - 10f, PosInWorld.Y - Main.screenPosition.Y - 18f);
            Utils.DrawBorderString(spriteBatch, $"{used}/{SlotCount}", drawPos, Color.LightSkyBlue * 0.9f, 0.8f);
        }
    }

    internal sealed class SupplyLockerEntry : IDemoEntry
    {
        public string Id => "SupplyLocker";
        public string Chapter => "Advanced";
        public string WikiSlug => "advanced/storages";
        public string SourcePath => "Content/Advanced/Storages/SupplyLocker.cs";
        public int DemoItemType => ModContent.ItemType<SupplyLockerItem>();
        public int SortIndex => 0;
    }
}
