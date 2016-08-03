namespace Tc_SDKexAIO.Champions
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
    using System.Windows.Forms;

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
        private static float LasQ = Variables.TickCount;
        private static Vector3 Position;
        private static bool ShotR => Player.HasBuff("ToxicShot");
        private static int RCast;
        private static float RRange => 300 * R.Level;
        private static bool Trap => GameObjects.Get<Obj_AI_Base>().Where(x => x.Name == "Noxious Trap").Any(x => Position.Distance(x.Position) <= 250);

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
                QMenu.GetBool("ADQ", "Use Q AD");
                QMenu.GetBool("KSQ", "KillStel Q");
                QMenu.GetBool("CheckAA", "Check AA", false);
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("ComboW", "Combo W", false);
                WMenu.GetBool("WRange", "Use W if enemy is in range only", false);
                WMenu.GetBool("AutoW", "Auto W", false);
                WMenu.GetKeyBind("FleeKey", "Flee Use W Key", Keys.Z, KeyBindType.Press);
            }
            var RMenu = Menu.Add(new Menu("R", "R.Set | R 設定"));
            {
                RMenu.GetBool("Charge", "Charges of R before using R :)", false);
                RMenu.Add(new MenuKeyBind("AutoR", "Auto R Key", Keys.T, KeyBindType.Toggle));
                RMenu.GetSlider("RCount", "R Count >=", 1, 1, 5);
                RMenu.GetBool("Gapcloser", "Gapcloser R");
            }

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.GetBool("Q", "Q Range");
                DrawMenu.GetBool("R", "R Range");
                DrawMenu.GetBool("DrawDamge", "Draw Damge Ks", false);
            }

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Events.OnGapCloser += OnGapCloser;
            Drawing.OnDraw += OnDraw;
            Variables.Orbwalker.OnAction += OnAction;
        }

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            try
            {
                var ComboQ = Menu["Q"]["ComboQ"].GetValue<MenuBool>().Value;
                var HarassQ = Menu["Q"]["HarassQ"].GetValue<MenuBool>().Value;
                var ADQ = Menu["Q"]["ADQ"].GetValue<MenuBool>();
                var CheckQA = Menu["Q"]["CheckAA"].GetValue<MenuBool>();

                if (args.Type == OrbwalkingType.AfterAttack)
                {
                    var QTarget = GetTarget(680, Q.DamageType);

                    var Attack = GetAttackRange(QTarget);

                    if (QTarget != null && Combo && ComboQ)
                    {
                        if (CheckQA)
                        {
                            if (ADQ && Marksman.Contains(QTarget.CharData.BaseSkinName) && Q.IsReady() && Q.IsInRange(QTarget, -170))
                            {
                                Q.Cast(QTarget);
                            }
                            else if (Q.IsReady() && Q.IsInRange(QTarget, -100))
                            {
                                Q.Cast(QTarget);
                            }
                        }
                        else if (ADQ && Marksman.Contains(QTarget.CharData.BaseSkinName) && Q.IsReady() && Q.IsInRange(QTarget))
                        {
                            Q.Cast(QTarget);
                        }
                        else if (ADQ && Q.IsReady() && Q.IsInRange(QTarget))
                        {
                            Q.Cast(QTarget);
                        }
                    }
                    if (QTarget != null && Harass && HarassQ)
                    {
                        if (CheckQA)
                        {
                            if (Q.IsReady() && Q.IsInRange(QTarget, -70))
                            {
                                Q.Cast(QTarget);
                            }
                        }
                        else if (Q.IsReady() && Q.IsReady() && Q.IsInRange(QTarget))
                        {
                            Q.Cast(QTarget);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In OnAction" + ex);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            if (!R.IsReady()) return;
                
            if (Menu["R"]["Gapcloser"].GetValue<MenuBool>())
            {
                if (args.Sender.IsValidTarget() && args.Sender.IsFacing(Player) && args.Sender.IsTargetable)
                {
                    R.Cast(args.Sender.Position);
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "TeemoRCast")
            {
                RCast = Variables.TickCount;
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            try
            {

                R = new Spell(SpellSlot.R, RRange);

                if (Player.IsDead)
                    return;

                //ComboLogic(args);

                //LaneClear(args);

                //JungleClear(args);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In OnUpdate" + ex);
            }
        }

        private static string[] Marksman =
        {
            "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Jinx", "Kalista",
            "KogMaw", "Lucian", "MissFortune", "Quinn", "Sivir", "Teemo", "Tristana", "Twitch", "Urgot", "Varus",
            "Vayne"
        };
    }
}