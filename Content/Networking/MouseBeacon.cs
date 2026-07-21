using InnoVault;
using InnoVault.VaultNetworks;
using InnoVaultExample.Common;
using InnoVaultExample.Content.Persistence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InnoVaultExample.Content.Networking
{
    //鼠标信标：握在手里时，把附近其他玩家的鼠标位置画成光斑标记。原版网络不会
    //广播别人的鼠标，PlayerNetwork 用"按需订阅 + 短期缓存"补上这块：谁感兴趣谁订阅，
    //没人订阅就一个包都不发。单人时画的是自己的鼠标，流程照样跑通。
    //
    //The mouse beacon: while held, it marks nearby players' mouse positions with glow
    //dots. Vanilla netcode never broadcasts other players' cursors; PlayerNetwork
    //fills the gap with interest-based subscriptions plus a short-lived cache - only
    //interested peers subscribe, and with no subscribers not a single packet is sent.
    //Solo it draws your own cursor, so the whole flow still works.

    internal class MouseBeacon : ModItem
    {
        //订阅半径：只对这个范围内的玩家续订兴趣，屏幕外八百里的玩家没必要跟踪
        //
        //Interest radius: renew interest only for players within range - no point
        //tracking someone half a map away
        internal const float InterestRange = 2400f;

        public override string Texture => $"Terraria/Images/Item_{ItemID.Binoculars}";

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.rare = ItemRarityID.Blue;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
        }

        //HoldItem 对每个端上的每个持握玩家都会跑，但订阅只该由"我这台机器"发起。
        //KeepAlive 向服务器续订"接下来约半秒我要这个玩家的鼠标世界坐标"：
        //持续持握就持续续订；收起物品后不再续订，服务器侧的兴趣自然过期，
        //数据流随兴趣走而不是常开——这就是按需订阅的设计意图。
        //每帧调用不用担心刷爆网络：框架对同一目标的请求包有几帧的冷却，
        //快照本身也只在变化明显或到达心跳时才回传。
        //
        //HoldItem runs for every holding player on every peer, but subscribing is the
        //local machine's job alone. KeepAlive renews "I want this player's mouse world
        //position for roughly the next half second": keep holding and it keeps
        //renewing; stow the item and the server-side interest simply expires. Data
        //flows only while someone cares - that is the point of interest-based
        //subscription. Calling it every tick is fine: the framework cools down request
        //packets per target for a few ticks, and snapshots only travel back when they
        //changed noticeably or a heartbeat is due.
        public override void HoldItem(Player player) {
            if (player.whoAmI != Main.myPlayer || !VaultUtils.isClient) {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++) {
                Player other = Main.player[i];
                if (!other.active || other.whoAmI == Main.myPlayer) {
                    continue;
                }
                if (other.DistanceSQ(player.Center) > InterestRange * InterestRange) {
                    continue;
                }
                PlayerNetwork.KeepAlive(other, PlayerNetworkDataFlags.MouseWorld);
            }
        }

        //使用一下可以在聊天栏报告当前缓存里有几份新鲜的远程鼠标快照，方便联机时确认订阅生效
        //
        //Using the item reports how many fresh remote mouse snapshots the cache holds
        //right now - handy for confirming the subscription works in multiplayer
        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer && !Main.dedServ) {
                int freshCount = 0;
                for (int i = 0; i < Main.maxPlayers; i++) {
                    Player other = Main.player[i];
                    if (!other.active || other.whoAmI == Main.myPlayer) {
                        continue;
                    }
                    if (PlayerNetwork.TryGetSnapshot(other, out PlayerNetworkSnapshot snapshot)
                        && snapshot.Has(PlayerNetworkDataFlags.MouseWorld)) {
                        freshCount++;
                    }
                }
                Main.NewText(Language.GetTextValue("Mods.InnoVaultExample.Messages.MouseBeaconStatus", freshCount), Color.Cyan);
            }

            UseCounterSave.CountLocalUse(player);
            ProgressPlayer.RecordDemoUse(player, "MouseBeacon");
            return true;
        }
    }

    //绘制端：本地玩家握着信标时，把每个能拿到鼠标快照的玩家标记出来。
    //
    //The draw side: while the local player holds the beacon, mark every player whose
    //mouse snapshot is available.
    internal class MouseBeaconDrawSystem : ModSystem
    {
        [VaultLoaden("Assets/Effects/")]
        internal static Asset<Texture2D> DiffusionCircle { get; set; }

        public override void PostDrawTiles() {
            //PostDrawTiles 只在图形端执行，服务器不会进来；守卫照写，习惯性保险
            //
            //PostDrawTiles only runs on graphical clients, never on the server; the
            //guard stays anyway as cheap insurance
            if (Main.dedServ || Main.gameMenu) {
                return;
            }

            Player local = Main.LocalPlayer;
            if (local.HeldItem == null || local.HeldItem.type != ModContent.ItemType<MouseBeacon>()) {
                return;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState
                , DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Main.maxPlayers; i++) {
                Player player = Main.player[i];
                if (!player.active || player.dead) {
                    continue;
                }

                //TryGetSnapshot 是纯缓存读取，不会发包：远程玩家读到的是 KeepAlive
                //换来的、默认 30 帧内有效的快照，过期就拿不到，画面上的标记随之消失；
                //本地玩家走实时采样分支——没有网络的单人照样能画自己的鼠标。
                //
                //TryGetSnapshot is a pure cache read and never sends packets: for remote
                //players it returns the snapshot earned by KeepAlive, valid for 30 ticks
                //by default - once stale, nothing returns and the marker fades from the
                //screen. The local player takes the live-sampling branch, so solo play
                //with no network still draws your own cursor.
                if (!PlayerNetwork.TryGetSnapshot(player, out PlayerNetworkSnapshot snapshot)) {
                    continue;
                }
                if (!snapshot.Has(PlayerNetworkDataFlags.MouseWorld)) {
                    continue;
                }

                Texture2D glow = DiffusionCircle.Value;
                Vector2 drawPos = snapshot.MouseWorld - Main.screenPosition;
                Color markColor = snapshot.IsLocalPlayer ? Color.Gold : Color.Cyan;

                DrawMark(glow, drawPos, markColor, player.name);
            }

            Main.spriteBatch.End();
        }

        private static void DrawMark(Texture2D glow, Vector2 drawPos, Color color, string playerName) {
            float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.15f;
            Main.spriteBatch.Draw(glow, drawPos, null, color * 0.65f, 0f, glow.Size() / 2f, 0.22f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, drawPos, null, Color.White * 0.5f, 0f, glow.Size() / 2f, 0.1f * pulse, SpriteEffects.None, 0f);
            Utils.DrawBorderString(Main.spriteBatch, playerName, drawPos + new Vector2(0f, -28f), color, 0.8f, 0.5f);
        }
    }

    internal class MouseBeaconEntry : IDemoEntry
    {
        public string Id => "MouseBeacon";
        public string Chapter => "Networking";
        public string WikiSlug => "";
        public string SourcePath => "Content/Networking/MouseBeacon.cs";
        public int DemoItemType => ModContent.ItemType<MouseBeacon>();
        public int SortIndex => 0;
    }
}
