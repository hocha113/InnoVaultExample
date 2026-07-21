using InnoVault;
using InnoVault.UIHandles;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;

namespace InnoVaultExample.Content.Guide
{
    //示例目录面板：把 DemoCatalog 反射收集到的全部演示按章节列出来，
    //每行可以直接领取演示物品或跳到对应的 wiki 页。它同时也是 UIHandle 的主示例，
    //生命周期动画、标题栏拖拽、滚轮滚动、悬停防穿透这些常见需求都能在这里找到写法。
    //
    //The demo catalog panel: lists every demo collected by DemoCatalog, grouped by
    //chapter, with per-row buttons to claim the demo item or open the matching wiki
    //page. It doubles as the flagship UIHandle example - lifecycle animation, title
    //bar dragging, wheel scrolling and mouse blocking are all shown here.
    //https://innovault.wiki/cn/content/ui-handle/
    internal class GuideCatalogUI : UIHandle<GuideCatalogUI>
    {
        private const float PanelWidth = 560f;
        private const int HeaderHeight = 40;
        private const int FooterHeight = 26;
        private const int ContentPad = 14;
        private const int ScrollBarWidth = 6;
        private const int NameLineHeight = 26;
        private const int BodyLineHeight = 17;
        private const int SourceLineHeight = 16;
        private const int ChapterRowHeight = 30;
        private const int ButtonWidth = 84;
        private const int ButtonHeight = 22;
        private const float NameScale = 0.85f;
        private const float BodyScale = 0.72f;
        private const float SourceScale = 0.62f;

        //[VaultLoaden] 在客户端资源阶段自动填充这些静态成员，省掉手写 Request 样板；
        //路径以 '/' 结尾时会自动接上成员名作为文件名。服务器上它们保持默认值，
        //但 UI 的更新与绘制本来就只在客户端发生，所以不需要判空
        //
        //[VaultLoaden] fills these static members during client asset loading, no
        //Request boilerplate; a path ending in '/' appends the member name as the
        //file name. On a dedicated server they stay default, but UI update/draw
        //only ever happens client side, so no null checks are needed
        [VaultLoaden("InnoVaultExample/Assets/UI/")]
        private static Asset<Texture2D> Panel = null;
        [VaultLoaden("InnoVaultExample/Assets/UI/")]
        private static Asset<Texture2D> ButtonPanel = null;
        [VaultLoaden("InnoVaultExample/Assets/Catalog/")]
        private static Asset<Texture2D> Header = null;
        [VaultLoaden("InnoVaultExample/Assets/Catalog/")]
        private static Asset<Texture2D> Tab = null;
        [VaultLoaden("InnoVaultExample/Assets/Sounds/CatalogOpen")]
        private static SoundStyle CatalogSound = default;

        //目录里的一行。ChapterTitle 非空表示章节标头，否则是一条演示条目。
        //两种形态挤在一个类里是刻意的：示例代码优先让人一眼看完数据结构
        //
        //One list row. A non-null ChapterTitle marks a chapter header, otherwise it
        //is a demo entry. Cramming both shapes into one class is deliberate: example
        //code favors a data structure readable at a glance
        private sealed class CatalogRow
        {
            public string ChapterTitle;
            public IDemoEntry Entry;
            public string Name;
            public string[] BodyLines;
            public string Source;
            public float Height;
        }

        private readonly List<CatalogRow> rows = [];
        private readonly List<(CatalogRow row, float y)> visibleRows = [];
        private readonly List<(Rectangle rect, IDemoEntry entry, bool isWiki)> rowButtons = [];
        private Rectangle closeButtonRect;
        private int scrollRow;
        private int maxScrollRow;
        private float rowsTotalHeight;
        private float desiredPanelHeight;
        private float contentHeightCache;
        private bool positioned;
        private string titleText = "";
        private string claimLabel = "";
        private string wikiLabel = "";
        private string hintText = "";

        //这一排只读属性就是 UIHandle 的声明式开关：命中箱自动维护、悬停时挡住鼠标穿透、
        //Esc 关闭、左键在标题栏（右侧留出关闭按钮）拖拽。基类替我们处理全部细节
        //
        //This block of read-only overrides is UIHandle's declarative surface: hitbox
        //auto-maintained, mouse blocked while hovered, Esc closes, left-drag on the
        //title bar (minus the close button). The base class does all the bookkeeping
        public override bool AutoUpdateHitBox => true;
        public override bool BlockMouseWhenHovered => true;
        public override bool CloseOnEscape => true;
        public override bool CanDrag => true;
        public override MouseButtonType DragMouseButton => MouseButtonType.Left;
        public override Rectangle? DragHandleRect => new Rectangle(
            (int)DrawPosition.X, (int)DrawPosition.Y, (int)Size.X - 44, HeaderHeight);

        //开关音效交给基类播放：只在状态真正翻转时响一次，且自带 dedServ 保护。
        //关闭复用同一个 wav，压低音调即可听出方向
        //
        //Open/close sounds are played by the base class: exactly once per real state
        //flip, dedServ-guarded. Closing reuses the same wav pitched down
        public override SoundStyle? OpenSound => CatalogSound;
        public override SoundStyle? CloseSound => CatalogSound.WithPitchOffset(-0.25f);

        //值越大画得越靠上，但排序只在加载时做一次，运行期改这个属性没有效果。
        //目录是示例的入口，给个偏高的 5，读者照着例子新加的面板默认(1)会垫在它下面
        //
        //Higher draws on top, but the sort happens once at load - changing this at
        //runtime does nothing. The catalog is the entry point, so it sits at 5 and
        //any panel readers add with the default (1) lands underneath it
        public override float RenderPriority => 5f;

        protected override void OnOpen() {
            //每次打开都重建行列表：本地化文本和换行结果会跟着当前语言走，
            //玩家中途切语言后重开面板即可刷新
            //
            //Rebuild rows on every open: localized text and wrapping follow the
            //current language, so switching languages just needs a reopen
            BuildRows();
            scrollRow = 0;
            Size = new Vector2(PanelWidth, MathF.Min(desiredPanelHeight, Main.screenHeight - 120f));
            if (!positioned) {
                DrawPosition = (new Vector2(Main.screenWidth, Main.screenHeight) - Size) / 2f;
                positioned = true;
            }
        }

        private void BuildRows() {
            titleText = Language.GetTextValue("Mods.InnoVaultExample.UI.GuideCatalog.Title");
            claimLabel = Language.GetTextValue("Mods.InnoVaultExample.UI.GuideCatalog.Claim");
            wikiLabel = Language.GetTextValue("Mods.InnoVaultExample.UI.GuideCatalog.Wiki");
            hintText = Language.GetTextValue("Mods.InnoVaultExample.UI.GuideCatalog.Hint");

            rows.Clear();
            var font = FontAssets.MouseText.Value;
            float innerWidth = PanelWidth - ContentPad * 2 - ScrollBarWidth - 6;
            //条目名和两个按钮挤同一行，给名字留出剩余宽度
            //
            //The entry name shares its line with both buttons, so cap its width
            float nameWidth = innerWidth - (ButtonWidth + 6) * 2 - 8;

            string chapter = null;
            foreach (IDemoEntry entry in DemoCatalog.Entries) {
                //Entries 已按 Chapters 的固定顺序排好，章节切换时插一行标头即可，
                //没有条目的章节自然不会出现
                //
                //Entries are pre-sorted in fixed chapter order; insert a header row
                //whenever the chapter changes, empty chapters simply never show up
                if (entry.Chapter != chapter) {
                    chapter = entry.Chapter;
                    string chapterKey = $"Mods.InnoVaultExample.DemoCatalog.Chapters.{chapter}";
                    rows.Add(new CatalogRow {
                        ChapterTitle = Language.Exists(chapterKey) ? Language.GetTextValue(chapterKey) : chapter,
                        Height = ChapterRowHeight,
                    });
                }

                string baseKey = $"Mods.InnoVaultExample.DemoCatalog.Entries.{entry.Id}";
                string[] body = VaultUtils.WrapTextArray(
                    Language.GetTextValue(baseKey + ".HowToTry"), font, innerWidth - 6, BodyScale);
                rows.Add(new CatalogRow {
                    Entry = entry,
                    Name = VaultUtils.WrapTextJoin(
                        Language.GetTextValue(baseKey + ".Name"), font, nameWidth, NameScale, 1, true),
                    BodyLines = body,
                    Source = entry.SourcePath,
                    Height = NameLineHeight + body.Length * BodyLineHeight + SourceLineHeight + 8,
                });
            }

            rowsTotalHeight = 0f;
            foreach (CatalogRow row in rows) {
                rowsTotalHeight += row.Height;
            }
            desiredPanelHeight = HeaderHeight + 8 + rowsTotalHeight + 8 + FooterHeight;
        }

        public override void Update() {
            //高度跟随内容但不超出屏幕；位置每帧夹紧，窗口缩放后面板不会丢到屏幕外
            //
            //Height follows content but stays on screen; clamping every frame keeps
            //the panel reachable after a window resize
            Size = new Vector2(PanelWidth, MathF.Min(desiredPanelHeight, Main.screenHeight - 120f));
            DrawPosition.X = MathHelper.Clamp(DrawPosition.X, 8f, Main.screenWidth - Size.X - 8f);
            DrawPosition.Y = MathHelper.Clamp(DrawPosition.Y, 8f, Main.screenHeight - Size.Y - 8f);

            float contentTop = DrawPosition.Y + HeaderHeight + 8f;
            contentHeightCache = Size.Y - HeaderHeight - FooterHeight - 16f;

            //按"行"整行滚动而不是按像素：省掉裁剪矩形，每一行要么完整可见要么不画，
            //对示例来说这是复杂度和观感之间最划算的折中
            //
            //Scroll by whole rows instead of pixels: no scissor rectangle needed,
            //every row is either fully visible or not drawn - the cheapest
            //complexity/polish trade-off for an example
            if (hoverInMainPage) {
                //先向原版声明这一轮滚轮归 UI：mouseInterface 挡不住 Player.HandleHotbar，
                //不加这句的话在面板上滚动，底下的快捷栏会跟着换格
                //
                //Claim the wheel from vanilla for this cycle: mouseInterface does not
                //stop Player.HandleHotbar, so without this the hotbar underneath
                //cycles along while scrolling the panel
                PlayerInput.LockVanillaMouseScroll("InnoVaultExample/GuideCatalogUI");
                if (MouseScrollDelta != 0) {
                    scrollRow -= Math.Sign(MouseScrollDelta);
                }
            }
            maxScrollRow = CalcMaxScrollRow();
            scrollRow = Math.Clamp(scrollRow, 0, maxScrollRow);

            //布局在 Update 里算好存下来，Draw 只负责照着画，
            //这样点击判定和绘制永远用同一份矩形
            //
            //Layout is computed once here and cached; Draw just renders it, so hit
            //testing and drawing always agree on the same rectangles
            visibleRows.Clear();
            rowButtons.Clear();
            float y = contentTop;
            int buttonRight = (int)(DrawPosition.X + PanelWidth - ContentPad - ScrollBarWidth - 6);
            for (int i = scrollRow; i < rows.Count; i++) {
                CatalogRow row = rows[i];
                if (y + row.Height > contentTop + contentHeightCache + 0.5f) {
                    break;
                }
                visibleRows.Add((row, y));
                if (row.Entry != null) {
                    int right = buttonRight;
                    if (!string.IsNullOrEmpty(row.Entry.WikiSlug)) {
                        rowButtons.Add((new Rectangle(right - ButtonWidth, (int)y + 2, ButtonWidth, ButtonHeight), row.Entry, true));
                        right -= ButtonWidth + 6;
                    }
                    if (row.Entry.DemoItemType > 0) {
                        rowButtons.Add((new Rectangle(right - ButtonWidth, (int)y + 2, ButtonWidth, ButtonHeight), row.Entry, false));
                    }
                }
                y += row.Height;
            }

            closeButtonRect = new Rectangle((int)(DrawPosition.X + Size.X) - 34, (int)DrawPosition.Y + 8, 24, 24);

            //打开动画没走完之前不接受点击：淡入期间画面带滑移，命中箱和视觉位置略有偏差
            //
            //Ignore clicks until the open animation settles: the fade-in slides the
            //visuals, so hitboxes and pixels briefly disagree
            bool interactable = IsOpen && OpenProgress.Current > 0.85f;
            if (!interactable || keyLeftPressState != KeyPressState.Pressed) {
                return;
            }

            if (closeButtonRect.Contains(MousePoint)) {
                Close();
                return;
            }

            foreach ((Rectangle rect, IDemoEntry entry, bool isWiki) in rowButtons) {
                if (!rect.Contains(MousePoint)) {
                    continue;
                }
                if (isWiki) {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    Utils.OpenToURL(BuildWikiUrl(entry.WikiSlug));
                }
                else {
                    //UI 只在本地端运行，player 就是 Main.LocalPlayer；
                    //QuickSpawnItem 在多人下自己负责生成同步，客户端直接调用即可
                    //
                    //UI code runs locally, player is Main.LocalPlayer; QuickSpawnItem
                    //handles multiplayer spawn sync on its own, safe to call client side
                    SoundEngine.PlaySound(SoundID.Grab);
                    player.QuickSpawnItem(player.GetSource_Misc("InnoVaultExample_GuideCatalog"), entry.DemoItemType);
                }
                break;
            }
        }

        //倒着累加行高，找出"最后一屏"的起始行，滚动超过它只会看到空白
        //
        //Accumulate heights backwards to find the first row of the last full page;
        //scrolling past it would only reveal blank space
        private int CalcMaxScrollRow() {
            float h = 0f;
            for (int i = rows.Count - 1; i >= 0; i--) {
                h += rows[i].Height;
                if (h > contentHeightCache) {
                    return i + 1;
                }
            }
            return 0;
        }

        private static string BuildWikiUrl(string slug) {
            bool chinese = Language.ActiveCulture.LegacyId == (int)GameCulture.CultureName.Chinese;
            return $"https://innovault.wiki/{(chinese ? "cn" : "en")}/{slug}/";
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //OpenProgress 由基类跟随 Open/Close 自动过渡，隐式转 float 后直接当
            //透明度和位移系数用；关闭后它没归零前 Draw 仍会被调用，淡出是白送的
            //
            //OpenProgress is tweened by the base class; the implicit float conversion
            //makes it a ready-made alpha/offset factor. Draw keeps running while it
            //fades back to zero, so the fade-out comes for free
            float alpha = OpenProgress;
            if (alpha <= 0f) {
                return;
            }
            Vector2 slide = new Vector2(0f, -16f * (1f - alpha));

            Rectangle panelRect = new Rectangle(
                (int)(DrawPosition.X + slide.X), (int)(DrawPosition.Y + slide.Y), (int)Size.X, (int)Size.Y);
            Utils.DrawSplicedPanel(spriteBatch, Panel.Value,
                panelRect.X, panelRect.Y, panelRect.Width, panelRect.Height, 12, 12, 12, 12, Color.White * alpha);
            Utils.DrawSplicedPanel(spriteBatch, Header.Value,
                panelRect.X, panelRect.Y, panelRect.Width, HeaderHeight, 6, 6, 6, 6, Color.White * alpha);

            //GlobalTimer 是基类按真实时间累加的秒数，拿来做呼吸感最方便
            //
            //GlobalTimer is wall-clock seconds accumulated by the base class,
            //perfect for a subtle breathing highlight
            float pulse = 0.85f + 0.15f * MathF.Sin(GlobalTimer * 2.4f);
            Utils.DrawBorderString(spriteBatch, titleText,
                new Vector2(panelRect.X + ContentPad, panelRect.Y + HeaderHeight / 2f),
                Color.White * (alpha * pulse), 0.9f, 0f, 0.5f);

            Rectangle closeView = closeButtonRect;
            closeView.Offset((int)slide.X, (int)slide.Y);
            bool hoverClose = closeButtonRect.Contains(MousePoint);
            Utils.DrawSplicedPanel(spriteBatch, ButtonPanel.Value,
                closeView.X, closeView.Y, closeView.Width, closeView.Height, 4, 4, 4, 4,
                (hoverClose ? Color.White : Color.White * 0.75f) * alpha);
            Utils.DrawBorderString(spriteBatch, "X",
                closeView.Center.ToVector2(), Color.White * alpha, 0.8f, 0.5f, 0.5f);

            float innerWidth = PanelWidth - ContentPad * 2 - ScrollBarWidth - 6;
            foreach ((CatalogRow row, float rowY) in visibleRows) {
                Vector2 rowPos = new Vector2(DrawPosition.X + ContentPad, rowY) + slide;
                if (row.ChapterTitle != null) {
                    Utils.DrawSplicedPanel(spriteBatch, Tab.Value,
                        (int)rowPos.X, (int)rowPos.Y + 3, (int)innerWidth, ChapterRowHeight - 6, 4, 4, 4, 4, Color.White * alpha);
                    Utils.DrawBorderString(spriteBatch, row.ChapterTitle,
                        new Vector2(rowPos.X + 10f, rowPos.Y + ChapterRowHeight / 2f),
                        Color.LightGoldenrodYellow * alpha, 0.8f, 0f, 0.5f);
                    continue;
                }

                Utils.DrawBorderString(spriteBatch, row.Name,
                    new Vector2(rowPos.X, rowPos.Y + NameLineHeight / 2f),
                    Color.White * alpha, NameScale, 0f, 0.5f);
                float lineY = rowPos.Y + NameLineHeight;
                foreach (string line in row.BodyLines) {
                    Utils.DrawBorderString(spriteBatch, line,
                        new Vector2(rowPos.X + 6f, lineY), new Color(225, 225, 225) * alpha, BodyScale);
                    lineY += BodyLineHeight;
                }
                //源码路径灰字放行尾：目录的真正用途是把读者带到代码那里去
                //
                //The source path sits in gray at the row's end: the catalog's real
                //job is walking readers over to the code
                Utils.DrawBorderString(spriteBatch, row.Source,
                    new Vector2(rowPos.X + 6f, lineY), new Color(150, 150, 150) * alpha, SourceScale);
            }

            foreach ((Rectangle rect, IDemoEntry _, bool isWiki) in rowButtons) {
                Rectangle view = rect;
                view.Offset((int)slide.X, (int)slide.Y);
                bool hover = rect.Contains(MousePoint);
                Utils.DrawSplicedPanel(spriteBatch, ButtonPanel.Value,
                    view.X, view.Y, view.Width, view.Height, 4, 4, 4, 4,
                    (hover ? Color.White : Color.White * 0.75f) * alpha);
                Utils.DrawBorderString(spriteBatch, isWiki ? wikiLabel : claimLabel,
                    view.Center.ToVector2(), Color.White * alpha, 0.68f, 0.5f, 0.5f);
            }

            if (maxScrollRow > 0) {
                DrawScrollBar(spriteBatch, panelRect, alpha);
            }

            //HoverProgress 也是基类自动过渡的：鼠标靠近时操作提示才亮起来，平时不抢戏
            //
            //HoverProgress is another base-class tween: the usage hint only lights up
            //while the mouse is over the panel
            float hintAlpha = alpha * (0.35f + 0.65f * HoverProgress);
            Utils.DrawBorderString(spriteBatch, hintText,
                new Vector2(panelRect.X + ContentPad, panelRect.Bottom - FooterHeight + 3f),
                new Color(170, 175, 190) * hintAlpha, 0.62f);
        }

        private void DrawScrollBar(SpriteBatch spriteBatch, Rectangle panelRect, float alpha) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int trackX = panelRect.Right - ContentPad - ScrollBarWidth;
            int trackY = panelRect.Y + HeaderHeight + 8;
            int trackH = (int)contentHeightCache;
            spriteBatch.Draw(pixel, new Rectangle(trackX, trackY, ScrollBarWidth, trackH), Color.Black * (0.35f * alpha));

            int thumbH = Math.Max(24, (int)(trackH * (contentHeightCache / rowsTotalHeight)));
            int thumbY = trackY + (int)((trackH - thumbH) * (scrollRow / (float)maxScrollRow));
            //滑块用面板边框同款的青色，顺便当"还有更多内容"的视觉暗示
            //
            //The thumb reuses the panel border's teal and doubles as a "there is
            //more below" cue
            spriteBatch.Draw(pixel, new Rectangle(trackX, thumbY, ScrollBarWidth, thumbH), new Color(1, 146, 147) * alpha);
        }
    }
}
