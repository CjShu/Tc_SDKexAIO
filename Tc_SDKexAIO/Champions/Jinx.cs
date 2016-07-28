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
        private static float DrawSpellTime = 0, DragonDmg = 0, lag = 0, LatFocusTime = Game.Time;
        private static bool BigGun => Player.HasBuff("BigGun");

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
                QMenu.GetBool("blockQ", "Min Mana Ban BigGun(20)", false);
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
                EMenu.GetBool("comboE", "Combo E");
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
                DrawMenu.GetBool("Draw.Enabl", "Draw Enable", false);
                DrawMenu.GetBool("Q", "Q Range", false);
                DrawMenu.GetBool("W", "W Range", false);
                DrawMenu.GetBool("E", "E Range", false);
                DrawMenu.GetBool("R", "R Range", false);
                DrawMenu.GetBool("DrawKSEnemy", "Killable Enemy Notification", false);
                DrawMenu.GetBool("DrawKillableEnemyMini", "Killable Enemy [Mini Map]", false);
                DrawMenu.GetBool("DrawDamage", "Draw Combo Damage", false);
                DrawMenu.GetList("DrawBuffs", "Show Red/Blue Time Circle", new[] { "Off", "Blue Buff", "Red Buff", "Both" });
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "Jinx OK! :)");

            Game.OnUpdate += OnUpdate;
            Variables.Orbwalker.OnAction += OnAction;
            Events.OnGapCloser += OnGapCloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Spellbook.OnCastSpell += OnCastSpell;
            Drawing.OnEndScene += OnEndScene;
            Drawing.OnDraw += OnDraw;
        }
       
        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Q.IsReady())
                Qlogic();

            if (W.IsReady())
                Wlogic();

            if (E.IsReady())
                Elogic();

            if (R.IsReady())
                Rlogic();

            AutoRLogic();
        }

        #region 邏輯

        private static void AutoRLogic()
        {

        }

        private static void Rlogic()
        {

        }

        private static void Elogic()
        {

        }

        private static void Wlogic()
        {
            if (!W.IsReady())
            {
                return;
            }

            if (Combo && Menu["W"]["ComboW"].GetValue<MenuBool>())
            {
                var target = GetTarget(W.Range, DamageType.Physical);

                if (target == null)
                    return;

                float distance = Player.Position.Distance(target.Position);

                if (distance >= 550)
                    if (target.IsValidTarget(W.Range))
                        SpellCast(W, target);
            }

            if ((Harass && Menu["W"]["HarassW"].GetValue<MenuBool>()) || Menu["W"]["AutoW"].GetValue<MenuKeyBind>().Active)
            {
                if (Player.ManaPercent >= Menu["W"]["HarassWMana"].GetValue<MenuSlider>().Value)
                    return;

                if (Player.IsUnderEnemyTurret())
                    return;

                var target = GetTarget(W.Range, DamageType.Physical);

                if (target == null)
                    return;

                float distance = Player.Position.Distance(target.Position);

                if (distance >= 500)
                    if (Menu["W"]["WList" + target.ChampionName].GetValue<MenuBool>())
                        if (target.IsValidTarget(W.Range))
                            if (W.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                                W.Cast(target, true);
            }

            if (Menu["W"]["KSW"].GetValue<MenuBool>().GetValue<MenuBool>())
            {
                var e = GetTarget(W.Range, DamageType.Physical);

                if (e.IsValidTarget() && e.Distance(Player.Position) > 500)
                    if (GetDamage(e) > e.Health)
                        if (CanMove(e))



        }

        private static void Qlogic()
        {

            if ((Farm) && (Game.Time - lag > 0.1) && !BigGun && !Player.IsWindingUp && Variables.Orbwalker.CanAttack)
            {
                if ((Player.ManaPercent >= Menu["Q"]["HarassQMana"].GetValue<MenuSlider>().Value) && Menu["Q"]["HarassQ"].GetValue<MenuBool>())
                {
                    foreach (var minion in GetMinions(Player.ServerPosition, 670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level + 30).Where(minion => !InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion) * 1.2
                            && (650f + Player.BoundingRadius + minion.BoundingRadius) < (Player.ServerPosition.Distance(Movement.GetPrediction(minion, 0.05f).CastPosition)
                            + Player.BoundingRadius + minion.BoundingRadius) && (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                            < (Player.ServerPosition.Distance(Movement.GetPrediction(minion, 0.05f).CastPosition)
                            + Player.BoundingRadius + minion.BoundingRadius)))
                    {
                        Variables.Orbwalker.ForceTarget = null;
                        Q.Cast();
                        return;
                    }
                    lag = Game.Time;
                }
                var t = GetTarget((670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                    + 60, DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if (!BigGun && (!InAutoAttackRange(t) || t.CountEnemyHeroesInRange(250) > 2) && Variables.Orbwalker.GetTarget() == null)
                    {
                        var distance = Player.ServerPosition.Distance(Movement.GetPrediction(t, 0.05f).CastPosition)
                            + Player.BoundingRadius + t.BoundingRadius;

                        if (Combo && (Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 15 || Player.GetAutoAttackDamage(t) * 3 > t.Health))
                        {
                            Q.Cast();
                        }
                        else if (Harass && Menu["Q"]["HarassQ"].GetValue<MenuBool>().Value)
                        {
                            if (!Player.IsWindingUp)
                                if (Variables.Orbwalker.CanAttack)
                                    if (!Player.IsUnderEnemyTurret())
                                        if (Player.ManaPercent >= Menu["Q"]["HarassQMana"].GetValue<MenuSlider>().Value)
                                            if (distance < (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                                                + t.BoundingRadius + Player.BoundingRadius)
                                                Q.Cast();
                        }
                    }
                }
                else if (!BigGun && Combo && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 20 && Player.CountEnemyHeroesInRange(3000) > 0)
                {
                    Q.Cast();
                }
                else if (BigGun && Combo && Player.Mana > R.Instance.ManaCost + W.Instance.ManaCost + 20)
                {
                    Q.Cast();
                }
                else if (BigGun && Combo && Player.CountEnemyHeroesInRange(3000) == 0)
                {
                    Q.Cast();
                }
                else if (BigGun && (LaneClear || Harass || LasHit))
                {
                    Q.Cast();
                }
            }
        }

        #endregion

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && args.Slot == SpellSlot.Q)
            {
                if (Q.IsReady() && Menu["Q"]["blockq"].GetValue<MenuBool>().Value)
                {
                    if (Player.HasBuff("BigGun"))
                    {
                        return;
                    }
                    if (Player.ManaPercent < GetMana(W.Slot, Menu["Q"]["LaneClearQ"]))
                    {
                        args.Process = false;
                    }
                }
            }
        }

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            if (args.Type == OrbwalkingType.BeforeAttack)
            {
                if (!(args.Target is Obj_AI_Hero))
                {
                    return;
                }
                if (!Q.IsReady())
                {
                    return;
                }
                      
                var t = (Obj_AI_Hero)args.Target;

                if (BigGun && t.IsValidTarget())
                {
                    var ReaDistanc = Player.ServerPosition.Distance(Movement.GetPrediction(t, 0.05f).CastPosition)
                        + Player.BoundingRadius + t.BoundingRadius;

                    if (Combo && Menu["Q"]["ComboQ"].GetValue<MenuBool>())
                    {
                        if (ReaDistanc < (650f + Player.BoundingRadius + t.BoundingRadius))
                        {
                            if (Player.Mana < R.Instance.ManaCost + 20 || Player.GetAutoAttackDamage(t) * 3 < t.Health)
                            {
                                Q.Cast();
                            }
                        }
                    }
                    else if ((Farm) && Menu["Q"]["HarassQ"].GetValue<MenuBool>())
                    {
                        if ((ReaDistanc > (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level)
                            || ReaDistanc < (650f + Player.BoundingRadius + t.BoundingRadius)
                            || Player.ManaPercent >= Menu["Q"]["HarassQMana"].GetValue<MenuSlider>().Value))
                        {
                            Q.Cast();
                        }
                    }
                }
                else if (LaneClear && Menu["Q"]["LaneClearQ"] && !BigGun)
                {
                    if (Player.ManaPercent >= Menu["Q"]["LaneClearQMana"].GetValue<MenuSlider>().Value)
                    {
                        var allMinionsQ = GetMinions(Player.ServerPosition, (670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level));

                        foreach (var m in allMinionsQ.Where(m =>
                        args.Target.NetworkId != m.NetworkId && m.Distance(args.Target.Position) < 200
                        && (5 - Q.Level) * Player.GetAutoAttackDamage(m) < args.Target.Health
                        && (5 - Q.Level) * Player.GetAutoAttackDamage(m) < m.Health))
                        {
                            Q.Cast();
                        }
                    }
                }
                if (args.Target is Obj_AI_Hero)
                {
                    var Target = (Obj_AI_Hero)args.Target;
                    var ForceFocusEnamy = Target;
                    var aaRange = Player.AttackRange + Player.BoundingRadius + 350;

                    foreach (var e in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(aaRange)))
                    {
                        if (e.Health / Player.GetAutoAttackDamage(e) + 1 < ForceFocusEnamy.Health / Player.GetAutoAttackDamage(ForceFocusEnamy))
                        {
                            ForceFocusEnamy = e;
                        }
                    }
                    if (ForceFocusEnamy.NetworkId != Target.NetworkId && Game.Time - LatFocusTime < 2)
                    {
                        args.Process = false;
                        return;
                    }
                }
            }
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            if (Menu["E"]["Gapcloser"].GetValue<MenuBool>().Value)
            {
                if (E.IsReady() && args.Sender.IsValidTarget(E.Range) && !Invulnerable.Check(args.Sender, DamageType.Magical, false))
                {
                    E.Cast(args.IsDirectedToPlayer ? Player.ServerPosition : args.End);
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
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

        private static void OnEndScene(EventArgs args)
        {
            var DrawKillableEnemy = Menu["Draw"]["DrawKillableEnemyMini"].GetValue<MenuBool>();
            if (DrawKillableEnemy.Value)
            {
                foreach (var e in GameObjects.EnemyHeroes.Where(e => e.IsVisible && !e.IsDead && !e.IsZombie && e.Health
                     < GetDamage(e)))
                {
                    if ((int)Game.Time % 2 == 1)
                    {
                        #pragma warning disable 618
                        Utility.DrawCircle(e.Position, 850, DrawKillableEnemy, 2, 30, true);
                        #pragma warning restore 618
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Menu["Draw"]["Draw.Enabl"])
            {
                return;
            }
            DrawSpell();
            DrawBuffs();
            DrawKillableEnemy();
            foreach (var t in GameObjects.EnemyHeroes.Where(e => !e.IsDead && e.Health < GetDamage(e)))
            {
                HpBarDraw.DrawText(HpBarDraw.TextStatus, "Can Kill", (int)t.HPBarPosition.X + 145, (int)t.HPBarPosition.Y + 5, SharpDX.Color.Red);
            }

            if (Menu["Draw"]["DrawDamage"].GetValue<MenuBool>())
            {
                foreach (var e in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsValidTarget() && e.IsValid && !e.IsDead && !e.IsZombie))
                {
                    HpBarDraw.Unit = e;
                    HpBarDraw.DrawDmg(GetDamage(e), new ColorBGRA(255, 204, 0, 170));
                }
            }
        }

        #region Draw分類

        private static void DrawSpell()
        {
            var t = GetTarget(Q.Range + 500, DamageType.Physical);
            if (t.IsValidTarget())
            {
                var target = t.Position + Vector3.Normalize(t.ServerPosition - Player.Position) * 80;
                Render.Circle.DrawCircle(target, 75f, Color.Red, 2);
            }

            if (Q != null && Q.IsReady())
            {
                if (Menu["Draw"]["Q"] != null && Menu["Draw"]["Q"].GetValue<MenuBool>().Value)
                {
                    Render.Circle.DrawCircle(Player.Position, Q.Range, Color.LightGreen, 2);
                }
            }

            if (W != null && W.IsReady())
            {
                if (Menu["Draw"]["W"] != null && Menu["Draw"]["W"].GetValue<MenuBool>().Value)
                {
                    Render.Circle.DrawCircle(Player.Position, W.Range, Color.Purple, 2);
                }
            }

            if (E != null && E.IsReady())
            {
                if (Menu["Draw"]["E"] != null && Menu["Draw"]["E"].GetValue<MenuBool>().Value)
                {
                    Render.Circle.DrawCircle(Player.Position, E.Range, Color.Cyan, 2);
                }
            }

            if (R != null && R.IsReady())
            {
                if (Menu["Draw"]["R"] != null && Menu["Draw"]["R"].GetValue<MenuBool>().Value)
                {
                    Render.Circle.DrawCircle(Player.Position, R.Range, Color.Red, 2);
                }
            }
        }

        private static void DrawBuffs()
        {
            var DrawBuffs = Menu["Draw"]["DrawBuffs"].GetValue<MenuList>().Index;

            if ((DrawBuffs == 1 | DrawBuffs == 3) && Player.HasBlueBuff())
            {
                if (BlueBuff.EndTime >= Game.Time)
                {
                    var Circlel = new Geometry.Circle2(new Vector2(Player.Position.X + 3, Player.Position.Y - 3), 170f,
                        Game.Time - BlueBuff.StartTime, BlueBuff.EndTime - BlueBuff.StartTime).ToPolygon();
                    Circlel.Draw(Color.Black, 4);

                    var Circle = new Geometry.Circle2(Player.Position.ToVector2(), 170f,
                        Game.Time - BlueBuff.StartTime, BlueBuff.EndTime - BlueBuff.StartTime).ToPolygon();
                    Circle.Draw(Color.Blue, 4);
                }
            }

            if ((DrawBuffs == 2 || DrawBuffs == 3) && Player.HasRedBuff())
            {
                if (RedBuff.EndTime >= Game.Time)
                {
                    var Circlel = new Geometry.Circle2(new Vector2(Player.Position.X + 3, Player.Position.Y - 3), 150f,
                        Game.Time - RedBuff.StartTime, RedBuff.EndTime - RedBuff.StartTime).ToPolygon();
                    Circlel.Draw(Color.Black, 4);

                    var Circle = new Geometry.Circle2(Player.Position.ToVector2(), 150f,
                        Game.Time - RedBuff.StartTime, RedBuff.EndTime - RedBuff.StartTime).ToPolygon();
                    Circle.Draw(Color.Red, 4);
                }
            }
        }

        public static Obj_AI_Hero GetKillableEnemy
        {
            get
            {
                if (Menu["Draw"]["DrawKSEnemy"].GetValue<MenuBool>())
                {
                    return GameObjects.EnemyHeroes.FirstOrDefault(e => e.IsVisible && !e.IsDead && !e.IsZombie && e.Health < GetDamage(e));
                }
                return null;
            }
        }

        private static void DrawKillableEnemy()
        {
            if (Menu["Draw"]["DrawKSEnemy"].GetValue<MenuBool>())
            {
                var t = KillavleEnemyAa;
                if (t.Item1 != null && t.Item1.IsValidTarget(Player.GetRealAutoAttackRange(null) + 800) && t.Item2 > 0)
                {
                    HpBarDraw.DrawText(HpBarDraw.Text, $"{t.Item1.ChampionName}: {t.Item2} Combo = Kill", (int)t.Item1.HPBarPosition.X + 85, (int)t.Item1.HPBarPosition.Y + 5,
                        SharpDX.Color.GreenYellow);
                }
            }
        }

        #endregion

        #region EXE

        public static void DrawText(Font aFont, String aText, int aPosX, int aPosY, SharpDX.Color aColor)
        {
            aFont.DrawText(null, aText, aPosX + 2, aPosY + 2, aColor != SharpDX.Color.Black ? SharpDX.Color.Black : SharpDX.Color.White);
            aFont.DrawText(null, aText, aPosX, aPosY, aColor);
        }

        public static float AARange => GameObjects.Player.GetRealAutoAttackRange();

        public static float MegaQRange => 525 + 45 + 25 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        public static bool MegaQActive => ObjectManager.Player.AttackRange > 565f;

        private static Tuple<Obj_AI_Hero, int> KillavleEnemyAa
        {
            get
            {
                var x = 0;
                var t = GetTarget(R.Range, DamageType.Physical);
                {
                    if (t.IsValidTarget())
                    {
                        if (t.Health <= GetDamage(t))
                        {
                            x = (int)Math.Ceiling(t.Health / Player.TotalAttackDamage);
                        }
                        return new Tuple<Obj_AI_Hero, int>(t, x);
                    }
                }
                return new Tuple<Obj_AI_Hero, int>(t, x);
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

        private static float GetDamage(Obj_AI_Base t)
        {
            var Damage = 0d;

            Damage -= t.HPRegenRate;

            if (Q.IsReady())
            {
                Damage += Player.GetSpellDamage(t, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                Damage += Player.GetSpellDamage(t, SpellSlot.W);
            }

            if (E.IsReady())
            {
                Damage += Player.GetSpellDamage(t, SpellSlot.E);
            }

            if (R.IsReady())
            {
                Damage += Player.GetSpellDamage(t, SpellSlot.R) * 3;
            }
            if (t.IsValidTarget(Q.Range + E.Range) && Q.IsReady() && R.IsReady())
            {
                Damage += Player.TotalAttackDamage * 2;
            }
            Damage += Player.TotalAttackDamage * 2;

            if (Player.HasBuff("SummonerExhaust"))
                Damage = Damage * 0.6f;

            return (float)Damage;
        }

        #endregion
    }
}