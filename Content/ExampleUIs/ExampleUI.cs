using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InnoVaultExample.Content.ExampleUIs
{
    internal class ExampleUI : UIHandle
    {
        //这个属性默认指向Vanilla_Mouse_Text，也就是鼠标文本层次
        //它的返回值决定了UI会在那个更新集合中运行，不要试图在游戏运行过程改变这个值，
        //不会有效果，UI的更新集合只会在游戏加载时分配一次
        public override LayersModeEnum LayersMode => base.LayersMode;

        public override Texture2D Texture => Mod.Assets.Request<Texture2D>("Asset/ExampleUIPanel").Value;

        internal bool _active;

        internal float _sengs;

        public override bool Active => _active || _sengs > 0;

        public override void Update() {
            //无论如何，我们应该尽量尊重DrawPosition字段，统一一个绘制原点是非常重要的
            DrawPosition = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + new Vector2(0, -600 + 600 * _sengs);
            if (_active) {
                if (_sengs < 1) {
                    _sengs += 0.02f;
                }
            }
            else {
                if (_sengs > 0) {
                    _sengs -= 0.02f;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Texture, DrawPosition, null, Color.White * _sengs
                , 0, Vector2.Zero, 2 * _sengs, SpriteEffects.None, 0);
        }
    }
}
