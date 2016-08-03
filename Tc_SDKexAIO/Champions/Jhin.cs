namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.UI;

    using System;
    using System.Linq;
    using System.Windows.Forms;

    using SharpDX;

    using Common;
    using Config;
    using static Common.Manager;

    using Menu = LeagueSharp.SDK.UI.Menu;

    internal static class Jhin
    {

        private static Menu Menu => PlaySharp.Menu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static float LasPing = Variables.TickCount;
        private static string StartR = "JhinR";
        private static string IsCastingR = "JhinR";
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
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("ComboW", "ComnoW");
                WMenu.GetBool("KSW", "Killsteal W");
                WMenu.GetBool("HarassW", "Harass W", false);
                WMenu.GetBool("LaneClearW", "LaneClear W", false);
                //WMenu.GetBool("StunW", "Stun W", false);
                WMenu.GetBool("WMO", "W Only Marked Target", false);
                WMenu.GetKeyBind("WTap", "W Fire On Tap", Keys.G, KeyBindType.Press);
                WMenu.Add(new MenuKeyBind("AutoW", "Use W Auto (Toggle)", Keys.Y, KeyBindType.Toggle));
                WMenu.GetSlider("HarassWMana", "Harass W Min Mana > =", 60);
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set | E 設定"));
            {
                EMenu.GetSeparator("E: Mobe");
                EMenu.GetBool("ComboE", "Combo E");
                EMenu.GetBool("LaneClearE", "LaneClear E", false);
                EMenu.GetSlider("LaneClearEMana", "LaneClear E Min Mana", 40, 0, 100);
                EMenu.GetSlider("LCminions", "LaneClear Min minion", 3, 8, 0);
                EMenu.GetSeparator("E: Gapcloser | Melee Modes");
                EMenu.GetBool("Gapcloser", "Gapcloser E", false);
                EMenu.GetSeparator("Auto E Always");
                EMenu.GetKeyBind("ETap", "Force E", Keys.H, KeyBindType.Press);
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set | R設定"));
            {
                RMenu.GetKeyBind("RTap", "R Fire On Tap", Keys.S, KeyBindType.Press);
                RMenu.GetBool("Ping", "Ping Who Can Killable(Every 3 Seconds)", false);
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

            Menu.GetBool("ComboY", "Combo Use Youmoo", false);
        
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

            try
            {
                if (Player.IsDead)
                    return;

                    ComboLogic(args);

                    HarassLogic(args);

                    LaneClearLogic(args);
  
                    KSLogic(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in On Update" + ex);
            }
        }
            
        static void CastYoumoo()
        {
            if (Items.CanUseItem(3142))
                Items.UseItem(3142);
        }

        private static void KSLogic(EventArgs args)
        {
            try
            {
                if (Menu["W"]["WTap"].GetValue<MenuKeyBind>().Active)
                {
                    if (W.IsReady())
                    {
                        var WTarget = GetTarget(W.Range, W.DamageType);

                        if (W.GetPrediction(WTarget).Hitchance >= HitChance.High)
                        {
                            W.Cast(W.GetPrediction(WTarget).UnitPosition);
                        }
                    }
                }
                if (Menu["Q"]["KillStealQ"].GetValue<MenuBool>())
                {
                    if (Q.IsReady())
                    {
                        var QTarget = GetTarget(Q.Range, Q.DamageType);

                        if (QTarget.Health <= Q.GetDamage(QTarget))
                        {
                            Q.Cast(QTarget);
                        }
                    }
                }
                if (Menu["W"]["KSW"].GetValue<MenuBool>())
                {
                    if (W.IsReady())
                    {
                        var WTarget = GetTarget(W.Range, W.DamageType);

                        if (WTarget.Health <= W.GetDamage(WTarget) && W.GetPrediction(WTarget).Hitchance >= HitChance.VeryHigh)
                        {
                            W.Cast(W.GetPrediction(WTarget).UnitPosition);
                        }
                    }
                }
                foreach (var e in GameObjects.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget() && x.Health
                    <= R.GetDamage(x) * 3 && !x.IsZombie && !x.IsDead && !x.IsDead))
                {
                    if (LasPing <= Variables.TickCount && Menu["R"]["Ping"])
                    {
                        LasPing = Variables.TickCount + 3000;
                        Game.SendPing(PingCategory.Danger, e);
                    }
                }
                if (Menu["E"]["ETap"].GetValue<MenuKeyBind>().Active)
                {
                    if (E.IsReady())
                    {
                        var ETarget = GetTarget(E.Range, E.DamageType);

                        if (!ETarget.IsDead && R.GetPrediction(ETarget).Hitchance >= HitChance.High)
                        {
                            E.Cast(R.GetPrediction(ETarget).UnitPosition);
                        }
                    }
                }
                if (Menu["R"]["RTap"].GetValue<MenuKeyBind>().Active)
                {
                    if (R.IsReady() && R.Instance.Name == StartR)
                    {
                        var RTarget = GetTarget(R.Range, R.DamageType);

                        if (RTarget.Health <= R.GetDamage(RTarget) * 3 && !RTarget.IsZombie && !RTarget.IsDead
                            && R.GetPrediction(RTarget).Hitchance >= HitChance.VeryHigh)
                        {
                            if (Items.CanUseItem(3363))
                            {
                                Items.UseItem(3363, RTarget.Position);
                            }
                            R.Cast(R.GetPrediction(RTarget).UnitPosition);
                        }
                    }
                }
                if (Menu["R"]["RTap"].GetValue<MenuKeyBind>().Active)
                {
                    if (Q.IsReady() && R.Instance.Name == IsCastingR)
                    {
                        var RTarget = GetTarget(R.Range, R.DamageType);

                        if (Items.CanUseItem(3363))
                        {
                            Items.UseItem(3363, RTarget.Position);
                        }
                        R.Cast(R.GetPrediction(RTarget).UnitPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In KSLogic" + ex);
            }
        }

        private static void ComboLogic(EventArgs args)
        {
            try
            {
                if (Combo && Menu["W"]["ComboW"].GetValue<MenuBool>())
                {
                    if (W.IsReady())
                    {
                        var WTarget = GetTarget(2500, W.DamageType);

                        var WMO = Menu["W"]["WMO"].GetValue<MenuBool>();

                        if (W.GetPrediction(WTarget).Hitchance >= HitChance.VeryHigh
                            && ((WTarget.HasBuff("jhinespotteddebuff") && WMO) || !WMO))
                        {
                            W.Cast(W.GetPrediction(WTarget).UnitPosition);
                        }
                    }
                }
                if (Combo && Menu["Q"]["ComboQ"].GetValue<MenuBool>())
                {
                    var QTarget = GetTarget(550, Q.DamageType);

                    if (Q.IsReady() && !Player.IsWindingUp && !Variables.Orbwalker.CanAttack)
                    {
                        Q.Cast(QTarget);
                    }
                }
                if (Combo && Menu["E"]["ComboE"].GetValue<MenuBool>())
                {
                    var ETarget = Variables.Orbwalker.GetTarget();

                    if (E.IsReady() && ETarget.IsValidTarget())
                    {
                        E.Cast(E.GetPrediction((Obj_AI_Base)ETarget).UnitPosition);
                    }
                }
                if (Combo && Menu["W"]["AutoW"].GetValue<MenuKeyBind>().Active)
                {
                    if (Player.ManaPercent >= Menu["W"]["HarassWMana"].GetValue<MenuSlider>().Value)
                    {
                        var WTarget = GetTarget(2500, W.DamageType);

                        var WMO = Menu["W"]["WMO"].GetValue<MenuBool>();

                        if (W.GetPrediction(WTarget).Hitchance >= HitChance.VeryHigh
                            && W.IsReady() && ((WTarget.HasBuff("jhinespotteddebuff")
                            && WMO) && !WMO))
                        {
                            W.Cast(W.GetPrediction(WTarget).UnitPosition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In ComboLogic" + ex);
            }
        }

        private static void HarassLogic(EventArgs args)
        {
            try
            {
                if (Harass && Menu["W"]["HarassW"].GetValue<MenuBool>())
                {
                    var WTarget = GetTarget(2500, W.DamageType);

                    var WMO = Menu["W"]["WMO"].GetValue<MenuBool>();

                    if (W.GetPrediction(WTarget).Hitchance >= HitChance.VeryHigh
                        && W.IsReady() && ((WTarget.HasBuff("jhinespotteddebuff") && WMO) || !WMO))
                    {
                        W.Cast(W.GetPrediction(WTarget).UnitPosition);
                    }
                }
                if (Harass && Menu["Q"]["HarassQ"].GetValue<MenuBool>())
                {
                    var QTarget = GetTarget(550, Q.DamageType);

                    if (Q.IsReady() && !Player.IsWindingUp && !Variables.Orbwalker.CanAttack)
                    {
                        Q.Cast(QTarget);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In HarassLogic" + ex);
            }
        }

        private static void LaneClearLogic(EventArgs args)
        {
            try
            {
                if (LaneClear && Menu["Q"]["LaneClearQ"].GetValue<MenuBool>())
                {
                    var minionQ = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range)).MinOrDefault(x => x.Health);

                    if (minionQ != null)
                    {
                        Q.Cast(minionQ);
                    }
                }

                if (LaneClear && Menu["Q"]["JungleQ"].GetValue<MenuBool>())
                {
                    var JungleQ = GameObjects.JungleLarge.Where(x => x.IsValidTarget(Q.Range)).MinOrDefault(x => x.Health);
                    if (JungleQ != null)
                    {
                        Q.Cast(JungleQ);
                    }
                }
                if (LaneClear && Menu["W"]["LaneClearW"].GetValue<MenuBool>())
                {
                    var minionW = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range)).MinOrDefault(x => x.Health);

                    if (minionW != null)
                    {
                        W.Cast(minionW);
                    }
                }
                if (LaneClear && Menu["E"]["LaneClearE"].GetValue<MenuBool>())
                {
                    var minionE = GetMinions(Player.ServerPosition, E.Range);

                    var farmPosition = E.GetCircularFarmLocation(minionE, W.Width);

                    if (Player.ManaPercent > Menu["E"]["LaneClearEMana"].GetValue<MenuSlider>().Value)
                    {
                        if (farmPosition.MinionsHit >= Menu["E"]["LCminions"].GetValue<MenuSlider>().Value)
                            E.Cast(farmPosition.Position);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error In LaneClearLogic" + ex);
            }
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