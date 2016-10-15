namespace Tc_SDKexAIO.Champions
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Enumerations;

    using System;
    using System.Linq;

    using SharpDX;

    using Common;
    using Config;

    using static Common.Manager;

    internal static class Sivir
    {

        private static Menu Menu => PlaySharp.ChampionMenu;
        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static Spell Q, Qc, W, E, R;
        private static HpBarDraw HpBarDraw = new HpBarDraw();

        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 1200f).SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);
            Qc = new Spell(SpellSlot.Q, 1200f).SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, float.MaxValue);
            E = new Spell(SpellSlot.E, float.MaxValue);
            R = new Spell(SpellSlot.R, 25000f);

            var QMenu = Menu.Add(new Menu("Q", "Q.Set"));
            {
                QMenu.GetSeparator("模式");
                QMenu.Add(new MenuBool("ComboQ", "連招 Q", true));
                QMenu.Add(new MenuBool("KillStealQ", "可擊殺 Q", true));
                QMenu.Add(new MenuSliderButton("LaneClear", "清線 Q | 最低魔力值 = ", 40, 0, 100, true));
                QMenu.Add(new MenuSlider("QMin", "清線 Q 最低命中小兵數量 = ", 3, 2, 5));
                QMenu.Add(new MenuSliderButton("JungleClear", "清野 Q | 最低魔力值 = ", 30, 0, 100, true));
                QMenu.Add(new MenuSliderButton("Harass", "騷擾 Q | 最低魔力值 = ", 40, 0, 100, false));
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set"));
            {
                WMenu.GetSeparator("模式");
                WMenu.Add(new MenuBool("ComboW", "連招 W", true));
                WMenu.Add(new MenuSliderButton("LaneClear", "清線 W | 最低魔力值 = ", 40, 0, 100, true));
                WMenu.Add(new MenuSliderButton("JungleClear", "清野 W | 最低魔力值 = ", 40, 0, 100, true));
                WMenu.Add(new MenuSliderButton("HarassW", "騷擾 W | 最低魔力值 = ", 30, 40, 100, true));
                WMenu.GetSeparator("其他 功能");
                WMenu.Add(new MenuBool("WTurret", "塔下 使用 W", true));
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set"));
            {
                EMenu.GetSeparator("模式");
                EMenu.Add(new MenuBool("AutoE", "自動 E", true));
                EMenu.Add(new MenuBool("AGC", "反突進 E", true));
                EMenu.Add(new MenuSlider("EDmg", "血量低於 多少使用E =", 0, 90, 100));
                EMenu.GetSeparator("自動隔檔技能模式");
                EMenu.Add(new MenuBool("Enable", "啟動", true));
                if (GameObjects.EnemyHeroes.Any())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            var spell = enemy.Spellbook.Spells[i];

                            if (spell.SData.TargettingType != SpellDataTargetType.Self && spell.SData.TargettingType != SpellDataTargetType.SelfAndUnit)
                            {
                                var targetMenu = EMenu.Add(new Menu(enemy.ChampionName.ToLower(), enemy.ChampionName));

                                if (spell.SData.TargettingType == SpellDataTargetType.Unit)
                                {
                                    targetMenu.Add(new MenuBool("spell" + spell.SData.Name, spell.Name, true));                               
                                }
                                else
                                {
                                    targetMenu.Add(new MenuBool("spell" + spell.SData.Name, spell.Name, false));
                                }
                            }
                        }
                    }
                }              
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set"));
            {
                RMenu.GetSeparator("自動 模式");
                RMenu.Add(new MenuBool("AutoR", "自動 R", true));
                RMenu.Add(new MenuSlider("RMin", "如果目標 數量多少 開啟自動 R = ", 3, 1, 5));
                RMenu.GetSeparator("連招 模式");
                RMenu.Add(new MenuBool("ComboR", "連招 R", true));
                RMenu.Add(new MenuSlider("ComboRMin", "如果目標 數量多少 開起連招 R = ", 2, 1, 5));
            }

            var DrawMenu = Menu.Add(new Menu("Draw", "Draw"));
            {
                DrawMenu.Add(new MenuBool("Q", "Q 範圍"));
                DrawMenu.Add(new MenuBool("Damage", "顯示連招傷害(青色)", true));
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "OK! :)");

            Game.OnUpdate += OnUpdate;
            Variables.Orbwalker.OnAction += OnAction;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Events.OnGapCloser += OnGapCloser;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;

        }

        private static void OnEndScene(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnDraw(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
