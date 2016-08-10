namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.Enumerations;

    using System;
    using System.Linq;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Collections.Generic;

    using Common;
    using Config;
    using Core;

    using static Common.Manager;
    using Geometry = Common.Geometry;
    using Menu = LeagueSharp.SDK.UI.Menu;

    internal static class Kayle
    {
        private static Menu Menu => PlaySharp.Menu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static Spell Q, W, E, R;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static int IgniteRange = 600;
        private static SpellSlot Ignite = SpellSlot.Unknown;

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 670);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 660);
            R = new Spell(SpellSlot.R, 900);

            var QMenu = Menu.Add(new Menu("Q", "Q.Set | Q 設定"));
            {
                QMenu.GetSeparator("Q: Always On");
                QMenu.GetBool("AutoQ", "Auto Q");
                QMenu.GetBool("JungleQ", "Jungle Q");
                QMenu.GetSliderButton("QMana", "Q Mana", 50, 0, 99);
                var QList = QMenu.Add(new Menu("QList", "HarassQ List:", false));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => QMenu.GetBool(i.ChampionName.ToLower(), i.ChampionName, PlaySharp.AutoEnableList.Contains(i.ChampionName)));
                    }
                }
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("AutoW", "Auto W");
                WMenu.GetBool("AutoWSpeed", "Auto W Speed", false);
                WMenu.GetSeparator("Auto W Ally Mobs");
                foreach (var ally in GameObjects.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly))
                {
                    WMenu.GetBool("Wally", "W ally" + ally.ChampionName, false);
                    WMenu.GetSlider("WallyHp" + ally.ChampionName, "WallyHp", 40);
                }
                WMenu.GetSlider("WallyMp", "W ally Min Mp", 40);
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set | E 設定"));
            {
                EMenu.GetBool("AutoE", "Auto E Combo");
                EMenu.GetBool("HarrasE", "Harras E", false);
                EMenu.GetBool("LaneClearE", "LaneClear E", false);
                EMenu.GetBool("JungleE", "Jungle E", false);
            }
            var RMenu = Menu.Add(new Menu("R", "R.Set | R 設定"));
            {
                RMenu.GetBool("AutoR", "Auto R Me", false);
                RMenu.GetSeparator("Auto R ally Mobs");
                foreach (var ally in GameObjects.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly))
                {
                    RMenu.GetBool("Rally", "R ally" + ally.ChampionName, false);
                }
                foreach (var enemy in GameObjects.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var spell = enemy.Spellbook.Spells[i];
                        if (spell.SData.TargettingType != SpellDataTargetType.Self && spell.SData.TargettingType != SpellDataTargetType.SelfAndUnit)
                        {
                            RMenu.GetBool(enemy.ChampionName, "Spell" + spell.SData.Name, false);
                        }
                    }
                }
            }

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.GetBool("Q", "Q Range", false);
                DrawMenu.GetBool("W", "W Range", false);
                DrawMenu.GetBool("E", "E Range", false);
                DrawMenu.GetBool("R", "R Range", false);
            }

            var MiscMenu = Menu.Add(new Menu("Misc", "Draw"));
            {
                MiscMenu.GetSliderButton("ComboIgnite", "Combo Use Ignite Min Hp%", 10);
                MiscMenu.Add(new MenuBool("ClearEnable", "Clear Enable", true));
            }

            Game.OnUpdate += OnUpdate;
            Game.OnWndProc += OnWndProc;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!R.IsReady() || sender.IsMinion || !sender.IsEnemy || args.SData.ConsideredAsAutoAttack
                || !Menu["R"]["AutoR"].GetValue<MenuBool>() || !sender.IsValid || args.SData.Name.ToLower() == "tormentedsoil")
                return;

            if (Menu["R"]["Spell" + args.SData.Name] == null || !Menu["R"]["Spell" + args.SData.Name].GetValue<MenuBool>())
                return;

            if (args.Target != null)
            {
                if (args.Target.IsAlly)
                {
                    var ally = args.Target as Obj_AI_Hero;

                    if (ally != null && Menu["R"]["Rally" + ally.ChampionName].GetValue<MenuBool>())
                    {
                        R.CastOnUnit(ally);
                    }
                }
                else
                {
                    foreach (var ally in PlaySharp.Allies.Where(ally => ally.IsValid && !ally.IsDead && ally.HealthPercent < 70
                     && Player.ServerPosition.Distance(ally.ServerPosition) < R.Range && Menu["R"]["Rally" + ally.ChampionName].GetValue<MenuBool>()))
                    {
                        if (SebbyLib.OktwCommon.CanHitSkillShot(ally, args))
                        {
                            R.CastOnUnit(ally);
                        }
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (Menu["Draw"]["Q"].GetValue<MenuBool>() && Q.IsReady())
                {
                    Render.Circle.DrawCircle(Player.Position, Q.Range, Color.Cyan, 3);
                }

                if (Menu["Draw"]["W"].GetValue<MenuBool>() && W.IsReady())
                {
                    Render.Circle.DrawCircle(Player.Position, W.Range, Color.Cyan, 3);
                }

                if (Menu["Draw"]["E"].GetValue<MenuBool>() && E.IsReady())
                {
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.Cyan, 3);
                }

                if (Menu["Draw"]["R"].GetValue<MenuBool>() && R.IsReady())
                {
                    Render.Circle.DrawCircle(Player.Position, R.Range, Color.Cyan, 3);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In OnDraw" + ex);
            }
        }

        private static void OnWndProc(WndEventArgs args)
        {
            if (args.Msg == 0x20a)
            {
                Menu["Misc"]["ClearEnable"].GetValue<LeagueSharp.SDK.UI.MenuItem>().GetValue<MenuBool>();
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            try
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In OnUpdate" + ex);
            }
        }
    }
}