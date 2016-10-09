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
    using System.Collections.Generic;

    using Color = System.Drawing.Color;

    using Common;
    using static Common.Manager;
    using Config;
    using static Core.TCommon;

    internal static class Jinx
    {

        private static Spell Q, W, E, R;
        private static Menu Menu => PlaySharp.ChampionMenu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static bool JinxQ = Player.HasBuff("JinxQ");

        private static float DragonDmg = 0;
        private static double QCastTime = 0, WCastTime = 0, DragonTime = 0, GrabTime = 0, lag = 0;

        internal static void Init()
        {

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1450f).SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 900f).SetSkillshot(1.2f, 100f, 1750f, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 3000f).SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);

            var QMenu = Menu.Add(new Menu("Q", "Q.Set"));
            {
                QMenu.GetSeparator("Q: Always On");
                QMenu.Add(new MenuBool("ComboQ", "Combo Q", true));
                QMenu.Add(new MenuSliderButton("LaneClearQ", "LaneClear Q | Min Mana", 50));
                QMenu.Add(new MenuSliderButton("HarassQ", "Harass Q | Min Mana", 50));
                QMenu.Add(new MenuSliderButton("LastHitQ", "LastHit Q | Min Mana", 20));
                QMenu.Add(new MenuBool("FarmQout", "Harass Q Only switch Not machine Gun Range Inner", true));
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set"));
            {
                WMenu.Add(new MenuBool("ComboW", "Combo W", true));
                WMenu.Add(new MenuBool("KillstealW", "Killsteal W Enemy", true));
                WMenu.Add(new MenuSliderButton("HarassW", "Harass W | Min Mana", 40));
                var WList = WMenu.Add(new Menu("WList", "W Harass List"));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => WList.Add(new MenuBool(i.ChampionName.ToLower(), i.ChampionName, true)));
                    }
                }
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set"));
            {
                EMenu.GetSeparator("E: Mobe");
                EMenu.Add(new MenuBool("ComboE", "Combo E", true));
                EMenu.GetSeparator("E: Gapcloser | Melee Modes");
                EMenu.Add(new MenuBool("Gapcloser", "Gapcloser E", true));
                EMenu.GetSeparator("Auto E Set");
                EMenu.Add(new MenuBool("AutoE", "AutoE", true));
                EMenu.Add(new MenuBool("SlowE", "Slow E", true));
                EMenu.Add(new MenuBool("StunE", "Stun E", true));
                EMenu.Add(new MenuBool("TelE", "Tel E", true));
                EMenu.Add(new MenuBool("ImmeE", "Imm E", true));
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set"));
            {
                RMenu.GetSeparator("R: Mobe");
                RMenu.Add(new MenuBool("AutoR", "Auto R", true));
                RMenu.Add(new MenuSlider("HitchanceR", "R Min enemy Count = ", 2, 1, 3));
                RMenu.Add(new MenuBool("Rturrent", "Not R Turrent", true));
                RMenu.Add(new MenuBool("RDragon", "Auto R Dragon", true));
                RMenu.Add(new MenuBool("RBaron", "Auto R Baron", true));
            }

            ModeBaseUlti.Init(Menu);

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.Add(new MenuBool("Q", "Q Range"));
                DrawMenu.Add(new MenuBool("W", "W Range"));
                DrawMenu.Add(new MenuBool("E", "E Range"));
                DrawMenu.Add(new MenuBool("RKill", "Draw R Kill", true));
                DrawMenu.Add(new MenuBool("Damage", "Draw Combo Damage", true));
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "OK! :)");


            Game.OnUpdate += OnUpdate;
            Variables.Orbwalker.OnAction += OnAction;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Events.OnGapCloser += OnGapCloser;
            Events.OnInterruptableTarget += OnInterruptableTarget;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
        }

        private static void OnEndScene(EventArgs args)
        {
            if (!Menu["Draw"]["Damage"])
                return;

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsValidTarget() && !e.IsZombie))
            {
                HpBarDraw.Unit = enemy;
                HpBarDraw.DrawDmg((float)GetDamage(enemy), SharpDX.Color.Cyan);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Menu["Draw"]["Q"])
            {
                if (Q.IsReady())
                    Render.Circle.DrawCircle(Player.Position, 585f + Player.BoundingRadius, Color.DeepPink);
                else
                    Render.Circle.DrawCircle(Player.Position, bonusRange() - 28, Color.DeepPink);                       
            }

            if (Menu["Draw"]["W"])
            {
                if (W.IsReady())
                    Render.Circle.DrawCircle(Player.Position, W.Range, Color.Blue);
                else
                    Render.Circle.DrawCircle(Player.Position, W.Range, Color.Blue);
            }

            if (Menu["Draw"]["E"])
            {
                if (E.IsReady())
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.Yellow);
                else
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.Yellow);
            }

            if (Menu["Draw"]["RKill"])
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && !x.IsDead && !x.IsZombie && x.Health < R.GetDamage(x)))
                {
                    if (target != null)
                    {
                       Drawing.DrawText(Drawing.WorldToScreen(target.Position)[0] - 20, Drawing.WorldToScreen(target.Position)[1], Color.Red, "R KillAble!!!!!");
                    }
                }
            }
        }

        private static void OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs args)
        {
            if (Menu["E"]["ImmeE"] && args.DangerLevel >= DangerLevel.High)
            {
                if (args.Sender.IsValidTarget(E.Range - 30) && E.IsReady() && Player.HealthPercent <= args.Sender.HealthPercent)
                {
                    E.CastOnUnit(args.Sender);
                }
            }
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            if (Menu["E"]["Gapcloser"] && E.IsReady() && args.Sender.IsValidTarget(E.Range))
            {
                if (args.Sender.DistanceToPlayer() <= 200)
                {
                    E.Cast(args.End);
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMinion)
                return;

            if (sender.IsMe)
            {
                if (args.SData.Name == "JinxWMissile")
                    WCastTime = Game.Time;
            }

            if (E.IsReady())
            {
                if (sender.IsEnemy && sender.IsValidTarget(E.Range) && ShouldUseE(args.SData.Name))
                {
                    E.Cast(sender.Position);
                }

                if (sender.IsAlly && args.SData.Name == "RocketGrab" && Player.Distance(sender.Position) < E.Range)
                {
                    GrabTime = Game.Time;
                }
            }
        }

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            if (args.Type == OrbwalkingType.BeforeAttack)
            {
                if (!Q.IsReady() && !JinxQ && !Menu["Q"]["ComboQ"])
                    return;

                var t = args.Target as Obj_AI_Hero;

                if (t != null)
                {
                    var realDistance = GetRealDistance(t) - 50;

                    if (Combo && (realDistance < GetRealPowPowRange(t) || (Player.Mana < R.Instance.ManaCost + 20 && Player.GetAutoAttackDamage(t) * 3 < t.Health)))
                    {
                        Q.Cast();
                    }
                    else if (Harass && Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().BValue && (realDistance > bonusRange() || realDistance < GetRealPowPowRange(t) && Player.ManaPercent >= Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().SValue))
                    {
                        Q.Cast();
                    }                        
                }

                var Minion = args.Target as Obj_AI_Minion;

                if (Minion != null)
                {
                    var realDistance = GetRealDistance(Minion);

                    if (LaneClear && Menu["Q"]["LaneClearQ"].GetValue<MenuSliderButton>().BValue)
                    {
                        if (realDistance < GetRealPowPowRange(Minion) || Player.ManaPercent >= Menu["Q"]["LaneClearQ"].GetValue<MenuSliderButton>().SValue)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead && !Player.IsWindingUp)
            {
                return;
            }

            Killsteal();
            JungleclearLogic();
            ELogic();

            switch (Variables.Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    ComboLogic();
                    break;
                case OrbwalkingMode.Hybrid:
                    HarassLogic();
                    break;
                case OrbwalkingMode.LaneClear:
                    LaneclearLogic();
                    break;
                case OrbwalkingMode.LastHit:
                    if (Menu["Q"]["LastHit"].GetValue<MenuSliderButton>().BValue && Q.IsReady())
                    {
                        var QTarget = GetMinions(Player.Position, bonusRange() + 30).Where(minion => !AutoAttack.InAutoAttackRange(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion));

                        foreach (var minion in QTarget)
                        {
                            var hpPred = GetHealthPrediction(minion, 400, 70);

                            if (hpPred < Player.GetAutoAttackDamage(minion) * 1.1 && hpPred > 4 || Player.ManaPercent >= Menu["Q"]["LastHit"].GetValue<MenuSliderButton>().SValue)
                            {
                                Q.Cast(minion);
                            }
                        }
                    }
                    break;
            }
        }

        private static void ComboLogic()
        {
            var QTarget = GetTarget(bonusRange() + 60, DamageType.Physical);
            var WTarget = GetTarget(W.Range, W.DamageType);
            var ETarget = GetTarget(E.Range, E.DamageType);

            if (QTarget.IsValidTarget() && Menu["Q"]["ComboQ"] && Q.IsReady())
            {
                if (!JinxQ && (!AutoAttack.InAutoAttackRange(QTarget) || QTarget.CountEnemyHeroesInRange(250) > 2) && Variables.Orbwalker.GetTarget() == null)
                {
                    var distance = GetRealDistance(QTarget);

                    if (Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 10 || Player.GetAutoAttackDamage(QTarget) * 3 > QTarget.Health)
                    {
                        Q.Cast();
                    }
                }
            }
            else if (!JinxQ && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 20 && Player.CountEnemyHeroesInRange(2000) > 0)
            {
                Q.Cast();
            }
            else if (JinxQ && Player.Mana < R.Instance.ManaCost + W.Instance.ManaCost + 20)
            {
                Q.Cast();
            }
            else if (JinxQ && Player.CountEnemyHeroesInRange(2000) == 0)
            {
                Q.Cast();
            }

            if (Player.CountEnemyHeroesInRange(bonusRange()) == 0)
            {
                if (Menu["W"]["ComboW"] && W.IsReady())
                {
                    if (Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 10)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && GetRealDistance(enemy) > bonusRange()).OrderBy(enemy => enemy.Health))
                        {
                            CastSpell(W, enemy);
                        }
                    }
                }
            }

            if (Player.IsMoving && Menu["E"]["ComboE"] && Player.Mana > R.Instance.ManaCost + E.Instance.ManaCost + W.Instance.ManaCost)
            {
                if (CheckTarget(ETarget) && E.GetPrediction(ETarget).CastPosition.Distance(ETarget.Position) > 200)
                {
                    E.CastIfWillHit(ETarget, 2);

                    if (ETarget.HasBuffOfType(BuffType.Slow) && Menu["E"]["SlowE"])
                    {
                        CastSpell(E, ETarget);
                    }

                    if (IsMovingInSameDirection(Player, ETarget))
                    {
                        CastSpell(E, ETarget);
                    }
                }
            }
        }

        private static void HarassLogic()
        {
            var QTarget = GetTarget(bonusRange() + 60, DamageType.Physical);
            var WTarget = GetTarget(W.Range, W.DamageType);

            if (CheckTarget(QTarget) && Q.IsReady() && Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().BValue)
            {
                if (!JinxQ && (!AutoAttack.InAutoAttackRange(QTarget) || QTarget.CountEnemyHeroesInRange(250) > 2) && Variables.Orbwalker.GetTarget() == null)
                {
                    var Distance = GetRealDistance(QTarget);

                    if (Player.ManaPercent >= Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().SValue || Player.GetAutoAttackDamage(QTarget) * 3 > QTarget.Health)
                    {
                        Q.Cast();
                    }
                    else if (!Player.IsWindingUp && Variables.Orbwalker.CanAttack && !Player.IsUnderEnemyTurret() && Distance < bonusRange() + QTarget.BoundingRadius + Player.BoundingRadius)
                    {
                        Q.Cast();
                    }
                }
            }

            if (!JinxQ && !Player.IsWindingUp && Variables.Orbwalker.GetTarget() == null && Q.IsReady())
            {
                if (Variables.Orbwalker.CanAttack && Menu["Q"]["FarmQout"] && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + E.Instance.ManaCost + 10)
                {
                    foreach (var minion in GetMinions(Player.Position, bonusRange() + 30).Where(minion => !AutoAttack.InAutoAttackRange(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion)))
                    {
                        var hpPred = GetHealthPrediction(minion, 400, 70);

                        if (hpPred < Player.GetAutoAttackDamage(minion) * 1.1 && hpPred > 4)
                        {
                            Variables.Orbwalker.ForceTarget.Distance(minion);
                            Q.Cast();
                            return;
                        }
                    }
                }
            }

            if (Player.CountEnemyHeroesInRange(bonusRange()) == 0)
            {
                if (Menu["W"]["HarassW"].GetValue<MenuSliderButton>().BValue && !Player.IsWindingUp && CanHarras())
                {
                    if (CheckTarget(WTarget) &&  W.IsReady())
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && Menu["W"]["WList"][enemy.ChampionName.ToLower()].GetValue<MenuBool>()))
                        {
                            if (Player.ManaPercent >= Menu["W"]["HarassW"].GetValue<MenuSliderButton>().SValue)
                            {
                                CastSpell(W, enemy);
                            }
                        }
                    }
                }
            }    
        }

        private static void LaneclearLogic()
        {
            var QTarget = GetTarget(bonusRange() + 60, DamageType.Physical);

            if (!JinxQ && !Player.IsWindingUp && Variables.Orbwalker.GetTarget() == null && Variables.Orbwalker.CanAttack)
            {
                if (Menu["Q"]["FarmQout"] && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + E.Instance.ManaCost + 10)
                {
                    foreach (var minion in GetMinions(Player.Position, bonusRange() + 30).Where(minion => !AutoAttack.InAutoAttackRange(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion)))
                    {
                        var hpPred = GetHealthPrediction(minion, 400, 70);
                        if (hpPred < Player.GetAutoAttackDamage(minion) * 1.1 && hpPred > 4)
                        {                            
                            Q.Cast(minion);
                            return;
                        }
                    }
                }
            }

            if (QTarget.IsValidTarget())
            {
                if (!JinxQ && (!AutoAttack.InAutoAttackRange(QTarget) || QTarget.CountEnemyHeroesInRange(250) > 2) && Variables.Orbwalker.GetTarget() == null)
                {
                    var Distance = GetRealDistance(QTarget);

                    if (!Player.IsWindingUp && Variables.Orbwalker.CanAttack && Q.IsReady() && Menu["Q"]["LaneClearQ"].GetValue<MenuSliderButton>().BValue)
                    {
                        if (Player.ManaPercent >= Menu["Q"]["LaneClearQ"].GetValue<MenuSliderButton>().SValue && !Player.IsUnderEnemyTurret() && Distance < bonusRange() + QTarget.BoundingRadius + Player.BoundingRadius)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static void JungleclearLogic()
        {
            var mobs = GetMobs(Player.Position, Q.Range, true);
            foreach (var mob in mobs)
            {
                if (mob.Health < mob.MaxHealth && ((mob.SkinName.ToLower().Contains("dragon") && Menu["R"]["RDragon"]) || (mob.SkinName == "SRU_Baron" && Menu["R"]["RBaron"])) && mob.CountAllyHeroesInRange(1000) == 0 && mob.Distance(Player.Position) > 1000)
                {
                   if (DragonDmg == 0)
                    {
                        DragonDmg = mob.Health;
                    }
                    if (Game.Time - DragonTime > 4)
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            DragonDmg = mob.Health;
                        }
                        DragonTime = Game.Time;
                    }
                    var DmgSec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 4);
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            var timeTravel = GetUltTravelTime(Player, R.Speed, R.Delay, mob.Position);
                            var timeR = (mob.Health - Player.CalculateDamage(mob, DamageType.Physical, (250 + (100 * R.Level)) + Player.FlatPhysicalDamageMod + 300)) / (DmgSec / 4);
                            {
                                if (timeTravel > timeR)
                                {
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
            }
        }

        private static void Killsteal()
        {
            if (Player.IsUnderEnemyTurret() && Menu["R"]["Rturrent"] && R.IsReady())
                return;

            if (Game.Time - WCastTime > 0.9 && Menu["R"]["AutoR"] && R.IsReady())
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(target => target.IsValidTarget(R.Range) && CanKill(target)))
                {
                    var PredHealth = target.Health - GetIncomingDamage(target);
                    var RDmg = R.GetDamage(target);

                    if (RDmg > PredHealth && !Manager.SpellCollision(target, R) && GetRealDistance(target) > bonusRange() + 200)
                    {
                        if (GetRealDistance(target) > bonusRange() + 300 + target.BoundingRadius && target.CountAllyHeroesInRange(600) == 0 && Player.CountEnemyHeroesInRange(400) == 0)
                        {
                            castR(target);
                        }
                        else if (target.CountEnemyHeroesInRange(200) > 2)
                        {
                            R.Cast(target);
                        }
                    }
                }
            }

            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.IsHPBarRendered))
            {
                if (CheckTarget(target))
                {

                    if (W.GetDamage(target) > target.Health && Menu["W"]["KillstealW"])
                    {
                        if (target.IsValidTarget(W.Range))
                        {
                            Q.Cast(target);
                        }
                    }
                }
            }
        }

        private static void ELogic()
        {
            if (Player.Mana > R.Instance.ManaCost + E.Instance.ManaCost && Menu["E"]["AutoE"] && E.IsReady() && Game.Time - GrabTime > 1)
            {
                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(E.Range + 50) && !CanMove(enemy)))
                {
                    E.Cast(enemy);
                    return;
                }
                if (Menu["E"]["TelE"])
                {
                    var trapPos = GetTrapPos(E.Range);
                    if (!trapPos.IsZero)
                    {
                        E.Cast(trapPos);
                    }                    
                }
            }
        }

        private static float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            float distance = Vector3.Distance(source.ServerPosition, targetpos);
            float missilespeed = speed;
            if (source.ChampionName == "Jinx" && distance > 1350)
            {
                const float accelerationrate = 0.3f; //= (1500f - 1350f) / (2200 - speed), 1 unit = 0.3units/second
                var acceldifference = distance - 1350f;
                if (acceldifference > 150f) //it only accelerates 150 units
                    acceldifference = 150f;
                var difference = distance - 1500f;
                missilespeed = (1350f * speed + acceldifference * (speed + accelerationrate * acceldifference) + difference * 2200f) / distance;
            }
            return (distance / missilespeed + delay);
        }

        private static void castR(Obj_AI_Hero target)
        {
            var AoeR = Menu["R"]["HitchanceR"].GetValue<MenuSlider>().Value;

            if (AoeR == 0)
            {
                R.Cast(R.GetPrediction(target).CastPosition);
            }
            else if (AoeR == 1)
            {
                R.Cast(target);
            }
            else if (AoeR == 2)
            {
                CastSpell(R, target);
            }
            else if (AoeR == 3)
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if ((Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > 400)
                {
                    CastSpell(R, target);
                }
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

        private static float bonusRange()
        {
            return 670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private static float GetRealPowPowRange(GameObject target)
        {
            return 620f + Player.BoundingRadius + target.BoundingRadius;

        }

        private static float GetRealDistance(Obj_AI_Base target)
        {

            return Player.ServerPosition.Distance(Movement.GetPrediction(target, 0.05f).CastPosition) + Player.BoundingRadius + target.BoundingRadius;
        }
    }
}