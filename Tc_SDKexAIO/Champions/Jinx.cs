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

    using Menu = LeagueSharp.SDK.UI.Menu;
    using Geometry = Tc_SDKexAIO.Common.Geometry;

    internal static class Jinx
    {
        private static Spell Q, Q2, W, E, R;

        private static Menu Menu => PlaySharp.Menu;
        private static Obj_AI_Hero Player => PlaySharp.Player;

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
                QMenu.GetBool("comboQ", "ComnoQ");
                QMenu.GetSliderButton("harassQ", "Harass Q  Min Mana > =", 50, 0, 99);
                QMenu.GetSliderButton("clear", "LaneClearQ Min Mana > =", 50, 0, 99);
                QMenu.GetBool("blockQ", "Min Mana Ban BigGun(20)", false);
            }

            var WMenu = Menu.Add(new Menu("W", "W.Set | W 設定"));
            {
                WMenu.GetBool("comboW", "ComnoW");
                WMenu.GetBool("killsteal", "killstealW");
                WMenu.GetBool("AutoW", "Auto W", false);
                WMenu.GetSliderButton("harassw", "Harass W Min Mana > =", 40, 0, 70);
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
                DrawMenu.GetBool("Q", "Q Range", false);
                DrawMenu.GetBool("W", "W Range", false);
                DrawMenu.GetBool("E", "E Range", false);
                DrawMenu.GetBool("R", "R Range", false);
                DrawMenu.GetBool("Draw.Enable", "Enable", false);
                DrawMenu.GetBool("DrawKSEnemy", "Killable Enemy Notification", false);
                DrawMenu.GetBool("DrawKillableEnemyMini", "Killable Enemy [Mini Map]", false);
                DrawMenu.GetBool("DrawDamge", "Draw Combo Damage", false);
                DrawMenu.GetList("DrawBuffs", "Show Red/Blue Time Circle", new[] { "Off", "Blue Buff", "Red Buff", "Both" });
            }

            PlaySharp.Write(GameObjects.Player.ChampionName + "Jinx OK! :)");

            Game.OnUpdate += OnUpdate;
            Variables.Orbwalker.OnAction += OnAction;
            Events.OnGapCloser += OnGapCloser;
            Events.OnDash += OnDash;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnEndScene += OnEndScene;
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDash(object sender, Events.DashArgs args)
        {
            throw new NotImplementedException();
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
            DraBuff();

        }

        private static void OnAction(object sender, OrbwalkingActionArgs args)
        {
            throw new NotImplementedException();
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
            
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Menu["Draw"]["Draw.Enable"])
            {
                return;
            }

            foreach (var t in GameObjects.EnemyHeroes.Where(e => !e.IsDead && e.Health < GetDamage(e)))
            {
                HpBarDraw.DrawText(HpBarDraw.TextStatus, "Can Kill", (int)t.HPBarPosition.X + 145, (int)t.HPBarPosition.Y + 5, SharpDX.Color.Red);
            }
            DrawSpell();
            DrawBuffs();
            DrawKillableEnemy();
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

            var DrawQ = Menu["Draw"]["Q"].GetValue<MenuBool>();
            var DrawW = Menu["Draw"]["W"].GetValue<MenuBool>();
            var DrawE = Menu["Draw"]["E"].GetValue<MenuBool>();

            if (DrawQ && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, AARange, Q.IsReady() ? DrawQ : Color.LightBlue, Q.IsReady() ? 5 : 1);
            }
            if (DrawW && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? DrawW : Color.LightCyan, W.IsReady() ? 5 : 1);
            }
            if (DrawE && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? DrawE : Color.LightGray, E.IsReady() ? 5 : 1);
            }
        }

        private static void DrawBuffs()
        {



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
