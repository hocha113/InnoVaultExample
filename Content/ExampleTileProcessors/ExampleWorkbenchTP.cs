﻿using InnoVault.TileProcessors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace InnoVaultExample.Content.ExampleTileProcessors
{
    //我们建议任何TileProcessor的子类都加上TP后缀
    //这个TP实体将与原版的木制工作台关联
    //不同于TileEntity，我们无需去Tile哪里编写任何代码，因为TileProcessor是相当独立的
    internal class ExampleWorkbenchTP : TileProcessor
    {
        //重写这个属性，让它指向TileID.WorkBenches，即原版工作台的物块ID
        //这将让游戏在玩家放置工作台时自动生成这个TP实体并进行挂载
        //世界中的所有工作台也都会自动被这个类型的TP实体挂载上去
        public override int TargetTileID => TileID.WorkBenches;

        private Vector2 Center => PosInWorld + new Vector2(16, 8);
        private Color tpColor = Color.White;
        private float light;

        //仅仅展示这个可重写函数的作用，当TP实体被放置生成，或者在世界初始化时自动生成时，这个函数都会被调用一次，我们可以使用它来设置一些值
        public override void SetProperty() {
            tpColor = new Color(Main.rand.NextFloat(), Main.rand.NextFloat(), Main.rand.NextFloat());
            light = 1 + Main.rand.NextFloat();
        }

        //这个更新函数会在所有客户端与服务器上运行，每帧调用
        public override void Update() {
            //比如，我们让这个TP实体在更新中发出不同颜色不同强度光亮
            //你可以选择在这里做任何事情，唯一需要注意的是，如是在多人模式下，生成Projectile或者NPC等操作只能在服务器上运行
            Lighting.AddLight(Center, tpColor.ToVector3() * light);
        }

        //这个绘制函数将运行你在物块上画一些有趣的东西，在这里，我们让工作台像展览台一样把玩家手上的物品画出来
        public override void Draw(SpriteBatch spriteBatch) {
            int itemType = (Main.mouseItem.IsAir ? Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem] : Main.mouseItem).type;
            Vector2 orig = default;
            if (itemType > ItemID.None) {
                Main.instance.LoadItem(itemType);
                Texture2D texture = TextureAssets.Item[itemType].Value;
                Rectangle? rectangle = Main.itemAnimations[itemType] != null 
                    ? Main.itemAnimations[itemType].GetFrame(texture) : texture.Frame(1, 1, 0, 0);
                orig = texture.Size() / 2;
                if (rectangle.HasValue) {
                    orig = rectangle.Value.Size() / 2;
                }
                float gfkY = -16 - rectangle.Value.Height / 2 + MathF.Sin(Main.GameUpdateCount * 0.1f) * 4;
                Vector2 drawPos = Center + new Vector2(0, gfkY);
                drawPos -= Main.screenPosition;
                spriteBatch.Draw(texture, drawPos, rectangle, Color.White, 0, orig, 1, SpriteEffects.None, 0);
            }
        }
    }
}