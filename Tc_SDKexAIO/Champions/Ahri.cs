namespace Tc_SDKexAIO.Champions
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;

    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.UI;

    using System;
    using System.Linq;

    using SharpDX;

    using Common;
    using Config;
    using static Common.Manager;

    using Keys = System.Windows.Forms.Keys;

    internal static class Ahri
    {
        private static Spell Q, W, E, R;

        private static Menu Menu => PlaySharp.ChampionMenu;

        private static SpellSlot Ignite = Player.GetSpellSlot("SummonerDot");

        private static float IgniteRange = 600f;

        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static HpBarDraw HpBarDraw = new HpBarDraw();
        private static GameObject QMissile = null, EMissile = null;
        private static Obj_AI_Hero Qtarget = null;
        private static string MissileName, MissileReturnName;
        private static MissileClient Missile;
        private static Vector3 MissileEndPos;


        internal static void Init()
        {
            Q = new Spell(SpellSlot.Q, 870f).SetSkillshot(0.25f, 90f, 1550f, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 580f);
            E = new Spell(SpellSlot.E, 920f).SetSkillshot(0.25f, 70f, 1550f, true, SkillshotType.SkillshotLine);
            R = new Spell(SpellSlot.R, 600f);

            var QMenu = Menu.Add(new Menu("Q", "Q.Set"));
            {
                QMenu.Add(new MenuSeparator("Mode", "模式"));
                QMenu.Add(new MenuBool("AutoQ", "自動 Q", true));
                QMenu.Add(new MenuBool("Aim", "Q 目標", true));
                QMenu.Add(new MenuBool("AimQ", "Q 返回", true));
                QMenu.Add(new MenuSeparator("LaneClearMode", "清線 模式"));
                QMenu.Add(new MenuSliderButton("LaneClear", "清線 Q | 最低魔力 = ", 40, 0, 100, true));
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
                WMenu.Add(new MenuBool("AutoW", "自動 W", true));
                WMenu.Add(new MenuSeparator("LaneClearMode", "清線 模式"));
                WMenu.Add(new MenuSliderButton("LaneClear", "清線 W | 最低魔力 = ", 40, 0, 100, true));
                WMenu.Add(new MenuSliderButton("JungClear", "清野 W | 最低魔力 = ", 40, 0, 100, true));
                WMenu.Add(new MenuSeparator("HarassMode", "騷擾 模式"));
                WMenu.Add(new MenuSliderButton("HarassW", "騷擾 W | 最低魔力 = ", 40, 0, 100, false));
            }

            var EMenu = Menu.Add(new Menu("E", "E.Set"));
            {
                EMenu.Add(new MenuSeparator("Mode", "模式"));
                EMenu.Add(new MenuBool("AutoE", "自動 E", true));
                EMenu.Add(new MenuSeparator("HarassMode", "騷擾 模式"));
                EMenu.Add(new MenuSliderButton("HarassE", "騷擾 E | 最低魔力 = ", 40, 0, 100, true));
                EMenu.Add(new MenuBool("NotTarget", "禁止 E 特定對象", true));
                var EList = EMenu.Add(new Menu("EList", "禁止 E 目標名單"));
                {
                    if (GameObjects.EnemyHeroes.Any())
                    {
                        GameObjects.EnemyHeroes.ForEach(i => EList.Add(new MenuBool(i.ChampionName.ToLower(), i.ChampionName, true)));
                    }
                }
            }

            var RMenu = Menu.Add(new Menu("R", "R.Set"));
            {
                RMenu.Add(new MenuSeparator("Mode", "模式"));
                RMenu.Add(new MenuBool("AutoR", "自動 R", true));
                RMenu.Add(new MenuBool("ComboR", "連招 R", true));
                RMenu.Add(new MenuBool("KillstealR", "可擊殺目標 R", true));
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
                DrawMenu.Add(new MenuBool("DrawHelperQ", "顯示 Q 技能施放路徑", false));
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

        private static void OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
