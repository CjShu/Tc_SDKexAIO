﻿namespace Tc_SDKexAIO.Champions
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.Enumerations;

    using System;
    using System.Linq;

    using SharpDX;

    using Common;

    using static Common.Manager;

    internal static class Ahri
    {
        private static Spell Q, W, E, R;

        private static Menu Menu => PlaySharp.ChampionMenu;

        private static SpellSlot Ignite = Player.GetSpellSlot("SummonerDot");

        private static float IgniteRange = 600f;

        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static GameObject QMissile = null, EMissile = null;
        private static MissileClient QReturn, Qobj;


        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 870f).SetSkillshot(0.25f, 90f, 1550f, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 680f);
            E = new Spell(SpellSlot.E, 920f).SetSkillshot(0.25f, 70f, 1550f, true, SkillshotType.SkillshotLine);
            R = new Spell(SpellSlot.R, 600f);

            var QMenu = Menu.Add(new Menu("Q", "Q.Set"));
            {
                QMenu.Add(new MenuSeparator("Mode", "模式"));
                QMenu.Add(new MenuBool("AutoQ", "自動 Q", true));
                QMenu.Add(new MenuBool("KillStealQ", "擊殺 Q", true));
                QMenu.Add(new MenuBool("ComboQ", "連招 Q", true));
                QMenu.Add(new MenuSeparator("LaneClearMode", "清線 模式"));
                QMenu.Add(new MenuSliderButton("LaneClear", "清線 Q | 最低魔力 = ", 40, 0, 100, true));
                QMenu.Add(new MenuSlider("LCMinionsHit", "清線 Q 最低命中小兵數量", 3, 2, 6));
                QMenu.Add(new MenuSliderButton("JungClear", "清野 Q | 最低魔力 = ", 40, 0, 100, true));
                QMenu.Add(new MenuSeparator("HarassMode", "騷擾 模式"));
                QMenu.Add(new MenuSliderButton("HarassQ", "騷擾 Q | 最低魔力 = ", 40, 0, 100, false));
                var QList = QMenu.Add(new Menu("QList", "騷擾 Q 目標名單"));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => QList.Add(new MenuBool(i.ChampionName.ToLower(), i.ChampionName, true)));
                    }
                }
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set"));
            {
                WMenu.Add(new MenuSeparator("Mode", "模式"));
                WMenu.Add(new MenuBool("KillStealW", "擊殺 W", true));
                WMenu.Add(new MenuBool("ComboW", "連招 W", true));
                WMenu.Add(new MenuSeparator("LaneClearMode", "清線 模式"));
                WMenu.Add(new MenuSliderButton("LaneClear", "清線 W | 最低魔力 = ", 40, 0, 100, true));
                WMenu.Add(new MenuSliderButton("JungClear", "清野 W | 最低魔力 = ", 40, 0, 100, true));
                WMenu.Add(new MenuSeparator("HarassMode", "騷擾 模式"));
                WMenu.Add(new MenuSliderButton("HarassW", "騷擾 W | 最低魔力 = ", 40, 0, 100, false));
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set"));
            {
                EMenu.Add(new MenuSeparator("Mode", "模式"));
                EMenu.Add(new MenuBool("KillStealE", "擊殺 E", true));
                EMenu.Add(new MenuSeparator("HarassMode", "騷擾 模式"));
                EMenu.Add(new MenuSliderButton("HarassE", "騷擾 E | 最低魔力 = ", 40, 0, 100, true));
                EMenu.Add(new MenuSeparator("Mode2", "連招 E 模式"));
                EMenu.Add(new MenuBool("ComboE", "連招 E", true));
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set"));
            {
                RMenu.Add(new MenuSeparator("Mode2", "連招 模式"));
                RMenu.Add(new MenuBool("ComboR", "連招 R", true));
                RMenu.Add(new MenuBool("RCheck", "檢查R", true));
                RMenu.Add(new MenuBool("RTurret", "塔下 禁止 R", true));

            }

            var MiscMenu = Menu.Add(new Menu("Misc", "Misc.Set"));
            {               
                MiscMenu.Add(new MenuSeparator("Mode", "反突進 模式"));
                MiscMenu.Add(new MenuBool("EGap", "反突進 E 目標", true));
                var MiscList = MiscMenu.Add(new Menu("MiscList", "反突進 目標名單"));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => MiscList.Add(new MenuBool(i.ChampionName.ToLower(), i.ChampionName, true)));
                    }
                }
            }

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.Add(new MenuBool("Q", "Q 範圍"));
                DrawMenu.Add(new MenuBool("W", "W 範圍"));
                DrawMenu.Add(new MenuBool("E", "E 範圍"));
                DrawMenu.Add(new MenuBool("R", "R 範圍"));
                DrawMenu.Add(new MenuBool("Damage", "顯示連招傷害(青色)", true));
            }

            Menu.Add(new MenuBool("ComboIgnite", "連招使用點燃", true));

            PlaySharp.Write(GameObjects.Player.ChampionName + "OK! :)");

            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Events.OnInterruptableTarget += OnInterruptableTarget;
            Events.OnGapCloser += OnGapCloser;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
        }

        private static void OnEndScene(EventArgs args)
        {
            if (!Menu["Draw"]["Damage"])
            {
                return;
            }

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsValidTarget() && !e.IsZombie))
            {
                HpBarDraw.Unit = enemy;
                HpBarDraw.DrawDmg((float)GetDamage(enemy), Color.Cyan);
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Menu["Draw"]["Q"] && Q.Level > 1)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.DeepPink);
            }

            if (Menu["Draw"]["W"] && W.Level > 1)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.AliceBlue);
            }

            if (Menu["Draw"]["E"] && E.Level > 1)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Gray);
            }

            if (Menu["Draw"]["R"] && R.Level > 1)
            {
                Render.Circle.DrawCircle(Player.Position, 450f, System.Drawing.Color.Yellow);
            }
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            if (Menu["Misc"]["EGap"] && E.IsReady())
            {
                if (Menu["Misc"]["MiscList"][args.Sender.ChampionName.ToLower()] && args.Sender.DistanceToPlayer() <= 300)
                {
                    CastSpell(E, args.Sender);
                }
            }
        }

        private static void OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs args)
        {
            if (!args.Sender.IsEnemy || !args.Sender.IsValidTarget(E.Range) || !E.IsReady())
                return;

            if (args.DangerLevel >= LeagueSharp.Data.Enumerations.DangerLevel.High || args.Sender.IsCastingInterruptableSpell())
            {
                CastSpell(E, args.Sender);
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is MissileClient)) return;

            var QMissleClient = (MissileClient)sender;

            if (!QMissleClient.SpellCaster.IsMe) return;

            var Name = QMissleClient.SData.Name;

            if (Name.Contains("AhriOrbMissile") && QMissleClient.IsValid)
            {
                Qobj = null;
            }

            if (Name.Contains("AhriOrbReturn") && QMissleClient.IsValid)
            {
                QReturn = null;
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var client = sender as MissileClient;

            if (client == null) return;

            var QMissleClient = client;

            if (!QMissleClient.SpellCaster.IsMe || !QMissleClient.IsValid) return;

            var Name = QMissleClient.SData.Name;

            if (Name.Contains("AhriOrbMissile"))
            {
                Qobj = QMissleClient;
            }

            if (Name.Contains("AhriOrbReturn"))
            {
                QReturn = QMissleClient;
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            KillStealLogic();
            AutoQLogic();

            switch (Variables.Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    ComboLogic();
                    break;
                case OrbwalkingMode.Hybrid:
                    HarassLogic();
                    break;
                case OrbwalkingMode.LaneClear:
                    LaneClearLogic();
                    JungleLogic();
                    break;
            }
        }

        private static void ComboLogic()
        {
            var QTarget = GetTarget(Q.Range, Q.DamageType);
            var Target = GetTarget(900f, DamageType.Magical);

            if (Menu["Q"]["ComboQ"] && Q.IsReady())
            {
                if (CheckTarget(QTarget) && QTarget.IsValidTarget(Q.Range - 50))
                {
                    CastSpell(Q, QTarget);
                }
            }

            if (Menu["W"]["ComboW"] && W.IsReady())
            {
                if (CheckTarget(Target) && W.IsInRange(Target))
                {
                    W.Cast();
                }
            }

            if (Menu["E"]["ComboE"] && E.IsReady() && Target.IsValidTarget(E.Range - 50))
            {
                CastSpell(E, Target);
            }

            if (Menu["R"]["ComboR"] && R.IsReady() && Target.IsValidTarget(900f))
            {
                RLogic(Target);
            }

            if (Menu["ComboIgnite"].GetValue<MenuBool>() && Ignite.IsReady() && Ignite != SpellSlot.Unknown)
            {
                var targetig = GetTarget(IgniteRange, DamageType.True);

                if (CheckTarget(targetig))
                {
                    if (targetig.IsValidTarget(IgniteRange) && targetig.HealthPercent < 20)
                    {
                        Player.Spellbook.CastSpell(Ignite, targetig);
                        return;
                    }
                    if (GetIgniteDamage(targetig) > targetig.Health && targetig.IsValidTarget(IgniteRange))
                    {
                        Player.Spellbook.CastSpell(Ignite, targetig);
                        return;
                    }
                }
            }
        }

        private static void HarassLogic()
        {
            var QTarget = GetTarget(Q.Range, Q.DamageType);
            var WTarget = GetTarget(W.Range, W.DamageType);
            var ETarget = GetTarget(E.Range, E.DamageType);

            if (Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().BValue && Q.IsReady())
            {
                if (Player.ManaPercent >= Menu["Q"]["HarassQ"].GetValue<MenuSliderButton>().SValue)
                {
                    if (CheckTarget(QTarget) && QTarget.IsValidTarget(Q.Range - 50))
                    {
                        CastSpell(Q, QTarget);                            
                    }
                }
            }

            if (Menu["W"]["HarassW"].GetValue<MenuSliderButton>().BValue && W.IsReady())
            {
                if (Player.ManaPercent >= Menu["W"]["HarassW"].GetValue<MenuSliderButton>().SValue)
                {
                    if (CheckTarget(WTarget) && WTarget.IsValidTarget(R.Range))
                    {
                        W.Cast();
                    }
                }
            }

            if (Menu["E"]["HarassE"].GetValue<MenuSliderButton>().BValue && E.IsReady())
            {
                if (Player.ManaPercent >= Menu["E"]["HarassE"].GetValue<MenuSliderButton>().SValue)
                {
                    if (CheckTarget(ETarget) && ETarget.IsValidTarget(E.Range))
                    {
                        CastSpell(E, ETarget);
                    }
                }
            }
        }

        private static void LaneClearLogic()
        {
            var minions = GetMinions(Player.Position, Q.Range);

            if (minions.Count <= 0)
            {
                return;
            }

            if (Menu["Q"]["LaneClear"].GetValue<MenuSliderButton>().BValue && Q.IsReady())
            {
                if (Player.ManaPercent >= Menu["Q"]["LaneClear"].GetValue<MenuSliderButton>().SValue)
                {
                    var QFarm = Q.GetLineFarmLocation(minions, Q.Width);

                    if (QFarm.MinionsHit >= Menu["Q"]["LCMinionsHit"].GetValue<MenuSlider>().Value)
                    {
                        Q.Cast(QFarm.Position);
                    }
                }
            }

            if (!Menu["W"]["LaneClear"].GetValue<MenuSliderButton>().BValue && !W.IsReady())
            {
                if (Player.ManaPercent >= Menu["W"]["LaneClear"].GetValue<MenuSliderButton>().SValue)
                {
                    var WFarm = GetMinions(Player.Position, W.Range);

                    if (Qobj != null || QReturn != null) return;

                    if (WFarm.Count >= 3)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void JungleLogic()
        {
            var mobs = GetMobs(Player.Position, Q.Range, true);
            var mob = mobs.FirstOrDefault();

            if (mobs.Count <= 0)
                return;

            if (Menu["Q"]["JungClear"].GetValue<MenuSliderButton>().BValue && Q.IsReady())
            {
                if (Player.ManaPercent >= Menu["Q"]["JungClear"].GetValue<MenuSliderButton>().SValue)
                {
                    Q.Cast(mob);
                }
            }

            if (Menu["W"]["JungClear"].GetValue<MenuSliderButton>().BValue && W.IsReady())
            {
                if (Player.ManaPercent >= Menu["W"]["JungClear"].GetValue<MenuSliderButton>().SValue)
                {
                    W.Cast(mob);
                    return;
                }
            }
        }

        private static void KillStealLogic()
        {
            foreach (var t in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Q.Range) && e.IsHPBarRendered))
            {
                if (!CheckTarget(t))
                    continue;

                if (Menu["Q"]["KillStealQ"] && Q.IsReady() && t.IsValidTarget(Q.Range - 50) && Q.GetDamage(t) > t.Health)
                {
                    CastSpell(Q, t);
                    return;
                }

                if (Menu["W"]["KillStealW"] && W.IsReady() && t.IsValidTarget(W.Range) && W.GetDamage(t) > t.Health)
                {
                    W.Cast();
                    return;
                }

                if (Menu["E"]["KillStealE"] && E.IsReady() && t.IsValidTarget(E.Range) && E.GetDamage(t) > t.Health)
                {
                    CastSpell(E, t);
                }
            }
        }

        private static void RLogic(Obj_AI_Hero Target)
        {
            if (!CheckTarget(Target) || !Target.IsValidTarget(900f))
            {
                return;
            }

            var DashPos = Vector3.Zero;

            DashPos = Player.ServerPosition.Extend(Game.CursorPos, 450f);

            if ((DashPos.IsWall() && Menu["R"]["RCheck"]) || (DashPos.IsUnderEnemyTurret() && Menu["R"]["RTurret"]) || (Target.Health < GetDamage(Target, false, true, true, true, false) && Target.IsValidTarget(650)))
            {
                return;
            }

            if (Player.HasBuff("AhriTumble"))
            {
                var buffTime = Player.GetBuff("AhriTumble").EndTime;

                if (buffTime - Game.Time <= 3)
                {
                    R.Cast(DashPos);
                }

                if (QReturn != null && QReturn.IsValid)
                {
                    var ReturnPos = QReturn.Position;

                    if (Target.DistanceToPlayer() > ReturnPos.DistanceToPlayer())
                    {
                        var targetdis = Target.Position.Distance(ReturnPos);
                        var QReturnEnd = QReturn.EndPosition;

                        if (!(targetdis < Q.Range)) return;

                        var CastPos = QReturnEnd.Extend(Target.ServerPosition, Target.ServerPosition.Distance(DashPos));

                        if ((CastPos.IsWall() && Menu["R"]["RCheck"]) || Player.ServerPosition.Distance(CastPos) > R.Range || CastPos.CountEnemyHeroesInRange(R.Range) > 2 || (DashPos.IsUnderEnemyTurret() && Menu["R"]["RTurret"]))
                        {
                            return;
                        }
                        R.Cast(CastPos);
                    }
                }
                else
                {
                    if (Q.IsReady() || DashPos.CountEnemyHeroesInRange(R.Range) > 2 || !Target.IsValidTarget(800f))
                        return;

                    if (Game.CursorPos.Distance(Target.Position) > Target.DistanceToPlayer() && Target.IsValidTarget(R.Range))
                    {
                        R.Cast(DashPos);
                    }
                    else if (Game.CursorPos.Distance(Target.Position) < Target.DistanceToPlayer() && !Target.IsValidTarget(R.Range) && Target.IsValidTarget(800f))
                    {
                        R.Cast(DashPos);
                    }
                }
            }
            else
            {
                var AllDamage = GetDamage(Target, false);
                var QDamage = Q.GetDamage(Target) * 2;
                var WDamage = W.GetDamage(Target);
                var RDamage = R.GetDamage(Target) * 3;

                if (!Target.IsValidTarget(800) || !(Target.Distance(DashPos) <= R.Range)) return;

                if (DashPos.CountEnemyHeroesInRange(R.Range) > 2 ||
                    Target.CountAllyHeroesInRange(R.Range) > 2)
                {
                    return;
                }

                if (Target.Health >= AllDamage && Target.Health <= QDamage + WDamage + RDamage)
                {
                    R.Cast(DashPos);
                }
                else if (Target.Health < RDamage + QDamage)
                {
                    R.Cast(DashPos);
                }
                else if (Target.Health < RDamage + WDamage)
                {
                    R.Cast(DashPos);
                }
            }
        }

        private static void AutoQLogic()
        {
            if (!Menu["Q"]["AutoQ"] || !Q.IsReady() || Player.IsUnderEnemyTurret())
            {
                return;
            }

            foreach (var t in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(Q.Range) && !e.HasBuffOfType(BuffType.SpellShield) && !CanMove(e)))
            {
                if (!CheckTarget(t))
                    continue;

                CastSpell(Q, t);
                return;
            }
        }
    }
}