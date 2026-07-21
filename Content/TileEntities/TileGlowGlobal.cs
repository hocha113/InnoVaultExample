using InnoVault.TileProcessors;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace InnoVaultExample.Content.TileEntities
{
    //GlobalTileProcessor 演示：鼠标悬停在世界里任意一个 TP 实体上时，
    //给它的碰撞框描一圈呼吸高亮。全局类不用挂到具体物块上，
    //子类写好就会被 InnoVault 自动扫描注册，对所有 TP 生效。
    //
    //A GlobalTileProcessor demo: hover the mouse over any TP entity in the world
    //and its hitbox gets a breathing highlight outline. A global class attaches to
    //nothing in particular; subclass it and InnoVault's scan registers it against
    //every TP automatically.
    //https://innovault.wiki/cn/content/tile-processor/

    internal class TileGlowGlobal : GlobalTileProcessor
    {
        //PostDraw 逐实例地跑在 TileProcessor.Draw 之后，此时画布已经打开，
        //且框架只对 Active 且在屏幕内的实体调用，不用自己做这些筛选。
        //绘制钩子只存在于客户端，无需 dedServ 防御
        //
        //PostDraw runs per instance right after TileProcessor.Draw, with the sprite
        //batch already open, and the framework only calls it for active on-screen
        //entities, so none of that filtering is our job. Draw hooks exist on clients
        //only; no dedServ guard needed.
        public override void PostDraw(TileProcessor tileProcessor, SpriteBatch spriteBatch) {
            //HoverTP 由框架每帧计算，除了普通的鼠标悬停，还包含智能光标 snap 到
            //该实体图格的情况；它只在本地客户端上有意义
            //
            //HoverTP is computed by the framework every tick; besides plain mouse
            //hover it also covers smart cursor snapping onto the entity's tile. It is
            //only meaningful on the local client.
            if (!tileProcessor.HoverTP) {
                return;
            }

            Rectangle box = tileProcessor.HitBox;
            box.Inflate(2, 2);

            float wave = 0.5f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.25f;
            DrawOutline(spriteBatch, box, Color.Gold * wave);
        }

        private static void DrawOutline(SpriteBatch spriteBatch, Rectangle box, Color color) {
            //用 MagicPixel 的一个像素拼出四条边。HitBox 是世界坐标，
            //这一层画布带缩放矩阵，减掉 screenPosition 即可对齐
            //
            //Four edges assembled from a single MagicPixel texel. HitBox is in world
            //space; this batch carries the zoom matrix, so subtracting screenPosition
            //is all the alignment needed.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle onePixel = new(0, 0, 1, 1);
            int x = (int)(box.X - Main.screenPosition.X);
            int y = (int)(box.Y - Main.screenPosition.Y);

            spriteBatch.Draw(pixel, new Rectangle(x, y, box.Width, 2), onePixel, color);
            spriteBatch.Draw(pixel, new Rectangle(x, y + box.Height - 2, box.Width, 2), onePixel, color);
            spriteBatch.Draw(pixel, new Rectangle(x, y + 2, 2, box.Height - 4), onePixel, color);
            spriteBatch.Draw(pixel, new Rectangle(x + box.Width - 2, y + 2, 2, box.Height - 4), onePixel, color);
        }
    }

    //这条演示没有对应物品，DemoItemType 保持默认的 0
    //
    //This demo hands out no item, so DemoItemType stays at the default 0
    internal sealed class TileGlowEntry : IDemoEntry
    {
        public string Id => "TileGlow";
        public string Chapter => "TileEntities";
        public string WikiSlug => "content/tile-processor";
        public string SourcePath => "Content/TileEntities/TileGlowGlobal.cs";
        public int SortIndex => 2;
    }
}
