namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.Data.Enumerations;

    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using SharpDX;
    using System;
    using System.Linq;
    using System.Drawing;
    using System.Collections.Generic;
    using System.Windows.Forms;

    using Color = System.Drawing.Color;
    using Font = SharpDX.Direct3D9.Font;

    using Common;
    using static Common.Manager;
    using Config;


    using Utility = LeagueSharp.Common.Utility;
    using Menu = LeagueSharp.SDK.UI.Menu;
    using Geometry = Common.Geometry;

    internal static class Jinx
    {

        private static Spell Q, Q2, W, E, R;
        private static Menu Menu => PlaySharp.Menu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static bool BigGun => Player.HasBuff("JinxQ");
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static float DrawSpellTime = 0, DragonDmg = 0, lag = 0, LatFocusTime = Game.Time;

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q);
            Q2 = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 920f).SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 1490f).SetSkillshot(0.7f, 120f, 1750f, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 4000f).SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);
            R.DamageType = DamageType.Physical;
            R.MinHitChance = HitChance.VeryHigh;


            var QMenu = Menu.Add(new Menu("Q", "Q.Set | Q 設定"));
            {
                QMenu.GetSeparator("Q: Always On");
                QMenu.GetBool("ComboQ", "Comno Q");
                QMenu.GetBool("HarassQ", "Harass Q");
                QMenu.GetBool("LaneClearQ", "LaneClear Q");
                QMenu.GetSlider("HarassQMana", "Harass Q  Min Mana > =", 40, 0, 99);
                QMenu.GetSlider("LaneClearQMana", "LaneClearQ Min Mana > =", 50, 0, 99);
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("ComboW", "ComnoW");
                WMenu.GetBool("KSW", "Killsteal W");
                WMenu.GetKeyBind("AutoW", "Auto W", Keys.T, KeyBindType.Press);
                WMenu.GetBool("HarassW", "Harass W", false);
                WMenu.GetSlider("HarassWMana", "Harass W Min Mana > =", 40, 0, 70);
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
                EMenu.GetSliderButton("AoeE", "Aoe E Min Hit Counts > =", 2, 1, 5);
                EMenu.GetSeparator("E: Gapcloser | Melee Modes");
                EMenu.GetBool("Gapcloser", "Gapcloser E", false);
                EMenu.GetSeparator("Auto E Set");
                EMenu.GetBool("SlowE", "Slow E", false);
                EMenu.GetBool("StunE", "Slow E", false);
                EMenu.GetBool("TelE", "Slow E", false);
                EMenu.GetBool("ImmE", "Slow E", false);
                EMenu.GetBool("DashE", "Dash E", false);
                EMenu.GetBool("ProtectE", "Protect E", false);
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set | R設定"));
            {
                RMenu.GetSeparator("R: Mobe");
                RMenu.GetSliderButton("AoeR", "Aoe R Min Hit Counts > =", 2, 1, 5);
                RMenu.GetSlider("Raoe", "Max Range R Aoe", 4000, 0, 15000);
                RMenu.GetKeyBind("RKey", "Semi Manual Key", Keys.T, KeyBindType.Press);
                RMenu.GetSeparator("Jungle R Modes");
                RMenu.GetBool("DragonR", "Dragon R", false);
                RMenu.GetBool("BaronR", "Baron R", false);
                RMenu.GetBool("BlueR", "Blue R", false);
                RMenu.GetBool("RedR", "Red R", false);
                RMenu.GetSeparator("Auto R KillSteal");
                RMenu.GetBool("AutoR", "Auto R Enable");
                var RList = RMenu.Add(new Menu("RList", "Auto R List"));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => RList.GetBool(i.ChampionName.ToLower(), i.ChampionName, PlaySharp.AutoEnableList.Contains(i.ChampionName)));
                    }
                }
            }
            ModeBaseUlti.Init(Menu);

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.GetBool("Q", "Q Range", false);
                DrawMenu.GetBool("W", "W Range", false);
                DrawMenu.GetBool("E", "E Range", false);
                DrawMenu.GetBool("EnableBuffs", "Draw Buff Enable", false);
                DrawMenu.GetList("DrawBuffs", "Show Red/Blue Time Circle", new[] { "Off", "Blue Buff", "Red Buff", "Both" });
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "Jinx OK! :)");

            Obj_AI_Base.OnBuffAdd += OnBuffAdd;
            Variables.Orbwalker.OnAction += OnAction;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (Player.IsDead)
                    return;

                if (Q.IsReady() && Menu["Draw"]["Q"].GetValue<MenuBool>())
                {
                    Render.Circle.DrawCircle(Player.Position, Q.Range, Color.Cyan);
                }
                if (W.IsReady() && Menu["Draw"]["W"].GetValue<MenuBool>())
                {
                    Render.Circle.DrawCircle(Player.Position, W.Range, Color.CadetBlue);
                }
                if (E.IsReady() && Menu["Draw"]["E"].GetValue<MenuBool>())
                {
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.CornflowerBlue);
                }
                DrawBuffs();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in On Draw" + ex);
            }
        }

        private static void DrawBuffs()
        {

            var DrawBuff = Menu["Draw"]["DrawBuffs"].GetValue<MenuList>().Index;

            if (!Menu["Draw"]["EnableBuffs"].GetValue<MenuBool>())
            {
                return;
            }
            if ((DrawBuff == 1 | DrawBuff == 3) && Player.HasBlueBuff())
            {
                if (BlueBuff.EndTime >= Game.Time)
                {
                    var circle1 =
                        new Geometry.Circle2(
                            new Vector2(ObjectManager.Player.Position.X + 3, ObjectManager.Player.Position.Y - 3), 170f,
                            Game.Time - BlueBuff.StartTime, BlueBuff.EndTime - BlueBuff.StartTime).ToPolygon();
                    circle1.Draw(Color.Black, 4);

                    var circle =
                        new Geometry.Circle2(ObjectManager.Player.Position.ToVector2(), 170f,
                            Game.Time - BlueBuff.StartTime, BlueBuff.EndTime - BlueBuff.StartTime).ToPolygon();
                    circle.Draw(Color.Blue, 4);
                }
            }
            if ((DrawBuff == 2 || DrawBuff == 3) && ObjectManager.Player.HasRedBuff())
            {
                if (RedBuff.EndTime >= Game.Time)
                {
                    var circle1 =
                        new Geometry.Circle2(
                            new Vector2(ObjectManager.Player.Position.X + 3, ObjectManager.Player.Position.Y - 3), 150f,
                            Game.Time - RedBuff.StartTime, RedBuff.EndTime - RedBuff.StartTime).ToPolygon();
                    circle1.Draw(Color.Black, 4);

                    var circle =
                        new Geometry.Circle2(ObjectManager.Player.Position.ToVector2(), 150f,
                            Game.Time - RedBuff.StartTime, RedBuff.EndTime - RedBuff.StartTime).ToPolygon();
                    circle.Draw(Color.Red, 4);
                }
            }
        }

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            try
            {
                if (args.Type == OrbwalkingType.BeforeAttack)
                {
                    if (!Q.IsReady())
                        return;

                    if (!(args.Target is Obj_AI_Hero))
                        return;

                    var t = (Obj_AI_Hero)args.Target;

                    if (BigGun && t.IsValidTarget())
                    {
                        var RealDistance = Player.Position.Distance(Movement.GetPrediction(t, 0.05f).CastPosition) + Player.BoundingRadius + t.BoundingRadius;
                        {
                            if (Combo && Menu["Q"]["Combo"].GetValue<MenuBool>())
                            {
                                if (RealDistance < (650f + Player.BoundingRadius + t.BoundingRadius))
                                {
                                    if (Player.Mana < R.Instance.ManaCost + 20 || Player.GetAutoAttackDamage(t) * 3 < t.Health)
                                    {
                                        Q.Cast(t);
                                    }
                                }
                            }
                            else if ((LaneClear || Harass) && Menu["Q"]["HarassQ"].GetValue<MenuBool>())
                            {
                                if ((RealDistance > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level) || RealDistance < (650f + Player.BoundingRadius + t.BoundingRadius)
                                    || Player.Mana < R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost + W.Instance.ManaCost))
                                {
                                    Q.Cast(t);
                                }
                            }
                        }
                        if (LaneClear && !BigGun && Menu["Q"]["LaneClearQ"].GetValue<MenuBool>())
                        {
                            if (Player.Mana > R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost + 30)
                            {
                                var minionQ = GetMinions(Player.Position, (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level));
                                foreach (var minion in minionQ.Where(minion => args.Target.NetworkId != minion.NetworkId
                                && minion.Distance(args.Target.Position) < 200 && (5 - Q.Level)
                                * Player.GetAutoAttackDamage(minion) < args.Target.Health && (5 - Q.Level)
                                * Player.GetAutoAttackDamage(minion) < minion.Health))
                                {
                                    Q.Cast(minion);
                                }
                            }
                        }
                        if (args.Target is Obj_AI_Hero)
                        {
                            var forceFocusEnemy = t;
                            var aaRange = Player.AttackRange + Player.BoundingRadius + 350;

                            foreach (var enemy in GetEnemies(Q.Range).Where(enemy => enemy.IsValidTarget(aaRange)))
                            {
                                if (enemy.Health / Player.GetAutoAttackDamage(enemy) + 1 < forceFocusEnemy.Health / Player.GetAutoAttackDamage(forceFocusEnemy))
                                {
                                    forceFocusEnemy = enemy;
                                    Q.Cast(enemy);
                                }
                            }
                            if (forceFocusEnemy.NetworkId != t.NetworkId && Game.Time - LatFocusTime < 2)
                            {
                                args.Process = false;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Before Attack Events " + ex);
            }
        }

        private static void OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
           try
            {
                if (!E.IsReady())
                {
                    return;
                }

                BuffInstance aBuff = (from fBuffs in sender.Buffs.Where(s =>
                sender.Team != Player.Team && sender.Distance(Player.Position) < E.Range)
                from b in new[] { "teleport_", /* Telepor */
                          "pantheon_grandskyfall_jump", /* Pantheon */ 
                          "crowstorm", /* FiddleScitck */
                          "zhonya", "katarinar", /* Katarita */
                           "MissFortuneBulletTime", /* MissFortune */
                           "gate", /* Twisted Fate */
                           "chronorevive" /* Zilean */
                }
                where args.Buff.Name.ToLower().Contains(b)
                select fBuffs).FirstOrDefault();

                if (aBuff != null)
                {
                    E.Cast(sender.Position);
                }
             }
            catch (Exception ex)
            {
                Console.WriteLine("Error in On BuffAdd" + ex);             
            }         
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            if (E.IsReady() && args.Sender.IsValidTarget(E.Range) && !Invulnerable.Check(
               args.Sender, DamageType.Magical, false) && Menu["E"]["Gapcloser"].GetValue<MenuBool>().Value)
            {
                E.Cast(args.IsDirectedToPlayer ? GameObjects.Player.ServerPosition : args.End);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            try
            {
                if (sender.IsMinion)
                    return;

                if (!E.IsReady())
                    return;

                if (sender.IsEnemy)
                {
                    if (Menu["E"]["ProtectE"].GetValue<MenuBool>())
                    {
                        if (sender.IsValidTarget(E.Range))
                        {
                            if (ShouldUseE(args.SData.Name))
                            {
                                E.Cast(sender.ServerPosition);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Process Spell Cast" + ex);
            }
        }
                           
        private static float GetSlowEndTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Slow)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }

        public static void CastQObjects(Obj_AI_Base t)
        {
            if (!Q.CanCast(t))
            {
                return;
            }

            Q.CastOnUnit(t);
        }

        private static bool ShouldUseE(string SpellName)
        {
            switch (SpellName)
            {
                case "ThreshQ":
                    return true;
                case "KatarinaR":
                    return true;
                case "AlZaharNetherGrasp":
                    return true;
                case "GalioIdolOfDurand":
                    return true;
                case "LuxMaliceCannon":
                    return true;
                case "MissFortuneBulletTime":
                    return true;
                case "RocketGrabMissile":
                    return true;
                case "CaitlynPiltoverPeacemaker":
                    return true;
                case "EzrealTrueshotBarrage":
                    return true;
                case "InfiniteDuress":
                    return true;
                case "VelkozR":
                    return true;
            }
            return false;
        }

        private static float GetDamage(Obj_AI_Hero t, Spell spell)
        {

            var Damage = spell.GetDamage(t);

            Damage -= t.HPRegenRate;

            if (Q.IsReady())
            {
                Damage += Q.GetDamage(t);
            }

            if (W.IsReady())
            {
                Damage += W.GetDamage(t);
            }

            if (E.IsReady())
            {
                Damage += E.GetDamage(t);
            }

            if (R.IsReady())
            {
                Damage += R.GetDamage(t);
            }

            if (Damage > t.Health)
            {
                if (Player.HasBuff("SummonerExhaust")) Damage = Damage * 0.6f;
                if (t.HasBuff("FerociousHowl")) Damage = Damage * 0.7f;
                if (t.ChampionName == "Blitzcrank" && !t.HasBuff("BlitzcrankManaBarrierCD")) Damage -= t.Mana / 2f;
                if (t.ChampionName == "Moredkaiser") Damage -= t.Mana;               
            }
            return Damage;
        }
    }
}