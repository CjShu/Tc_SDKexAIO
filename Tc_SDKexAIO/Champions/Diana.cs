namespace Tc_SDKexAIO.Champions
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using System;
    using System.Linq;
    using System.Drawing;

    using Color = System.Drawing.Color;

    using SharpDX;

    using Tc_SDKexAIO.Common;
    using static Common.Manager;

    using Config;

    internal static class Diana
    {

        private static Spell Q, W, E, R, R1;
        private static Menu Menu => PlaySharp.Menu;
        private static Obj_AI_Hero Player => PlaySharp.Player;

        private static HpBarDraw HpBarDraw = new HpBarDraw();

        private static SpellSlot Ignite = Player.GetSpellSlot("SummonerDot");

        private static float IgniteRange = 600f;

        private static float SwitchTime = Game.Time;

        private static int LastR;

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 850).SetSkillshot(0.25f, 150f, 1400f, false, SkillshotType.SkillshotCircle);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 450);
            R = new Spell(SpellSlot.R, 825);

            var QMenu = Menu.Add(new Menu("Q", "Q.Set"));
            {
                QMenu.GetSeparator("Combo Mobe");
                QMenu.Add(new MenuBool("ComboQ", "Combo Q", true));
                QMenu.GetSeparator("Harass Mobe");
                QMenu.Add(new MenuBool("HarassQ", "Harass Q", true));
                QMenu.Add(new MenuSlider("HarassQMana", "Harass Q Min Mana >= ", 40, 0, 100));
                QMenu.GetSeparator("LaneClear Mobe");
                QMenu.Add(new MenuBool("LaneClearQ", "LaneClear Q", true));
                QMenu.Add(new MenuBool("JungleQ", "Jungle Q", true));
                QMenu.Add(new MenuSlider("LaneClearQMana", "LaneClear Q Min Mana >= ", 40, 0, 100));
                QMenu.Add(new MenuSlider("LaneClearMinMinions", "LaneClear Q Min Minions", 3, 1, 5));
                var QList = QMenu.Add(new Menu("QList", "Q List"));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => QList.Add(new MenuBool(i.ChampionName.ToLower(), i.ChampionName, !PlaySharp.AutoEnableList.Contains(i.ChampionName))));
                    }
                }
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set"));
            {
                WMenu.GetSeparator("Combo Mobe");
                WMenu.Add(new MenuBool("ComboW", "Combo W", true));
                WMenu.GetSeparator("Harass Mobe");
                WMenu.Add(new MenuBool("HarassW", "Harass W", true));
                WMenu.GetSeparator("LaneClear Mobe");
                WMenu.Add(new MenuBool("LaneClearW", "LaneClear W", true));
                WMenu.Add(new MenuBool("JungleW", "Jungle W", true));
                WMenu.Add(new MenuSlider("WMinMana", "W Spell Min Mana", 40, 0, 100));
                WMenu.Add(new MenuSeparator("MISC", "    "));
                WMenu.Add(new MenuBool("AntiMelee", "Anti Melee Auto W", true));
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set"));
            {
                EMenu.GetSeparator("Combo Mobe");
                EMenu.Add(new MenuBool("ComboE", "Combo E", true));
                EMenu.GetSeparator("Harass Mobe");
                EMenu.Add(new MenuBool("HarassE", "Harass E", true));
                EMenu.Add(new MenuSlider("HarassEMana", "Harass E Min Mana", 40, 0, 100));
                EMenu.GetSeparator("Misc Mobe");
                EMenu.Add(new MenuBool("InterruptE", "Use E Interrupt Enemy Spell", true));
                EMenu.Add(new MenuBool("GapcloserE", "Use E Gapcloser", true));
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set"));
            {
                RMenu.GetSeparator("Combo Mobe");
                RMenu.Add(new MenuBool("ComboR", "Combo R", true));
                RMenu.Add(new MenuBool("ComboR2", "Combo R2", true));
                RMenu.Add(new MenuSlider("RLimitation", "Use R2 Nearby Enemy Counts Kill >= ", 2, 1, 5));
                RMenu.GetSeparator("R or R2 Mobe");
                RMenu.GetList("ComboRMobe", "Combo R Mobe", new[] { "RQ", "QR", "RQR" }, 1);
                RMenu.Add(new MenuSlider("ComboRMisayaMinRange", "Combo RQ Min Range", Convert.ToInt32(R.Range * 0.8), 0, Convert.ToInt32(R.Range)));
                RMenu.GetSeparator("Misc Mobe");
                RMenu.Add(new MenuSlider("PreventMeMinUseR", "Prevent Me Hp Min Use R >= ", 20));
                RMenu.Add(new MenuBool("RPreventUnderTower", "R Prevent Under Tower Use R", true));
                RMenu.Add(new MenuBool("RAD", "Battle priority Use R AD Hero", true));
                RMenu.Add(new MenuKeyBind("RQQRKey", "If Can kill Use R Q Q R Key", System.Windows.Forms.Keys.T, KeyBindType.Press));
            }

            var FleeMenu = Menu.Add(new Menu("Flee", "Flee.Set"));
            {
                FleeMenu.GetSeparator("Flee Mobe");
                FleeMenu.Add(new MenuBool("FleeEnable", "Enable Flee", true));
                FleeMenu.Add(new MenuKeyBind("FleeKey", "Use R Q R Flee Key", System.Windows.Forms.Keys.Z, KeyBindType.Press));
                FleeMenu.Add(new MenuKeyBind("FleeKey2", "Use R Flee Key", System.Windows.Forms.Keys.A, KeyBindType.Press));
            }

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.Add(new MenuBool("Q", "Q Range"));
                DrawMenu.Add(new MenuBool("E", "E Range"));
                DrawMenu.Add(new MenuBool("R", "R Range"));
                DrawMenu.Add(new MenuBool("DrawRQ", "Draw RQ Range", true));
                DrawMenu.Add(new MenuBool("DrawDamage", "Draw Combo Damage", true));
            }

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            Events.OnDash += OnDash;
            Events.OnGapCloser += OnGapCloser;
            Events.OnInterruptableTarget += OnInterruptableTarget;
        }

        private static void OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnDash(object sender, Events.DashArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnEndScene(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnDraw(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}