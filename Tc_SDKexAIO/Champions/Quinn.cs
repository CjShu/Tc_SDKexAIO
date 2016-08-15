namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using System;
    using System.Linq;
    using Tc_SDKexAIO.Common;

    internal static class Quinn
    {
        private static Menu Menu => PlaySharp.Menu;
        private static Spell Q, W, E, R;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static Obj_AI_Hero Player => PlaySharp.Player;

        internal static void Init()
        {


        }
    }
}
