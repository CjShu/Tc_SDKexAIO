namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

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

    using Core;
    using Common;
    using static Common.Manager;
    using Config;

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
        public static readonly Dictionary<int, List<OnDamageEvent>> DamagesOnTime = new Dictionary<int, List<OnDamageEvent>>();
        private const float InitialSpeed = 1700;
        private const float ChangerSpeedDistance = 1350;
        private const float FinalSpeed = 2200;


        internal static void Init()
        {

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 920f).SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 1490f).SetSkillshot(0.7f, 120f, 1750f, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 4000f).SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);


            var QMenu = Menu.Add(new Menu("Q", "Q.Set | Q 設定"));
            {
                QMenu.GetSeparator("Q: Always On");
                QMenu.GetBool("ComboQ", "Comno Q");
                QMenu.GetBool("HarassQ", "Harass Q");
                QMenu.GetBool("LaneClearQ", "LaneClear Q");
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
                EMenu.GetSeparator("E: Gapcloser | Melee Modes");
                EMenu.GetBool("Gapcloser", "Gapcloser E", false);
                EMenu.GetSeparator("Auto E Set");
                EMenu.GetBool("SlowE", "Slow E", false);
                EMenu.GetBool("StunE", "Stun E", false);
                EMenu.GetBool("TelE", "Tel E", false);
                EMenu.GetBool("ImmeE", "Imm E", false);
                EMenu.GetBool("ProtectE", "Protect E", false);
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set | R設定"));
            {
                RMenu.GetSeparator("R: Mobe");
                RMenu.GetKeyBind("RKey", "Semi Manual Key", Keys.T, KeyBindType.Press);
                RMenu.GetList("AoeR", "Aoe R Min Hit Counts > =", new[] { "Aoe", "1 Enemy" });
                RMenu.GetSeparator("Jungle R Modes");
                RMenu.GetBool("Steal", "Auto Steal Jungle!", false);
                RMenu.GetSeparator("Auto R KillSteal");
                RMenu.GetBool("AutoR", "Auto R Enable");
            }
            ModeBaseUlti.Init(Menu);

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.GetBool("Q", "Q Range", false);
                DrawMenu.GetBool("W", "W Range", false);
                DrawMenu.GetBool("E", "E Range", false);
                DrawMenu.GetBool("EnableBuffs", "Draw Buff Enable");
                DrawMenu.GetList("DrawBuffs", "Show Red/Blue Time Circle", new[] { "Off", "Blue Buff", "Red Buff", "Both" });
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "Jinx OK! :)");

            Obj_AI_Base.OnBuffAdd += OnBuffAdd;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Events.OnGapCloser += OnGapCloser;
            AttackableUnit.OnDamage += OnDamage;
            GameObject.OnDelete += OnDelete;
            Variables.Orbwalker.OnAction += OnAction;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            try
            {
                var minion = sender as Obj_AI_Minion;
                if (minion != null && minion.IsEnemy && minion.MaxHealth >= 3500)
                {
                    if (DamagesOnTime.ContainsKey(minion.NetworkId))
                    {
                        DamagesOnTime.Remove(minion.NetworkId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In OnDelete" + ex);
            }
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            try
            {
                if (sender.IsEnemy)
                {
                    var minion = sender as Obj_AI_Minion;
                    if (minion != null && minion.MaxHealth >= 3500)
                    {
                        if (!DamagesOnTime.ContainsKey(minion.NetworkId))
                            DamagesOnTime[minion.NetworkId] = new List<OnDamageEvent>();
                        DamagesOnTime[minion.NetworkId].Add(new OnDamageEvent(Variables.TickCount, args.Damage));
                    }
                    var SourceMinion = sender as Obj_AI_Minion;
                    if (SourceMinion != null && SourceMinion.MaxHealth >= 3500)
                    {
                        if (!DamagesOnTime.ContainsKey(SourceMinion.NetworkId))
                        {
                            DamagesOnTime[SourceMinion.NetworkId] = new List<OnDamageEvent>();
                        }
                        DamagesOnTime[SourceMinion.NetworkId].Add(new OnDamageEvent(Variables.TickCount, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In OnDamage" + ex);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            try
            {
                if (Player.IsDead)
                    return;

                QLogic(args);

                WLogic(args);

                ELogic(args);

                RLogic(args);

                AutoRLogic(args);

                JungleRLogic(args);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in On Update " + ex);
            }
        }

        private static void QLogic(EventArgs args)
        {
            try
            {
                if ((Harass || LaneClear) && (Game.Time - lag > 0.1) && !BigGun && !Player.IsWindingUp && Variables.Orbwalker.CanAttack && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + E.Instance.ManaCost + 10
                    && Menu["Q"]["HarassQ"].GetValue<MenuBool>())
                {
                    foreach (var minion in GetMinions(Player.Position, Q.Range).Where(minion => !InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion) * 1.2 && (650f + Player.BoundingRadius + minion.BoundingRadius)
                        < (Player.ServerPosition.Distance(Prediction.GetPrediction(minion, 0.05f).CastPosition) + Player.BoundingRadius + minion.BoundingRadius) && (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                        < (Player.ServerPosition.Distance(Prediction.GetPrediction(minion, 0.05f).CastPosition) + Player.BoundingRadius + minion.BoundingRadius)))
                    {
                        Variables.Orbwalker.ForceTarget = minion;
                        Q.Cast();
                        return;
                    }
                    lag = Game.Time;
                }
                var t = GetTarget((670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level) + 60, DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (!BigGun && (!InAutoAttackRange(t) || t.CountEnemyHeroesInRange(250) > 2) && GetTarget(Q.Range) == null)
                    {
                        var distance = Player.ServerPosition.Distance(Prediction.GetPrediction(t, 0.05f).CastPosition) + Player.BoundingRadius + t.BoundingRadius;

                        if (Combo && (Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 10 || Player.GetAutoAttackDamage(t) * 3 > t.Health))
                        {
                            Q.Cast();
                        }
                        else if (Harass || Menu["Q"]["HarassQ"].GetValue<MenuBool>())
                        {
                            if (!Player.IsWindingUp)
                                if (Variables.Orbwalker.CanAttack)
                                    if (!Player.IsUnderEnemyTurret())
                                        if (Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + E.Instance.ManaCost + 20)
                                            if (distance < (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level) + t.BoundingRadius + Player.BoundingRadius)
                                                Q.Cast();
                        }
                    }
                }
                else if (!BigGun && Combo && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 20 && Player.CountEnemyHeroesInRange(2000) > 0)
                {
                    Q.Cast();
                }
                else if (BigGun && Combo && Player.Mana < R.Instance.ManaCost + W.Instance.ManaCost + 20)
                {
                    Q.Cast();
                }
                else if (BigGun && Combo && Player.CountEnemyHeroesInRange(2000) == 0)
                {
                    Q.Cast();
                }
                else if (BigGun && (Harass || LaneClear || LastHit))
                {
                    Q.Cast();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Q Logic " + ex);
            }
        }

        private static void WLogic(EventArgs args)
        {
            try
            {
                if (!W.IsReady())
                    return;

                if (Combo && Menu["W"]["ComboW"].GetValue<MenuBool>())
                {
                    var t = GetTarget(W.Range, DamageType.Physical);

                    if (t == null)
                        return;

                    float distance = Player.Position.Distance(t.Position);

                    if (distance >= 550)
                        if (t.IsValidTarget(W.Range))
                            SpellCast(W, t);
                }
                if ((Harass && Menu["W"]["HarassW"].GetValue<MenuBool>()) || Menu["W"]["AutoW"].GetValue<MenuKeyBind>().Active)
                {
                    if (Player.ManaPercent < Menu["W"]["HarassWMana"].GetValue<MenuSlider>().Value)
                        return;

                    if (Player.IsUnderEnemyTurret())
                        return;

                    var t = GetTarget(W.Range, DamageType.Physical);

                    if (t == null)
                        return;

                    float distance = Player.Position.Distance(t.Position);

                    if (distance >= 500)
                        if (Menu["W"]["WList" + t.ChampionName].GetValue<MenuBool>().Value)
                            if (t.IsValidTarget(W.Range))
                                if (W.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                                    W.Cast(t);
                }
                if (Menu["W"]["KSW"].GetValue<MenuBool>().Value)
                {
                    var e = GetTarget(W.Range, DamageType.Physical);

                    if (e.IsValidTarget() && e.Distance(Player.Position) > 500)
                        if (GetDamage(e, W) > e.Health)
                            if (CanKill(e))
                                if (Player.Position.Distance(e.Position) >= 600)
                                    SpellCast(W, e);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Auto W Logic " + ex);
            }
        }

        private static void ELogic(EventArgs args)
        {
            try
            {
                if (!E.IsReady())
                    return;

                if (Player.Mana < (E.Instance.ManaCost + R.Instance.ManaCost + W.Instance.ManaCost))
                    return;

                if (Combo && Menu["E"]["ComboE"].GetValue<MenuBool>())
                {
                    var t = GetTarget(E.Range, DamageType.Physical);

                    if (t.IsValidTarget(E.Range) && E.GetPrediction(t).CastPosition.Distance(t.Position)
                        > 200 && (int)E.GetPrediction(t).Hitchance == 5)
                    {
                        if (t.HasBuffOfType(BuffType.Slow) || CountEnemiesInRangeDeley(E.GetPrediction(t).CastPosition, 250, E.Delay) > 1)
                        {
                            SpellCast(E, t);
                        }
                        else
                        {
                            if (E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                            {
                                if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                                {
                                    if (t.Position.Distance(Player.ServerPosition) < t.Position.Distance(Player.Position))
                                        SpellCast(E, t);
                                }
                                else
                                {
                                    if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                                        SpellCast(E, t);
                                }
                            }
                        }
                    }
                }
                List<Obj_AI_Hero> Enemies = ObjectManager.Get<Obj_AI_Hero>().Where(
                    e => e.IsEnemy && e.IsValidTarget()).ToList();

                foreach (var e in Enemies)
                {
                    if (Menu["E"]["StunE"].GetValue<MenuBool>())
                    {
                        if (e.HasBuffOfType(BuffType.Stun))
                        {
                            if (e.IsValidTarget(E.Range))
                            {
                                if (E.GetPrediction(e).Hitchance >= HitChance.VeryHigh)
                                {
                                    E.Cast(e);
                                }
                            }
                        }
                    }
                    if (Menu["E"]["SlowE"].GetValue<MenuBool>())
                    {
                        if (e.HasBuffOfType(BuffType.Slow))
                        {
                            if (e.IsValidTarget(E.Range))
                            {
                                if (E.GetPrediction(e).Hitchance >= HitChance.VeryHigh)
                                {
                                    E.Cast(e);
                                }
                            }
                        }
                    }
                    if (Menu["E"]["ImmeE"].GetValue<MenuBool>())
                    {
                        if (!CanMove(e))
                        {
                            if (E.GetPrediction(e).Hitchance >= HitChance.VeryHigh)
                            {
                                E.Cast(e);
                            }
                        }
                        else
                        {
                            E.CastIfHitchanceEquals(e, HitChance.Immobile);
                        }
                    }
                }
                if (Menu["E"]["TelE"].GetValue<MenuBool>())
                {
                    foreach (var Obj in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.IsEnemy && Obj.Distance(Player.ServerPosition) < E.Range && (Obj.HasBuff("teleport_target") || Obj.HasBuff("Pantheon_GrandSkyfall_Jump"))))
                    {
                        E.Cast(Obj.Position);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in Auto E Logic " + e);
            }
        }

        private static void RLogic(EventArgs args)
        {
            try
            {
                if (!R.IsReady())
                    return;

                if (Menu["R"]["RKey"].GetValue<MenuKeyBind>().Active)
                {
                    var t = GetTarget(R.Range, DamageType.Physical);
                    if (t.IsValidTarget())
                    {
                        if (Menu["R"]["AoeR"].GetValue<MenuList>().Index == 0)
                        {
                            if (t.HealthPercent < 40)
                                R.Cast(t);
                        }
                        else
                        {
                            R.CastIfWillHit(t, 2);
                            R.Cast(t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Toggle R Logic " + ex);
            }
        }

        private static void AutoRLogic(EventArgs args)
        {
            try
            {
                if (!R.IsReady())
                    return;

                if (Menu["R"]["AutoR"].GetValue<MenuBool>())
                {
                    bool cast = false;

                    foreach (var t in GameObjects.EnemyHeroes.Where(t => t.IsValidTarget() && CanKill(t)))
                    {
                        float Health = t.Health + t.HPRegenRate * 2;

                        var Rdmg = R.GetDamage(t);

                        if (Rdmg > Health)
                        {
                            cast = true;

                            LeagueSharp.SDK.PredictionOutput output = R.GetPrediction(t);

                            Vector2 direction = output.CastPosition.ToVector2() - Player.Position.ToVector2();

                            direction.Normalize();

                            List<Obj_AI_Hero> enemies = GameObjects.EnemyHeroes.Where(x => x.IsEnemy && x.IsValidTarget()).ToList();

                            foreach (var enemy in enemies)
                            {
                                if (enemy.SkinName == t.SkinName || !cast)
                                    continue;

                                LeagueSharp.SDK.PredictionOutput prediction = R.GetPrediction(enemy);

                                Vector3 predictedPosition = prediction.CastPosition;
                                Vector3 v = output.CastPosition - Player.ServerPosition;
                                Vector3 w = predictedPosition - Player.ServerPosition;

                                double c1 = Vector3.Dot(w, v);
                                double c2 = Vector3.Dot(v, v);
                                double b = c1 / c2;

                                Vector3 pb = Player.ServerPosition + ((float)b * v);

                                float length = Vector3.Distance(predictedPosition, pb);

                                if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(t.ServerPosition))
                                    cast = false;
                            }

                            if (cast && (Player.ServerPosition.Distance(t.ServerPosition)
                                + Player.BoundingRadius + t.BoundingRadius)
                                > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                                + 300 + t.BoundingRadius && t.CountAllyHeroesInRange(600) == 0
                                && Player.CountEnemyHeroesInRange(400) == 0)
                            {
                                List<Vector2> waypoints = t.GetWaypoints();

                                if ((Player.Distance(waypoints.Last().ToVector3()) - Player.Distance(t.Position)) > 400)
                                    SpellCast(R, t);
                            }
                            else if (cast && t.CountEnemyHeroesInRange(200) > 2
                                && (Player.Position.Distance(t.Position) + Player.BoundingRadius + t.BoundingRadius)
                                > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level) + 200 + t.BoundingRadius)
                            {
                                R.Cast(t);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Auto R Logic " + ex);
            }
        }

        private static void JungleRLogic(EventArgs args)
        {

            try
            {
                if (R.IsReady() && Menu["R"]["Steal"].GetValue<MenuBool>())
                {
                    foreach (var mob in GetMobs(Player.Position, 10000))
                    {
                        if (mob != null && (mob.SkinName == "SRU_Dragon" || mob.SkinName == "SRU_Baron" || mob.SkinName == "SRU_Red" || mob.SkinName == "SRU_Blue") && Player.Distance(mob) >= W.Range)
                        {
                            var time = (int)(1000 * mob.GetUltimateTravelTime());
                            var health = mob.GetPredictedHealth(time);
                            var damage = mob.GetUltimateDamage(health);
                            var Enemy = GameObjects.EnemyHeroes.Find(e => e.Distance(mob) >= 225f + mob.BoundingRadius);
                            var Ally = GameObjects.AllyHeroes.Find(e => e.Distance(mob) < 800 + mob.BoundingRadius);

                            if (Enemy != null && health <= damage && Ally == null)
                            {
                                R.Cast(mob);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Jungle R Logic " + ex);
            }
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
                DrawBuffs(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in On Draw" + ex);
            }
        }

        private static void DrawBuffs(EventArgs args)
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
                        var RealDistance = Player.ServerPosition.Distance(Prediction.GetPrediction(t, 0.05f).CastPosition) + Player.BoundingRadius + t.BoundingRadius;
                        {
                            if (Combo && Menu["Q"]["ComboQ"].GetValue<MenuBool>())
                            {
                                if (RealDistance < (650f + Player.BoundingRadius + t.BoundingRadius))
                                {
                                    if (Player.Mana < R.Instance.ManaCost + 20 || Player.GetAutoAttackDamage(t) * 3 < t.Health)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                            else if ((LaneClear || Harass) && Menu["Q"]["HarassQ"].GetValue<MenuBool>())
                            {
                                if ((RealDistance > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level) || RealDistance < (650f + Player.BoundingRadius + t.BoundingRadius)
                                    || Player.Mana < R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost + W.Instance.ManaCost))
                                {
                                    Q.Cast();
                                }
                            }
                        }
                        if (LaneClear && !BigGun && Menu["Q"]["LaneClearQ"].GetValue<MenuBool>())
                        {
                            if (Player.Mana > R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost + 30)
                            {
                                var minionQ = GetMinions(Player.ServerPosition, (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level));
                                foreach (var minion in minionQ.Where(minion => args.Target.NetworkId != minion.NetworkId
                                && minion.Distance(args.Target.Position) < 200
                                && (5 - Q.Level)
                                * Player.GetAutoAttackDamage(minion) < args.Target.Health
                                && (5 - Q.Level)
                                * Player.GetAutoAttackDamage(minion) < minion.Health))
                                {
                                    Q.Cast();
                                }
                            }
                        }
                        if (Combo)
                        {
                            if (args.Target is Obj_AI_Hero)
                            {
                                var newTarget = (Obj_AI_Hero)args.Target;
                                var forceFocusEnemy = newTarget;

                                var aaRange = Player.AttackRange + Player.BoundingRadius + 350;

                                foreach (var enemy in GetEnemies(Q.Range).Where(enemy => enemy.IsValidTarget(aaRange)))
                                {
                                    if (enemy.Health / Player.GetAutoAttackDamage(enemy) + 1 < forceFocusEnemy.Health / Player.GetAutoAttackDamage(forceFocusEnemy))
                                    {
                                        forceFocusEnemy = enemy;
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
            if (!E.IsReady())
                return;

            if (Menu["E"]["Gapcloser"].GetValue<MenuBool>())
            {
                if (args.Sender.IsValidTarget(E.Range))
                {
                    if (E.GetPrediction(args.Sender).Hitchance >= HitChance.VeryHigh)
                    {
                        E.Cast(args.Sender);
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

            try
            {
                if (sender.IsMinion && !E.IsReady())
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
                           
        private static double GetUltimateDamage(this Obj_AI_Base mob, float health)
        {
            var percentMod = Math.Min((int)(Player.Distance(mob) / 100f) * 6f + 10f, 100f) / 100f;
            var level = Player.Spellbook.GetSpell(SpellSlot.R).Level;
            double rawDamage = 0.8f * percentMod *
                (200f + 50f * level + Player.TotalAttackDamage +
                Math.Min((0.25f + 0.05f * level) * (mob.MaxHealth - health), 300f));
            return Player.CalculateDamage(mob, DamageType.Physical, rawDamage);
        }

        private static float GetUltimateTravelTime(this Obj_AI_Base mob)
        {
            var distance = Vector3.Distance(Player.ServerPosition, mob.ServerPosition);
            if (distance >= ChangerSpeedDistance)
            {
                return ChangerSpeedDistance / InitialSpeed + (distance - ChangerSpeedDistance) / FinalSpeed +
                    R.Delay / 1000f;
            }
            return distance / InitialSpeed + R.Delay / 1000f;
        }

        private static float GetPredictedDamage(this Obj_AI_Base monster, int time)
        {
            if (!DamagesOnTime.ContainsKey(monster.NetworkId))
            {
                return 0f;
            }
            return
                DamagesOnTime[monster.NetworkId].Where(
                    onDamage => onDamage.Time > Variables.TickCount - time && onDamage.Time <= Variables.TickCount)
                    .Sum(onDamage => onDamage.Damage);
        }

        private static float GetPredictedHealth(this Obj_AI_Base mob, int time)
        {
            return mob.AllShield + mob.HPRegenRate * 2 - mob.GetPredictedDamage(time);
        }

        private static int CountEnemiesInRangeDeley(Vector3 position, float range, float delay)
        {
            int count = 0;

            foreach (var t in GameObjects.EnemyHeroes.Where(t => t.IsValidTarget()))
            {
                Vector3 prepos = Prediction.GetPrediction(t, delay).CastPosition;

                if (position.Distance(prepos) < range)
                    count++;
            }

            return count;
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

        private static float GetDamage(Obj_AI_Base t, Spell spell)
        {

            var Damage = spell.GetDamage(t);

            if (Player.HasBuff("SummonerExhaust"))
                Damage = Damage * 0.6f;

            if (t.HasBuff("FerociousHowl"))
                Damage = Damage * 0.7f;

            if (t is Obj_AI_Hero)
            {
                var champion = (Obj_AI_Hero)t;
                if (champion.ChampionName == "Blitzcrank" && !t.HasBuff("BlitzcrankManaBarrierCD") && !t.HasBuff("ManaBarrier"))
                {
                    Damage -= champion.Mana / 2f;
                }
            }
            var Hp = t.Health - SebbyLib.HealthPrediction.GetHealthPrediction(t, 500);

            Damage += Hp;
            Damage -= t.HPRegenRate;
            Damage -= t.PercentLifeStealMod * 0.005f * t.FlatPhysicalDamageMod;

            return Damage;
        }
    }
}