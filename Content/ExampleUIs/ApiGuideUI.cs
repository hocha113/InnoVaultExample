using InnoVault.UIHandles;

using InnoVaultExample.Content;

using InnoVaultExample.Content.Academy.Persistence;

using InnoVaultExample.Core;

using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Graphics;

using Terraria;

using Terraria.Audio;

using Terraria.GameContent;

using Terraria.ID;



namespace InnoVaultExample.Content.ExampleUIs

{

    // Showcase: Course Catalog UI

    // Demonstrates: UIHandle<T> singleton, drag handle, registry-driven course list

    // Wiki: https://innovault.wiki/cn/content/ui-handle/

    // Source: InnoVaultExample/Content/ExampleUIs/ApiGuideUI.cs

    internal class ApiGuideUI : UIHandle<ApiGuideUI>

    {

        private static readonly string[] IntroLines =

        [

            "Vault Academy Course Catalog (v0.9)",

            "",

            "Left click manual: Academy Beacon Actor + PRT at cursor.",

            "Right click manual: toggle this panel.",

            "Drag title bar to move. ESC closes (CloseOnEscape).",

            "",

            "Environment traps (see Wiki utilities/environment):",

        ];



        private static readonly string[] TrapLines =

        [

            CommentStandards.TrapDedServ,

            CommentStandards.TrapListenHost,

            CommentStandards.TrapSyncVarAuthority,

            CommentStandards.TrapActorSpawn,

            CommentStandards.TrapSaveMod,

        ];



        public override bool AutoUpdateHitBox => true;

        public override bool BlockMouseWhenHovered => true;

        public override bool CanDrag => true;

        public override bool CloseOnEscape => true;

        public override SoundStyle? OpenSound => SoundID.MenuOpen;

        public override SoundStyle? CloseSound => SoundID.MenuClose;

        public override float RenderPriority => 4f;



        public override Rectangle? DragHandleRect => new Rectangle(

            (int)DrawPosition.X,

            (int)DrawPosition.Y,

            (int)Size.X,

            34

        );



        protected override void OnOpen() {

            ExampleGuideSaveData.RecordPanelOpen();

        }



        public override void Update() {

            float lineCount = IntroLines.Length + TrapLines.Length + AcademyRegistry.All.Count * 3f + 8f;

            Size = new Vector2(640f, 48f + lineCount * 19f);

            Size.Y = MathHelper.Clamp(Size.Y, 360f, Main.screenHeight - 48f);



            if (DrawPosition == Vector2.Zero) {

                DrawPosition = new Vector2(Main.screenWidth, Main.screenHeight) / 2f - Size / 2f;

            }



            DrawPosition.X = MathHelper.Clamp(DrawPosition.X, 12f, Main.screenWidth - Size.X - 12f);

            DrawPosition.Y = MathHelper.Clamp(DrawPosition.Y, 12f, Main.screenHeight - Size.Y - 12f);

        }



        public override void Draw(SpriteBatch spriteBatch) {

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float opacity = OpenProgress;

            Rectangle panel = new Rectangle((int)DrawPosition.X, (int)DrawPosition.Y, (int)Size.X, (int)Size.Y);

            Rectangle titleBar = new Rectangle(panel.X, panel.Y, panel.Width, 34);



            spriteBatch.Draw(pixel, panel, new Color(20, 26, 36) * (0.88f * opacity));

            spriteBatch.Draw(pixel, titleBar, new Color(46, 77, 110) * (0.95f * opacity));

            DrawBorder(spriteBatch, pixel, panel, Color.CornflowerBlue * opacity);



            Vector2 textPos = DrawPosition + new Vector2(18f, 14f);

            for (int i = 0; i < IntroLines.Length; i++) {

                Color color = i == 0 ? Color.LightCyan : Color.White;

                Utils.DrawBorderString(spriteBatch, IntroLines[i], textPos, color * opacity, 0.82f);

                textPos.Y += i == 0 ? 26f : 21f;

            }



            foreach (string trapLine in TrapLines) {

                Utils.DrawBorderString(

                    spriteBatch,

                    $"  {trapLine}",

                    textPos,

                    Color.Orange * (0.88f * opacity),

                    0.64f);

                textPos.Y += 17f;

            }



            textPos.Y += 6f;

            Utils.DrawBorderString(

                spriteBatch,

                $"Registered showcases (Phases 0-6, {AcademyRegistry.All.Count} entries):",

                textPos,

                Color.LightGoldenrodYellow * opacity,

                0.76f);

            textPos.Y += 24f;



            foreach (ShowcaseEntry entry in AcademyRegistry.All) {

                Utils.DrawBorderString(

                    spriteBatch,

                    $"[P{entry.Phase}] [{entry.Tier}] {entry.DisplayName}",

                    textPos,

                    Color.LightGoldenrodYellow * opacity,

                    0.76f);

                textPos.Y += 19f;



                Utils.DrawBorderString(

                    spriteBatch,

                    $"  Try: {entry.TryInGame}",

                    textPos,

                    Color.White * (0.92f * opacity),

                    0.68f);

                textPos.Y += 17f;



                Utils.DrawBorderString(

                    spriteBatch,

                    $"  Wiki: {entry.WikiSlug}",

                    textPos,

                    Color.LightGreen * (0.85f * opacity),

                    0.64f);

                textPos.Y += 20f;

            }



            AcademyProgressModule progress = AcademyStudentPlayer.Get(Main.LocalPlayer).Progress;

            string courseProgress =

                $"Courses: PRT-101={CourseFlag(progress.CoursePrt101Complete)}  " +

                $"COMBAT-201={CourseFlag(progress.CourseCombat201Complete)}  " +

                $"WORLD-301={CourseFlag(progress.CourseWorld301Complete)}  " +

                $"({progress.CompletedCourseCount}/3 complete)";



            string stats =

                $"Guide uses: {ExampleGuideSaveData.TotalGuideUses}   " +

                $"Actors: {ExampleGuideSaveData.TotalActorsSpawned}   " +

                $"Panel opens: {ExampleGuideSaveData.PanelOpenCount}";



            Utils.DrawBorderString(

                spriteBatch,

                courseProgress,

                DrawPosition + new Vector2(18f, Size.Y - 52f),

                Color.LightCyan * opacity,

                0.72f);

            Utils.DrawBorderString(

                spriteBatch,

                stats,

                DrawPosition + new Vector2(18f, Size.Y - 32f),

                Color.LightGreen * opacity,

                0.72f);

        }



        private static string CourseFlag(bool complete) => complete ? "done" : "open";



        private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color) {

            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);

            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);

            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);

            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);

        }

    }

}


