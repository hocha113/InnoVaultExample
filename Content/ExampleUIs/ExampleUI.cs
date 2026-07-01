using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace InnoVaultExample.Content.ExampleUIs
{
    // Showcase: Legacy Example UI
    // Demonstrates: non-generic UIHandle, manual Active tween
    // Wiki: https://innovault.wiki/cn/content/ui-handle/
    // Source: InnoVaultExample/Content/ExampleUIs/ExampleUI.cs
    // Legacy: prefer ApiGuideUI for new UIHandle code.
    internal class ExampleUI : UIHandle
    {
        //这个属性默认指向Vanilla_Mouse_Text，也就是鼠标文本层次
        //它的返回值决定了UI会在那个更新集合中运行，不要试图在游戏运行过程改变这个值，
        //不会有效果，UI的更新集合只会在游戏加载时分配一次
        public override LayersModeEnum LayersMode => base.LayersMode;

        public override Texture2D Texture => TextureAssets.MagicPixel.Value;

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
            Rectangle panel = new Rectangle((int)DrawPosition.X, (int)DrawPosition.Y, (int)(220 * _sengs), (int)(120 * _sengs));
            spriteBatch.Draw(Texture, panel, new Color(42, 56, 88) * _sengs);
            Utils.DrawBorderString(spriteBatch, "Old UIHandle sample", DrawPosition + new Vector2(16, 18), Color.White * _sengs, 0.8f);
        }
    }
}
