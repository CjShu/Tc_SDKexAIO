namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.UI;

    using System;
    using System.Linq;

    using Common;
    using Config;
    using Core;
    using static Common.Manager;

    using Menu = LeagueSharp.SDK.UI.Menu;
    using Geometry = Common.Geometry;


    internal static class Jhin
    {

        private static Menu Menu => PlaySharp.Menu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static float LasPing = Variables.TickCount;
        private static string StartR = "JhinR";
        private static string ShotR = "JhinRShot";
        private static Spell Q, W, E, R;

        internal static void Init()
        {



        }



    }
}
