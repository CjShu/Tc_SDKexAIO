namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.UI;

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Windows.Forms;

    using SharpDX;

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
        private static Vector3 PosCastR = Vector3.Zero;
        public static bool IsCastingR => R.Instance.Name == "JhinRShot";
        private static bool Ractive = false;
        private static Spell Q, W, E, R;

        internal static void Init()
        {

            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W, 2500).SetSkillshot(0.75f, 40, float.MaxValue, false, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 750).SetSkillshot(0.5f, 120, 1600, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 3500).SetSkillshot(0.21f, 80, 5000, false, SkillshotType.SkillshotLine);


            var QMenu = Menu.Add(new Menu("Q", "Q.Set | Q 設定"));
            {
                QMenu.GetSeparator("Q: Always On");
                QMenu.GetBool("ComboQ", "Comno Q");
                QMenu.GetBool("HarassQ", "Harass Q");
                QMenu.GetBool("LaneClearQ", "LaneClear Q");
                QMenu.GetBool("JungleQ", "Jungle Q");
                QMenu.GetBool("KillStealQ", "KillSteal Q", false);
                QMenu.GetBool("QMinion", "Use Q Minion Harass Enemy", false);
                QMenu.GetSlider("QMinion", "How much Minion to Use Q Blow Enemy", 2, 3, 4);
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("ComboW", "ComnoW");
                WMenu.GetBool("KSW", "Killsteal W");
                WMenu.GetBool("HarassW", "Harass W", false);
                WMenu.GetBool("LaneClearW", "LaneClear W", false);
                WMenu.GetBool("StunW", "Stun W", false);
                WMenu.GetKeyBind("WTap", "W Fire On Tap", Keys.G, KeyBindType.Press);
                WMenu.Add(new MenuKeyBind("AutoW", "Use W Auto (Toggle)", Keys.Y, KeyBindType.Toggle));
                WMenu.GetSlider("HarassWMana", "Harass W Min Mana > =", 60);
                var WList = WMenu.Add(new Menu("WList", "HarassW List:"));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => WList.GetBool(i.ChampionName.ToLower(), i.ChampionName, PlaySharp.AutoEnableList.Contains(i.ChampionName)));
                    }
                }
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set | E 設定"));
            {
                EMenu.GetSeparator("E: Mobe");
                EMenu.GetBool("ComboE", "Combo E");
                EMenu.GetBool("LaneClearE", "LaneClear E", false);
                EMenu.GetBool("JungleE", "Jungle E", false);
                EMenu.GetSeparator("E: Gapcloser | Melee Modes");
                EMenu.GetBool("Gapcloser", "Gapcloser E", false);
                EMenu.GetSeparator("Auto E Set");
                EMenu.GetBool("AutoE", "Auto E", false);
                EMenu.GetKeyBind("ETap", "Force E", Keys.H, KeyBindType.Press);
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set | R設定"));
            {
                RMenu.GetSeparator("Auto R KillSteal");
                RMenu.GetKeyBind("RTap", "R Fire On Tap", Keys.T, KeyBindType.Press);
                RMenu.GetBool("Ping", "Ping Who Can Killable(Every 3 Seconds)", true);
                RMenu.GetBool("Rvisable", "enemy is not visable Not R", false);
                EMenu.GetBool("AutoR", "Enable R Auto");
            }

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.GetBool("Q", "Q Range", false);
                DrawMenu.GetBool("W", "W Range", false);
                DrawMenu.GetBool("E", "E Range", false);
                DrawMenu.GetBool("R", "R Range", false);
                DrawMenu.GetBool("RDKs", "Draw Who Can Killable With R (3 Fire)", false);
                DrawMenu.GetBool("RDind", "Draw R Damage Indicator (3 Fire)", false);
            }

            Menu.GetBool("ComboY", "ComboY", false);
        
            PlaySharp.Write(GameObjects.Player.ChampionName + "Jhin OK! :)");


            Obj_AI_Base.OnDoCast += OnDoCast;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Events.OnGapCloser += OnGapCloser;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            Game.OnUpdate += OnUpdate;

        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            try
            {
                if (sender.IsMe && args.SData.Name == "JhinR")
                {
                    PosCastR = args.End;
                }

                if (!sender.IsMe && !AutoAttack.IsAutoAttack(args.SData.Name) || !args.Target.IsEnemy
                    || !args.Target.IsValid || !(args.Target is Obj_AI_Hero)) return;
                if (Combo && Menu["ComboY"].GetValue<MenuBool>().Value)
                {
                    CastYoumoo();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In On ProcessSpellCast" + ex);
            }
        }

        private static void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {

                if (!sender.IsMe || !AutoAttack.IsAutoAttack(args.SData.Name)) return;

                if (Combo)
                {
                    if (args.Target is Obj_AI_Hero)
                    {
                        var target = (Obj_AI_Hero)args.Target;
                        if (!target.IsDead)
                        {
                            if (Menu["W"]["ComboW"].GetValue<MenuBool>() && W.IsReady())
                            {
                                W.Cast(W.GetPrediction(target).UnitPosition);
                                return;
                            }
                            if (Q.IsReady() && Menu["Q"]["ComboQ"].GetValue<MenuBool>() && Player.Distance(target) <= 550)
                            {
                                Q.Cast(target);
                            }
                        }
                    }
                }
                if (Harass)
                {
                    if (args.Target is Obj_AI_Hero)
                    {
                        var target = (Obj_AI_Hero)args.Target;
                        if (!target.IsDead)
                        {
                            if (Menu["W"]["HarassW"].GetValue<MenuBool>() && W.IsReady())
                            {
                                W.Cast(W.GetPrediction(target).UnitPosition);
                                return;
                            }
                            if (Q.IsReady() && Menu["Q"]["HarassQ"].GetValue<MenuBool>() && Player.Distance(target) <= 550)
                            {
                                Q.Cast(target);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In On DoCast" + ex);
            }
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            try
            {
                if (E.IsReady() && !Invulnerable.Check(args.Sender) && args.Sender.IsValidTarget(E.Range))
                {
                    if (Menu["E"]["Gapcloser"].GetValue<MenuBool>().Value)
                    {
                        E.Cast(args.End);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In On GapCloser" + ex);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }

        static void CastYoumoo()
        {
            if (Items.CanUseItem(3142))
                Items.UseItem(3142);
        }

        private static void OnEndScene(EventArgs args)
        {
            try
            {

                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget() && x.IsEnemy))
                {
                    if (Menu["Draw"]["RDind"].GetValue<MenuBool>() && R.Level >= 1)
                    {
                        HpBarDraw.Unit = enemy;
                        HpBarDraw.DrawDmg(R.GetDamage(enemy) * 3, new ColorBGRA(0, 100, 200, 150));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In On EndScene" + ex);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (Menu["Draw"]["RDKs"].GetValue<MenuBool>() && R.IsReady() && R.Level >= 1)
                {
                    var spos = Drawing.WorldToScreen(Player.Position);
                    var target = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.Health <= R.GetDamage(x) * 3
                    && !x.IsZombie && !x.IsDead);
                    int addpos = 0;
                    foreach (var killable in target)
                    {
                        Drawing.DrawText(spos.X - 50, spos.Y + 35 + addpos, System.Drawing.Color.Red, killable.ChampionName + "Is Killable !!!");
                        addpos = addpos + 15;
                    }
                }
                if (Menu["Draw"]["R"].GetValue<MenuBool>() && R.Level >= 1)
                {
                    Drawing.DrawCircle(Player.Position, 3500, R.IsReady() ? System.Drawing.Color.Cyan : System.Drawing.Color.DarkRed);
                }
                if (Menu["Draw"]["W"].GetValue<MenuBool>() && W.Level >= 1)
                {
                    Drawing.DrawCircle(Player.Position, 2500, W.IsReady() ? System.Drawing.Color.Cyan : System.Drawing.Color.DarkRed);
                }
                if (Menu["Draw"]["E"].GetValue<MenuBool>() && E.Level >= 1)
                {
                    Drawing.DrawCircle(Player.Position, 750, E.IsReady() ? System.Drawing.Color.Cyan : System.Drawing.Color.DarkRed);
                }
                if (Menu["Draw"]["Q"].GetValue<MenuBool>() && Q.Level >= 1)
                {
                    Drawing.DrawCircle(Player.Position, 550 + Player.BoundingRadius, Q.IsReady() ? System.Drawing.Color.Cyan : System.Drawing.Color.DarkRed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In On Draw" + ex);
            }
        }          
    }
}
