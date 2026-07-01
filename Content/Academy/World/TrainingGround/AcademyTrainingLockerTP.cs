using InnoVault.TileProcessors;
using InnoVaultExample.Content.Academy.Persistence;
using InnoVaultExample.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InnoVaultExample.Content.Academy.World.TrainingGround
{
    // Showcase: Academy Supply Locker TP (WORLD-301)
    // Demonstrates: TP SaveData inventory, SendData/ReceiveData, Storages deposit hook
    // Wiki: https://innovault.wiki/cn/advanced/storages/
    // Source: InnoVaultExample/Content/Academy/World/TrainingGround/AcademyTrainingLockerTP.cs

    public class AcademyTrainingLockerTP : TileProcessor
    {
        public const int SlotCount = 20;

        public override int TargetTileID => ModContent.TileType<AcademyTrainingLockerTile>();

        internal readonly Item[] Slots = new Item[SlotCount];

        public AcademyTrainingLockerTP() {
            for (int i = 0; i < Slots.Length; i++) {
                Slots[i] = new Item();
            }
        }

        public override void SetProperty() {
            PlaceNet = true;
            for (int i = 0; i < Slots.Length; i++) {
                Slots[i] ??= new Item();
                Slots[i].TurnToAir();
            }
        }

        public override void SaveData(TagCompound tag) {
            tag["items"] = Slots.Select(ItemIO.Save).ToList();
        }

        public override void LoadData(TagCompound tag) {
            if (!tag.TryGet("items", out List<TagCompound> saved)) {
                return;
            }

            for (int i = 0; i < Slots.Length; i++) {
                Slots[i].TurnToAir();
            }

            for (int i = 0; i < saved.Count && i < Slots.Length; i++) {
                ItemIO.Load(Slots[i], saved[i]);
            }
        }

        public override void SendData(ModPacket data) {
            data.Write((byte)Slots.Length);
            for (int i = 0; i < Slots.Length; i++) {
                Item item = Slots[i];
                data.Write(!item.IsAir);
                if (!item.IsAir) {
                    data.Write(item.type);
                    data.Write((ushort)item.stack);
                }
            }
        }

        public override void ReceiveData(BinaryReader reader, int whoAmI) {
            int count = reader.ReadByte();
            for (int i = 0; i < count && i < Slots.Length; i++) {
                if (!reader.ReadBoolean()) {
                    Slots[i].TurnToAir();
                    continue;
                }

                int type = reader.ReadInt32();
                ushort stack = reader.ReadUInt16();
                Slots[i].SetDefaults(type);
                Slots[i].stack = stack;
            }
        }

        public override bool? RightClick(int i, int j, Tile tile, Player player) {
            if (!AcademyAuthority.HasAuthority()) {
                return true;
            }

            AcademyStudentPlayer.Get(player).Progress.MarkLockerVisited();

            Item held = player.selectedItem < player.inventory.Length ? player.inventory[player.selectedItem] : null;
            if (held != null && !held.IsAir) {
                if (TryDeposit(held)) {
                    SendData();
                }
                return true;
            }

            Item withdrawn = TryWithdrawFirst();
            if (withdrawn != null && !withdrawn.IsAir) {
                player.QuickSpawnItem(player.GetSource_TileInteraction(i, j), withdrawn);
                SendData();
            }

            return true;
        }

        internal bool TryDeposit(Item item) {
            if (item == null || item.IsAir) {
                return false;
            }

            for (int i = 0; i < Slots.Length; i++) {
                Item slot = Slots[i];
                if (slot.IsAir) {
                    slot.SetDefaults(item.type);
                    slot.stack = item.stack;
                    item.TurnToAir();
                    return true;
                }

                if (slot.type == item.type && slot.stack < slot.maxStack) {
                    int move = System.Math.Min(item.stack, slot.maxStack - slot.stack);
                    slot.stack += move;
                    item.stack -= move;
                    if (item.stack <= 0) {
                        item.TurnToAir();
                    }
                    return true;
                }
            }

            return false;
        }

        internal Item TryWithdrawFirst() {
            for (int i = 0; i < Slots.Length; i++) {
                Item slot = Slots[i];
                if (slot.IsAir) {
                    continue;
                }

                Item clone = slot.Clone();
                slot.TurnToAir();
                return clone;
            }

            return new Item();
        }

        internal bool HasSpaceFor(Item item) {
            if (item == null || item.IsAir) {
                return false;
            }

            for (int i = 0; i < Slots.Length; i++) {
                Item slot = Slots[i];
                if (slot.IsAir || (slot.type == item.type && slot.stack < slot.maxStack)) {
                    return true;
                }
            }

            return false;
        }

        internal bool HasSpace => Slots.Any(slot => slot.IsAir);

        public override void Draw(SpriteBatch spriteBatch) {
            if (Main.dedServ) {
                return;
            }

            int stored = Slots.Count(slot => slot != null && !slot.IsAir);
            Vector2 drawPos = CenterInWorld - Main.screenPosition + new Vector2(0f, -18f);
            Utils.DrawBorderString(spriteBatch, $"{stored}/{SlotCount}", drawPos, Color.LightSkyBlue, 0.85f);
        }
    }
}
