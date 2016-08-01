namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using PredictionOutput = LeagueSharp.SDK.PredictionOutput;

    using SharpDX;
    using System;
    using System.Linq;
    using System.Drawing;
    using System.Collections.Generic;
    using System.Windows.Forms;

    using Color = System.Drawing.Color;

    using Core;
    using Common;
    using static Common.Manager;
    using Config;

    using Menu = LeagueSharp.SDK.UI.Menu;
    using Geometry = Common.Geometry;

    internal static class Jinx
    {

        private static Spell Q, Q1, W, E, R;
        private static Menu Menu => PlaySharp.Menu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static bool BigGun => Player.HasBuff("JinxQ");
        private static bool usew = false;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        public static float DragonDmg = 0, lag = 0, LatFocusTime = Game.Time;
        public static double DragonTime = 0;
        private static int CastRTick = 0;
        private const float InitialSpeed = 1700, ChangerSpeedDistance = 1350, FinalSpeed = 2200;
        public static float AARange => GameObjects.Player.GetRealAutoAttackRange();

        internal static void Init()
        {

            Q = new Spell(SpellSlot.Q);
            Q1 = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 920f).SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 1490f).SetSkillshot(0.7f, 120f, 1750f, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 3000f).SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);


            var QMenu = Menu.Add(new Menu("Q", "Q.Set | Q 設定"));
            {
                QMenu.GetSeparator("Q: Always On");
                QMenu.GetBool("ComboQ", "Comno Q");
                QMenu.GetBool("HarassQ", "Harass Q");
                QMenu.GetBool("LaneClearQ", "LaneClear Q");
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("ComboW", "Comno W");
                WMenu.GetBool("HarassW", "Harass W", false);
                WMenu.GetBool("WKS", "W KillSteal");
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
                EMenu.GetBool("ProtectE", "Protect E");
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set | R設定"));
            {
                RMenu.GetSeparator("R: Mobe");
                RMenu.GetKeyBind("RKey", "Semi Manual Key", Keys.T, KeyBindType.Press);
                RMenu.GetSeparator("Jungle R Modes");
                RMenu.GetBool("Steal", "Auto Steal Jungle!");
                RMenu.GetSeparator("Auto R KillSteal");
                RMenu.GetBool("AutoR", "Auto R KillSteal", false);
                RMenu.GetSlider("Aoe", "Aoe R ", 2, 1, 5);
            }
            ModeBaseUlti.Init(Menu);

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.GetBool("Q", "Q Range", false);
                DrawMenu.GetBool("W", "W Range", false);
                DrawMenu.GetBool("E", "E Range", false);
                DrawMenu.GetBool("RDKs", "Draw R KS", false);
                DrawMenu.GetBool("EnableBuffs", "Draw Buff Enable");
                DrawMenu.GetList("DrawBuffs", "Show Red/Blue Time Circle", new[] { "Off", "Blue Buff", "Red Buff", "Both" });
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "Jinx OK! :)");

            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Events.OnGapCloser += OnGapCloser;
            Variables.Orbwalker.OnAction += OnAction;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnUpdate(EventArgs args)
        {
            try
            {
                Q1.Range = !BigGun ? AARange : 525f + Player.BoundingRadius;
                Q.Range = Q1.Range + (40f + 20f * Player.Spellbook.GetSpell(SpellSlot.Q).Level);

                if (Player.IsDead)
                    return;

                QLogic(args);

                WLogic(args);

                ELogic(args);

                AutoRLogic(args);

                JungleLogic(args);

                Drawggg(args);

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
                        < (Player.ServerPosition.Distance(Movement.GetPrediction(minion, 0.05f).CastPosition) + Player.BoundingRadius + minion.BoundingRadius) && (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                        < (Player.ServerPosition.Distance(Movement.GetPrediction(minion, 0.05f).CastPosition) + Player.BoundingRadius + minion.BoundingRadius)))
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
                    if (!BigGun && (!InAutoAttackRange(t) || t.CountEnemyHeroesInRange(250) > 2) && GetTarget(Q) == null)
                    {
                        var distance = Player.ServerPosition.Distance(Movement.GetPrediction(t, 0.05f).CastPosition) + Player.BoundingRadius + t.BoundingRadius;

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
                    var target = GetTarget(W.Range, DamageType.Magical);

                    if (target != null && target.IsHPBarRendered && target.DistanceToPlayer() >= 550 && target.IsValidTarget(W.Range))
                        W.Cast(target);
                }
                if (Harass && Menu["W"]["HarassW"].GetValue<MenuBool>())
                {
                    if (Player.ManaPercent < Menu["W"]["HarassWMana"].GetValue<MenuSlider>().Value)
                        return;

                    var target = GetTarget(W.Range, DamageType.Magical);

                    if (target != null && target.IsHPBarRendered && Menu["W"]["WList" + target.ChampionName].GetValue<MenuBool>().Value && target.DistanceToPlayer() >= 500 && target.IsValidTarget(W.Range))
                        W.Cast(target);
                }
                if (Menu["W"]["WKS"].GetValue<MenuBool>())
                {
                    foreach (var e in GetEnemies(W.Range))
                    {
                        if (W.GetDamage(e) > e.Health)
                            if (e.IsValidTarget(W.Range))
                                CastW(e);
                    }
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
                    var target = GetTarget(E.Range, DamageType.Physical);

                    if (target != null && target.IsHPBarRendered)
                    {
                        if (target.IsValidTarget(E.Range) &&
                            E.GetPrediction(target).CastPosition.Distance(target.Position) > 200 &&
                            E.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                        {
                            if (target.HasBuffOfType(BuffType.Slow) || CountEnemiesInRangeDeley(E.GetPrediction(target).CastPosition, 250, E.Delay) > 1)
                            {
                                E.Cast(target);
                            }
                            else
                            {
                                if (E.GetPrediction(target).CastPosition.Distance(target.Position) > 200)
                                {
                                    if (Player.Position.Distance(target.ServerPosition) > Player.Position.Distance(target.Position))
                                    {
                                        if (target.Position.Distance(Player.ServerPosition) < target.Position.Distance(Player.Position))
                                            E.Cast(target);
                                    }
                                    else
                                    {
                                        if (target.Position.Distance(Player.ServerPosition) > target.Position.Distance(Player.Position))
                                            E.Cast(target);
                                    }
                                }
                            }
                        }
                    }

                }
                foreach (var e in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.IsHPBarRendered))
                {
                    if (CheckTarget(e))
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

        private static void AutoRLogic(EventArgs args)
        {
            try
            {
                if (!R.IsReady())
                    return;

                if (Menu["R"]["AutoR"].GetValue<MenuBool>())
                {
                    bool cast = false;

                    foreach (var t in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget() && CanKill(e)))
                    {
                        float predictedHealth = t.Health + t.HPRegenRate * 2;
                        float PredictedHealth1 = t.HealthPercent * 10;
                        var Rdmg = R.GetDamage(t);
                        if (Rdmg > predictedHealth)
                        {
                            cast = true;

                            PredictionOutput output = R.GetPrediction(t);

                            Vector2 direction = output.CastPosition.ToVector2() - Player.Position.ToVector2();

                            direction.Normalize();

                            List<Obj_AI_Hero> enemies = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget()).ToList();

                            foreach (var enemy in enemies)
                            {
                                if (enemy.SkinName == t.SkinName || !cast)
                                    continue;

                                PredictionOutput prediction = R.GetPrediction(enemy);

                                Vector3 predictedPosition = prediction.CastPosition;
                                Vector3 v = output.CastPosition - GameObjects.Player.ServerPosition;
                                Vector3 w = predictedPosition - GameObjects.Player.ServerPosition;

                                double c1 = Vector3.Dot(w, v);
                                double c2 = Vector3.Dot(v, v);
                                double b = c1 / c2;

                                Vector3 pb = GameObjects.Player.ServerPosition + ((float)b * v);

                                float length = Vector3.Distance(predictedPosition, pb);


                                if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(t.ServerPosition))
                                    cast = false;

                                if (cast && (Player.ServerPosition.Distance(t.ServerPosition) + Player.BoundingRadius + t.BoundingRadius) > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                                    + 300 + t.BoundingRadius && t.CountAllyHeroesInRange(600) == 0 &&
                                    Player.CountEnemyHeroesInRange(500) == 0)
                                {
                                    List<Vector2> Way = t.GetWaypoints();
                                    if ((Player.Distance(Way.Last().ToVector3()) - Player.Distance(t.Position)) > 400)
                                        R.Cast(t);
                                }
                                else if (cast && t.CountEnemyHeroesInRange(200) > 2 && (Player.ServerPosition.Distance(t.ServerPosition)
                                    + Player.BoundingRadius + t.BoundingRadius) > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                                    + 200 + t.BoundingRadius)
                                {
                                    R.Cast(t);
                                }
                            }
                        }
                        if (Menu["R"]["RKey"].GetValue<MenuKeyBind>().Active)
                        {
                            if (t.IsValidTarget())
                            {
                                if (Rdmg > PredictedHealth1)
                                    R.Cast(t);
                            }
                        }
                        if (t.CountAllyHeroesInRange(1000) >= 1)
                        {
                            R.CastIfWillHit(t, Menu["R"]["Aoe"].GetValue<MenuSlider>().Value);
                        }
                        else
                        {
                            R.Cast(t);
                        }                         
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Auto R Logic " + ex);
            }
        }

        private static void JungleLogic(EventArgs args)
        {
            try
            {
                if (!R.IsReady())
                    return;

                if (Menu["R"]["Steal"].GetValue<MenuBool>())
                {
                    var mobs = GetMinions(Player.ServerPosition, R.Range);
                    foreach (var mob in mobs)
                    {
                        if (mob.Health < mob.MaxHealth && ((mob.SkinName == "SRU_Dragon"
                            || mob.SkinName == "SRU_Baron" || mob.SkinName == "SRU_Red"
                            || mob.SkinName == "SRU_Blue")) && mob.CountAllyHeroesInRange(2000)
                            == 0 && mob.Distance(Player.Position) > 2000)
                        {
                            if (DragonDmg == 0)
                                DragonDmg = mob.Health;
                            if (Game.Time - DragonTime > 4)
                            {
                                if (DragonDmg - mob.Health > 0)
                                {
                                    DragonDmg = mob.Health;
                                }
                                DragonTime = Game.Time;
                            }
                        }
                        else
                        {
                            var Sec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 4);

                            if (DragonDmg - mob.Health > 0)
                            {
                                var time = (int)(1000 * mob.GetUltimateTravelTime());
                                var health = mob.AllShield + mob.HPRegenRate * 2 - mob.GetPredictedDamage(time);
                                var damage = mob.GetUltimateDamage(health);
                                var heroNear = GameObjects.EnemyHeroes.Find(h => h.Distance(mob) >= 225f + mob.BoundingRadius);

                                if (health <= damage && heroNear != null)
                                    R.Cast(heroNear);
                            }
                            else
                            {
                                DragonDmg = mob.Health;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Jungle R Logic" + ex);
            }
        }

        private static void Drawggg(EventArgs args)
        {
            try
            {
                var drawBuffs = Menu["Draw"]["DrawBuffs"].GetValue<MenuList>().Index;

                if ((drawBuffs == 1 | drawBuffs == 3) && Player.HasBlueBuff())
                {
                    BuffInstance b = Player.Buffs.Find(buff =>
                    buff.DisplayName == "CrestoftheAncientGolem");
                    if (BlueBuff.EndTime < Game.Time || b.EndTime > BlueBuff.EndTime)
                    {
                        BlueBuff.StartTime = b.StartTime;
                        BlueBuff.EndTime = b.EndTime;
                    }
                }
                if ((drawBuffs == 2 | drawBuffs == 3) && Player.HasRedBuff())
                {
                    BuffInstance b = Player.Buffs.Find(buff =>
                    buff.DisplayName == "BlessingoftheLizardElder");
                    if (RedBuff.EndTime < Game.Time || b.EndTime > RedBuff.EndTime)
                    {
                        RedBuff.StartTime = b.StartTime;
                        RedBuff.EndTime = b.EndTime;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In On Darwggg" + ex);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (Q != null && Q.IsReady())
                {
                    if (Menu["Draw"]["Q"] != null && Menu["Draw"]["Q"].GetValue<MenuBool>().Value)
                    {
                        Render.Circle.DrawCircle(Player.Position, Q.Range, Color.LightGreen, 2);
                    }
                }

                if (W != null && W.IsReady() && Menu["Draw"]["W"] != null && Menu["Draw"]["W"].GetValue<MenuBool>().Value)
                {
                    Render.Circle.DrawCircle(Player.Position, W.Range, Color.Purple, 2);
                }

                if (E != null && E.IsReady() && Menu["Draw"]["E"] != null && Menu["Draw"]["E"].GetValue<MenuBool>().Value)
                {
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.Cyan, 2);
                }

                if (R != null && R.IsReady())
                {
                    if (Menu["Draw"]["R"] != null && Menu["Draw"]["R"].GetValue<MenuBool>().Value)
                    {
                        Render.Circle.DrawCircle(Player.Position, R.Range, Color.Red, 2);

                    }
                }

                if (Menu["Draw"]["RDKs"].GetValue<MenuBool>() && R.IsReady() && R.Level >= 1)
                {
                    var spos = Drawing.WorldToScreen(Player.Position);
                    var target = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.Health <= R.GetDamage(x) * 3
                    && !x.IsZombie && !x.IsDead);
                    int addpos = 0;
                    foreach (var killable in target)
                    {
                        Drawing.DrawText(spos.X - 50, spos.Y + 35 + addpos, Color.Red, killable.ChampionName + "Is Killable !!!");
                        addpos = addpos + 15;
                    }
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
            try
            {
                if (!Menu["Draw"]["EnableBuffs"].GetValue<MenuBool>())
                {
                    return;
                }

                var drawBuffs = Menu["Draw"]["DrawBuffs"].GetValue<MenuList>().Index;

                if ((drawBuffs == 1 | drawBuffs == 3) && Player.HasBlueBuff())
                {
                    if (BlueBuff.EndTime >= Game.Time)
                    {
                        var circle1 =
                            new Geometry.Circle2(
                                new Vector2(Player.Position.X + 3, Player.Position.Y - 3), 170f,
                                Game.Time - BlueBuff.StartTime, BlueBuff.EndTime - BlueBuff.StartTime).ToPolygon();
                        circle1.Draw(Color.Black, 4);

                        var circle =
                            new Geometry.Circle2(Player.Position.ToVector2(), 170f,
                                Game.Time - BlueBuff.StartTime, BlueBuff.EndTime - BlueBuff.StartTime).ToPolygon();
                        circle.Draw(Color.Blue, 4);
                    }
                }
                if ((drawBuffs == 2 || drawBuffs == 3) && Player.HasRedBuff())
                {
                    if (RedBuff.EndTime >= Game.Time)
                    {
                        var circle1 =
                            new Geometry.Circle2(
                                new Vector2(Player.Position.X + 3, Player.Position.Y - 3), 150f,
                                Game.Time - RedBuff.StartTime, RedBuff.EndTime - RedBuff.StartTime).ToPolygon();
                        circle1.Draw(Color.Black, 4);

                        var circle =
                            new Geometry.Circle2(Player.Position.ToVector2(), 150f,
                                Game.Time - RedBuff.StartTime, RedBuff.EndTime - RedBuff.StartTime).ToPolygon();
                        circle.Draw(Color.Red, 4);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in DrawBuffs" + ex);
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
                        var RealDistance = Player.ServerPosition.Distance(Movement.GetPrediction(t, 0.05f).CastPosition) + Player.BoundingRadius + t.BoundingRadius;
                        {
                            if (Combo && Menu["Q"]["ComboQ"].GetValue<MenuBool>().Value)
                            {
                                if (RealDistance < (650f + Player.BoundingRadius + t.BoundingRadius))
                                {
                                    if (Player.Mana < R.Instance.ManaCost + 20 || Player.GetAutoAttackDamage(t) * 3 < t.Health)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                            else if ((LaneClear || Harass) && Menu["Q"]["HarassQ"].GetValue<MenuBool>().Value)
                            {
                                if ((RealDistance > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level) || RealDistance < (650f + Player.BoundingRadius + t.BoundingRadius)
                                    || Player.Mana < R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost + W.Instance.ManaCost))
                                {
                                    Q.Cast();
                                }
                            }
                        }
                        if (LaneClear && !BigGun && Menu["Q"]["LaneClearQ"].GetValue<MenuBool>().Value)
                        {
                            if (Player.Mana > R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost + 30)
                            {
                                var minionQ = GetMinions(Player.Position, (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level));
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
                        if (!(Combo)) return;
                        if (args.Target is Obj_AI_Hero)
                        {
                            var newTarget = GetTarget(Q);
                            var forceFocusEnemy = newTarget;

                            foreach (var enemy in GameObjects.EnemyHeroes.Where(e =>
                            e.IsValidTarget(AARange)))
                            {
                                if (enemy.Health / Player.GetAutoAttackDamage(enemy) + 1 < forceFocusEnemy.Health
                                    / Player.GetAutoAttackDamage(forceFocusEnemy))
                                {
                                    forceFocusEnemy = enemy;
                                }
                            }
                            if (forceFocusEnemy.NetworkId != newTarget.NetworkId && Game.Time - LatFocusTime < 2)
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

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            if (!E.IsReady()) return;

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
                if (sender.IsMinion) return;
                if (!E.IsReady()) return;
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

        private static double GetUltimateDamage(this Obj_AI_Base t, float health)
        {
            var percentMod = Math.Min((int)(Player.Distance(t) / 100f) * 6f + 10f, 100f) / 100f;
            var level = Player.Spellbook.GetSpell(SpellSlot.R).Level;
            double rawDamage = 0.8f * percentMod *
                               (200f + 50f * level + Player.TotalAttackDamage +
                                Math.Min((0.25f + 0.05f * level) * (t.MaxHealth - health), 300f));
            return Player.CalculateDamage(t, DamageType.Physical, rawDamage);
        }

        private static float GetUltimateTravelTime(this Obj_AI_Base t)
        {
            var distance = Vector3.Distance(Player.ServerPosition, t.ServerPosition);
            if (distance >= ChangerSpeedDistance)
            {
                return ChangerSpeedDistance / InitialSpeed + (distance - ChangerSpeedDistance) / FinalSpeed + R.Delay / 1000f;
            }
            return distance / InitialSpeed + R.Delay / 1000f;
        }

        private static float GetPredictedDamage(this Obj_AI_Base t, int time)
        {
            if (!DamagesOnTime.ContainsKey(t.NetworkId))
            {
                return 0f;
            }
            return DamagesOnTime[t.NetworkId].Where(o => o.Time >
            Environment.TickCount - time && o.Time <= Environment.TickCount)
            .Sum(o => o.Damage);
        }

        private static void CastW(Obj_AI_Base t)
        {
            if (t.IsValidTarget(W.Range) && t.IsHPBarRendered && !usew)
            {
                W.Cast(t);
            }
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

        private static float GetDamage(Obj_AI_Base e)
        {

            var Damage = 0f;

            if (Player.HasBuff("SummonerExhaust"))
                Damage = Damage * 0.6f;

            if (e.HasBuff("FerociousHowl"))
                Damage = Damage * 0.7f;

            if (e is Obj_AI_Hero)
            {
                var champion = (Obj_AI_Hero)e;
                if (champion.ChampionName == "Blitzcrank" && !e.HasBuff("BlitzcrankManaBarrierCD") && !e.HasBuff("ManaBarrier"))
                {
                    Damage += champion.Mana / 2;
                }
            }
            return e.Health + e.PhysicalShield + e.HPRegenRate + Damage;
        }
    }
}