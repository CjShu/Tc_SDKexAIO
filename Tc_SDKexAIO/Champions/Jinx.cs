namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.Data.Utility;
    using LeagueSharp.Data.Enumerations;

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
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        public static float DrawSpellTime = 0, DragonDmg = 0, lag = 0, LatFocusTime = Game.Time;
        public static double DragonTime = 0;
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
                WMenu.GetBool("AutoW", "Auto W", false);
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
                EMenu.GetBool("ProtectE", "Protect E");
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set | R設定"));
            {
                RMenu.GetSeparator("R: Mobe");
                RMenu.GetKeyBind("RKey", "Semi Manual Key (hp 15%)", Keys.T, KeyBindType.Press);
                RMenu.GetKeyBind("AoeRkey", "Aoe R Key(2)", Keys.R, KeyBindType.Press);
                //RMenu.GetList("AoeR", "Aoe R Min Hit Counts > =", new[] { "1 Enemy", "Aoe" });
                //RMenu.GetSliderButton("AoeR", "Aoe R Min Hit Counts > =", 2, 1, 5);
                RMenu.GetSeparator("Jungle R Modes");
                RMenu.GetBool("Steal", "Auto Steal Jungle!");
                RMenu.GetSeparator("Auto R KillSteal");
                RMenu.GetBool("AutoR", "Auto R KillSteal", false);
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

                RLogic(args);

                AutoRLogic(args);

                JungleLogic(args);

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
                        Q.Cast(minion);
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
                if (Menu["W"]["AutoW"].GetValue<MenuBool>().Value)
                {
                    var e = GetTarget(W.Range, DamageType.Magical);
                    if (e.IsValidTarget() && e.HasBuffOfType(BuffType.Slow) && e.HasBuffOfType(BuffType.Stun)
                        && CanKill(e) && e.DistanceToPlayer() >= 570 && e.IsValidTarget(W.Range))
                        W.Cast(e);                       
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

        private static void RLogic(EventArgs args)
        {
            try
            {
                if (!R.IsReady())
                    return;

                if (Menu["R"]["RKey"].GetValue<MenuKeyBind>().Active)
                {
                    var t = GetTarget(R.Range, DamageType.Physical);
                    if (t.IsValidTarget() && t.HealthPercent < 15)
                    {
                        R.Cast(t);
                    }
                }
                if (Menu["R"]["AoeRkey"].GetValue<MenuKeyBind>().Active)
                {
                    var t = GetTarget(R.Range, DamageType.Physical);
                    if (t.IsValidTarget())
                    {
                        R.CastIfWillHit(t, 2);
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

                if (Menu["R"]["AutoR"].GetValue<MenuBool>().Value)
                {
                    if (Q.IsReady())
                    {
                        foreach (var target in GameObjects.EnemyHeroes.Where(e
                            => !Invulnerable.Check(e) && e.IsValidTarget(R.Range)
                            && GetDamage(e) < (float)Player.GetSpellDamage(e, SpellSlot.R)))
                        {
                            if (target.HealthPercent < 10)
                            {
                                R.Cast(target);
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
                            || mob.SkinName == "SRU_Baron")) && mob.CountAllyHeroesInRange(1000)
                            == 0 && mob.Distance(Player.Position) > 1000)
                        {
                            if (DragonDmg == 0)
                                DragonDmg = mob.Health;
                            if (Game.Time - DragonTime > 3)
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
                            var Sec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 3);

                            if (DragonDmg - mob.Health > 0)
                            {
                                var time = (int)(Vector3.Distance(Player.ServerPosition, mob.ServerPosition) / R.Speed + R.Delay);
                                var timeR = (mob.Health - Player.CalculateDamage(mob, DamageType.Physical,
                                    (250 + (100 * R.Level)) + Player.FlatPhysicalDamageMod + 300))
                                    / (Sec / 4);

                                if (time > timeR)
                                    R.Cast(mob.Position);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in On Draw" + ex);
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
                                    Q.Cast(minion);
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

        private static float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            float distance = Vector3.Distance(source.ServerPosition, targetpos);
            float missilespeed = speed;
            if (source.ChampionName == "Jinx" && distance > 1350)
            {
                const float accelerationrate = 0.3f;
                var acceldifference = distance - 1350f;
                if (acceldifference > 150f)
                    acceldifference = 150f;
                var difference = distance - 1500f;
                missilespeed = (1350f * speed + acceldifference * (speed + accelerationrate * acceldifference) + difference * 2200f) / distance;
            }
            return (distance / missilespeed + delay);
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