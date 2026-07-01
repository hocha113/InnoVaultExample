using InnoVault;
using InnoVault.Storages;
using InnoVault.TileProcessors;
using InnoVaultExample.Content.Academy.World.TrainingGround;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using static InnoVault.TileProcessors.TileProcessorLoader;

namespace InnoVaultExample.Content.Academy.Storages
{
    // Showcase: Academy Locker Storage Provider (WORLD-301)
    // Demonstrates: IStorageProviderFactory auto-registration, TP-backed stash lookup
    // Wiki: https://innovault.wiki/cn/advanced/storages/
    // Source: InnoVaultExample/Content/Academy/Storages/AcademyLockerStorageProvider.cs

    public sealed class AcademyLockerStorageFactory : IStorageProviderFactory
    {
        public string Identifier => "InnoVaultExample.AcademyLocker";
        public int Priority => 10;
        public bool IsAvailable => true;

        public IEnumerable<IStorageProvider> FindStorageProviders(Point16 position, int range, Item item) {
            Vector2 worldPos = position.ToWorldCoordinates();
            float rangeSq = range * range;

            foreach (TileProcessor tp in TP_InWorld) {
                if (tp is not AcademyTrainingLockerTP locker || !locker.Active) {
                    continue;
                }

                if (Vector2.DistanceSquared(worldPos, locker.CenterInWorld) > rangeSq) {
                    continue;
                }

                if (item != null && item.Alives() && !locker.HasSpaceFor(item)) {
                    continue;
                }

                yield return new AcademyLockerStorageProvider(locker);
            }
        }

        public IStorageProvider GetStorageProviders(Point16 position, Item item) {
            if (!ByPositionGetTP(position, out TileProcessor tp) || tp is not AcademyTrainingLockerTP locker) {
                return null;
            }

            if (item != null && item.Alives() && !locker.HasSpaceFor(item)) {
                return null;
            }

            return new AcademyLockerStorageProvider(locker);
        }
    }

    public sealed class AcademyLockerStorageProvider : IStorageProvider
    {
        private readonly AcademyTrainingLockerTP _locker;

        public AcademyLockerStorageProvider(AcademyTrainingLockerTP locker) {
            _locker = locker;
        }

        public string Identifier => "InnoVaultExample.AcademyLocker";
        public Point16 Position => _locker.Position;
        public Vector2 WorldCenter => _locker.CenterInWorld;
        public Rectangle HitBox => _locker.HitBox;
        public bool IsValid => _locker.Active;
        public bool HasSpace => _locker.HasSpace;

        public bool CanAcceptItem(Item item) => IsValid && _locker.HasSpaceFor(item);

        public bool DepositItem(Item item) {
            if (!CanAcceptItem(item)) {
                return false;
            }

            bool deposited = _locker.TryDeposit(item);
            if (deposited && AcademyAuthority.HasAuthority()) {
                _locker.SendData();
            }

            return deposited;
        }

        public Item WithdrawItem(int itemType, int count) {
            if (!IsValid || count <= 0) {
                return new Item();
            }

            int remaining = count;
            Item result = new Item();

            for (int i = 0; i < _locker.Slots.Length; i++) {
                Item slot = _locker.Slots[i];
                if (slot.IsAir || slot.type != itemType) {
                    continue;
                }

                int take = System.Math.Min(remaining, slot.stack);
                if (result.IsAir) {
                    result.SetDefaults(itemType);
                    result.stack = take;
                }
                else {
                    result.stack += take;
                }

                slot.stack -= take;
                remaining -= take;
                if (slot.stack <= 0) {
                    slot.TurnToAir();
                }

                if (remaining <= 0) {
                    break;
                }
            }

            if (result.stack > 0 && AcademyAuthority.HasAuthority()) {
                _locker.SendData();
            }

            return result.stack > 0 ? result : new Item();
        }

        public IEnumerable<Item> GetStoredItems() {
            foreach (Item item in _locker.Slots) {
                if (item != null && !item.IsAir) {
                    yield return item;
                }
            }
        }

        public long GetItemCount(int itemType) {
            long total = 0;
            foreach (Item item in _locker.Slots) {
                if (item != null && !item.IsAir && item.type == itemType) {
                    total += item.stack;
                }
            }

            return total;
        }
    }
}
