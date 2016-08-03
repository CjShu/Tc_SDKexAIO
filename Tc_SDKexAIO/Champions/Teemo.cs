﻿namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.Enumerations;

    using Common;
    using Config;

    using Menu = LeagueSharp.SDK.UI.Menu;

    using SharpDX;

    using System;
    using System.Linq;
    using System.Dynamic;
    using System.Collections.Generic;

    using static Common.Manager;

    using Geometry = Common.Geometry;


    internal static class Teemo
    {

        private static Menu Menu => PlaySharp.Menu;

        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static Spell Q, W, E, R;
        private static Vector3 LastQ;
        private static bool CastQQQ = false;

        internal static void Init()
        {

            Q = new Spell(SpellSlot.Q, 680).SetTargetted(0.5f, 1500f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 300).SetSkillshot(0.5f, 120f, 1000f, false, SkillshotType.SkillshotCircle);


            var QMenu = Menu.Add(new Menu("Q", "Q.Set | Q 設定"));
            {
                QMenu.GetBool("ComboQ", "Comno Q");
                QMenu.GetBool("HarassQ", "Harass Q");
                QMenu.GetBool("LaneClearQ", "LaneClear Q");
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("ComboW", "Combo W", false);
                WMenu.GetBool("WRange", "Use W if enemy is in range only", false);
            }
        }
    }
}