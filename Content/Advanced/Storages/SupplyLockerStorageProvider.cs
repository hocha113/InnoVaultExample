using InnoVault;
using InnoVault.Storages;
using InnoVault.TileProcessors;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using static InnoVault.TileProcessors.TileProcessorLoader;

namespace InnoVaultExample.Content.Advanced.Storages
{
    //把补给柜接入 Storages 体系的两块拼图：provider 负责"怎么存取这一只柜子"，
    //factory 负责"怎么在世界里找到柜子们"。工厂实现 IStorageProviderFactory 后
    //会被 StorageLoader 在 SetupData 阶段自动扫描注册，不需要任何手动登记。
    //
    //The two pieces that plug the supply locker into the Storages system: the
    //provider answers "how do I access this one locker", the factory answers "how
    //do I find lockers in the world". Implementing IStorageProviderFactory is
    //enough — StorageLoader scans and registers it during SetupData, no manual
    //registration anywhere.
    //https://innovault.wiki/cn/advanced/storages/

    internal sealed class SupplyLockerStorageFactory : IStorageProviderFactory
    {
        public string Identifier => "InnoVaultExample.SupplyLocker";

        //数值越小优先级越高，原版箱子工厂是 0。排在它后面是刻意的：
        //柜子和箱子不会占同一格，这个数字只是表明"我们不和原版抢位置"
        //
        //Lower runs first; the vanilla chest factory sits at 0. Coming after it is
        //deliberate: lockers and chests never share a tile, the number just says
        //"we do not compete with vanilla".
        public int Priority => 10;

        public bool IsAvailable => true;

        public IEnumerable<IStorageProvider> FindStorageProviders(Point16 position, int range, Item item) {
            Vector2 worldPos = position.ToWorldCoordinates();
            float rangeSq = (float)range * range;

            //TP_InWorld 可能在遍历途中被增删，官方注释建议不要直接 foreach，
            //这里取一份快照再枚举；yield 的惰性执行让这一点更加重要
            //
            //TP_InWorld can gain or lose entries mid-iteration and its own docs warn
            //against a raw foreach, so enumerate a snapshot. The laziness of yield
            //makes that snapshot even more important.
            foreach (TileProcessor tp in TP_InWorld.ToList()) {
                if (tp is not SupplyLockerTP locker || !locker.Active) {
                    continue;
                }
                if (Vector2.DistanceSquared(worldPos, locker.CenterInWorld) > rangeSq) {
                    continue;
                }
                //调用方给了具体物品时，顺手过滤掉放不下的柜子，和原版箱子工厂的行为一致
                //
                //When the caller supplies a concrete item, filter out lockers that
                //cannot fit it, mirroring what the vanilla chest factory does
                if (item != null && item.Alives() && !locker.HasRoomFor(item)) {
                    continue;
                }
                yield return new SupplyLockerStorageProvider(locker);
            }
        }

        public IStorageProvider GetStorageProviders(Point16 position, Item item) {
            //精确坐标查找：泛型重载直接按类型筛，position 需要是 TP 的锚点（左上角）
            //
            //Exact-position lookup: the generic overload filters by type directly;
            //position must be the TP's anchor (its top-left tile)
            if (!ByPositionGetTP(position, out SupplyLockerTP locker)) {
                return null;
            }
            if (item != null && item.Alives() && !locker.HasRoomFor(item)) {
                return null;
            }
            return new SupplyLockerStorageProvider(locker);
        }
    }

    internal sealed class SupplyLockerStorageProvider : IStorageProvider
    {
        private readonly SupplyLockerTP locker;

        public SupplyLockerStorageProvider(SupplyLockerTP locker) {
            this.locker = locker;
        }

        public string Identifier => "InnoVaultExample.SupplyLocker";
        public Point16 Position => locker.Position;
        public Vector2 WorldCenter => locker.CenterInWorld;
        public Rectangle HitBox => locker.HitBox;

        //柜子被挖掉后 TP 会失活，靠 Active 就能让外界手里的旧 provider 引用自然失效。
        //调用方在每次变更前都应当先查 IsValid
        //
        //When the locker is mined the TP deactivates, so Active alone invalidates
        //any stale provider reference held outside. Callers should check IsValid
        //before every mutation.
        public bool IsValid => locker.Active;

        public bool HasSpace => IsValid && locker.HasAnyFreeSlot;

        public bool CanAcceptItem(Item item) => IsValid && locker.HasRoomFor(item);

        public bool DepositItem(Item item) {
            //Storages 框架不包多人权威：provider 可能被任何模组在任何端调用，
            //所以在这里自己把关——纯客户端直接拒绝，否则它本地改了槽位又没资格广播，
            //一定和服务器分道扬镳
            //
            //The Storages framework does not police multiplayer authority: any mod on
            //any peer may call this provider, so the gate lives here — a pure client
            //is refused outright, because it would mutate its local slots with no
            //right to broadcast and drift away from the server for good.
            if (VaultUtils.isClient || !CanAcceptItem(item)) {
                return false;
            }

            bool deposited = locker.DepositWhole(item);
            if (deposited) {
                //变更后立刻广播是 provider 自己的责任，不指望每个调用方都记得发包
                //
                //Broadcasting right after the mutation is the provider's own duty;
                //never rely on every caller remembering to send
                locker.SendData();
            }
            return deposited;
        }

        public Item WithdrawItem(int itemType, int count) {
            //与存入同理，取出也只在权威端执行
            //
            //Same as deposits, withdrawals run on the authority only
            if (VaultUtils.isClient || !IsValid || count <= 0) {
                return new Item();
            }

            int remaining = count;
            int withdrawn = 0;

            foreach (Item slot in locker.Slots) {
                if (slot.IsAir || slot.type != itemType) {
                    continue;
                }
                int take = System.Math.Min(remaining, slot.stack);
                slot.stack -= take;
                withdrawn += take;
                remaining -= take;
                if (slot.stack <= 0) {
                    slot.TurnToAir();
                }
                if (remaining <= 0) {
                    break;
                }
            }

            if (withdrawn <= 0) {
                return new Item();
            }

            locker.SendData();

            //接口约定按类型取出，跨堆叠合并后返回；这一步和原版箱子 provider 一样
            //会丢失前缀信息，需要保留前缀的场景应整份取出（见 TakeFirstStack）
            //
            //The contract withdraws by type, merging across stacks; like the vanilla
            //chest provider this loses prefix data. Take whole stacks instead when
            //prefixes matter (see TakeFirstStack).
            return new Item(itemType, withdrawn);
        }

        public IEnumerable<Item> GetStoredItems() {
            foreach (Item slot in locker.Slots) {
                if (!slot.IsAir) {
                    yield return slot;
                }
            }
        }

        public long GetItemCount(int itemType) {
            long total = 0;
            foreach (Item slot in locker.Slots) {
                if (!slot.IsAir && slot.type == itemType) {
                    total += slot.stack;
                }
            }
            return total;
        }

        public void PlayDepositAnimation() {
            //存入反馈是可选实现。存入运行在权威端，只有单人模式的权威端带着画面，
            //所以这几粒尘埃只在单人里看得到——联机的视觉反馈交给 SendData 后的计数变化
            //
            //Deposit feedback is optional. Deposits run on the authority, and only
            //the single-player authority has a screen, so these dust motes show up in
            //single player only — in multiplayer the counter moving after SendData is
            //the feedback.
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 6; i++) {
                Dust dust = Dust.NewDustDirect(locker.PosInWorld, (int)locker.Size.X, (int)locker.Size.Y, DustID.Smoke);
                dust.velocity = new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.5f));
                dust.noGravity = true;
            }
        }
    }
}
