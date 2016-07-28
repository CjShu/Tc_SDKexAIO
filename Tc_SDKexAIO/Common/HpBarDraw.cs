namespace Tc_SDKexAIO.Common
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Utils;
    using SharpDX;
    using SharpDX.Direct3D9;
    using System;
    using System.Linq;

    public class HpBarDraw
    {
        public static Device DxDevice = Drawing.Direct3DDevice;
        public static Line DxLine;
        public static Font TextStatus, Text, TextLittle;
        public static string Tab => "       ";
        public static float Hight = 9;
        public static float Width = 104;

        public static Obj_AI_Hero Unit { get; set; }

        private static Vector2 Offset
        {
            get
            {
                if (Unit != null)
                {
                    return Unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public static Vector2 StartPosition
        {
            get { return new Vector2(Unit.HPBarPosition.X + Offset.X, Unit.HPBarPosition.Y + Offset.Y); }
        }

        public HpBarDraw()
        {
            DxLine = new Line(DxDevice) { Width = 9 };

            TextStatus = new Font(Drawing.Direct3DDevice, new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 17,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                    Weight = FontWeight.Bold
                });
            Text = new Font(Drawing.Direct3DDevice, new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 19,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });
            TextLittle = new Font(Drawing.Direct3DDevice, new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });

            Drawing.OnPreReset += OnPreReset;
            Drawing.OnPostReset += OnPostReset;
            AppDomain.CurrentDomain.DomainUnload += Unload;
            AppDomain.CurrentDomain.ProcessExit += Unload;
        }

        private static void Unload(object sender, EventArgs eventArgs)
        {
            DxLine.Dispose();
        }

        private static void OnPostReset(EventArgs args)
        {
            DxLine.OnResetDevice();
        }

        private static void OnPreReset(EventArgs args)
        {
            DxLine.OnLostDevice();
        }

        private static float GetHpProc(float dmg = 0)
        {
            float Health = ((Unit.Health - dmg) > 0) ? (Unit.Health - dmg) : 0;
            return (Health / Unit.MaxHealth);
        }

        private static float GetManaProc(float manaPer)
        {
            return (manaPer / GameObjects.Player.MaxMana);
        }

        private static Vector2 GetHpPosAfterDmg(float dmg)
        {
            float w = GetHpProc(dmg) * Width;
            float m = GetManaProc(dmg) * Width;
            return new Vector2(StartPosition.X + w, StartPosition.Y);
        }

        public static void DrawDmg(float dmg, ColorBGRA color)
        {
            Vector2 hpPosNow = GetHpPosAfterDmg(0);
            Vector2 hpPosAfter = GetHpPosAfterDmg(dmg);

            FullHPBar(hpPosNow, hpPosAfter, color);
        }

        private void FullHPBar(int to, int from, System.Drawing.Color color)
        {
            var sPos = StartPosition;

            for (var i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private static void FillManaBar(Vector2 pos, ColorBGRA color)
        {
            DxLine.Begin();
            DxLine.Draw(
                new[] { new Vector2((int)pos.X, (int)pos.Y + 4f), new Vector2((int)pos.X + 2, (int)pos.Y + 4f) },
                color);
            DxLine.End();
        }

        private static void FullHPBar(Vector2 from, Vector2 to, ColorBGRA color)
        {
            DxLine.Begin();

            DxLine.Draw(new[]
            {
                new Vector2((int) from.X, (int) from.Y + 4f),
                new Vector2((int) to.X, (int) to.Y + 4f)
            }, color);

            DxLine.End();
        }

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int)vPosX, (int)vPosY, vColor);
        }

        public static void DrawRange(Spell spell, System.Drawing.Color color, bool draw = true, bool checkCoolDown = false)
        {
            if (!draw)
            {
                return;
            }

            if (checkCoolDown)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range,
                    spell.IsReady() ? color : System.Drawing.Color.Gray,
                    spell.IsReady() ? 5 : 1);
            }
            else
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, color, 1);
            }
        }

        public static Vector3 CenterOfVectors(Vector3[] vectors)
        {
            var sum = Vector3.Zero;
            if (vectors == null || vectors.Length == 0)
                return sum;

            sum = vectors.Aggregate(sum, (current, vec) => current + vec);
            return sum / vectors.Length;
        }
    }
}
