namespace Tc_SDKexAIO.Toolss
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using System;

    using Common;
    using Config;

    internal class Offensive
    {
        private static Obj_AI_Hero Player => PlaySharp.Player;

        private static Menu Menu => Tools.Menu;

        internal static void Init()
        {
            var OffMenu = Menu.Add(new Menu("Offensive", "Offensive"));
            {
                OffMenu.GetSeparator("Youmuus Mode");
                OffMenu.Add(new MenuBool("Youmuus", "Use Youmuu", true));
                OffMenu.GetSlider("Youmuus.s", "Youmuus enemy  HP Min >=", 70, 0, 100);
                OffMenu.GetSeparator("Cutlass Mode");
                OffMenu.Add(new MenuBool("Cutlass", "Use Cutlass", true));
                OffMenu.GetSlider("Cutlass.s", "Cutlass enemy HP Min >=", 70, 0, 100);
                OffMenu.GetSeparator("Botrk Mode");
                OffMenu.Add(new MenuBool("Botrk", "Use Botrk", true));
                OffMenu.GetSlider("Botrk.s", "Botrk enemy HP Min >=", 70, 0, 100);
                OffMenu.GetSeparator("Combo Mode");
                OffMenu.Add(new MenuBool("Combo", "Combo Use", true));
            }
            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            if (Manager.Combo && Menu["Offensive"]["Combo"])
            {
                var t = Variables.TargetSelector.GetTarget(600, DamageType.Physical);

                if (t != null && t.IsHPBarRendered)
                {
                    if (Menu["Offensive"]["Youmuus"] && Items.HasItem(3142)
                        && t.HealthPercent >= Menu["Offensive"]["Youmuus.s"].GetValue<MenuSlider>().Value
                        && t.IsValidTarget(Player.GetRealAutoAttackRange() + 150))
                    {
                        Items.UseItem(3142, t);
                    }

                    if (Menu["Offensive"]["Cutlass"] && Items.HasItem(3144)
                        && t.HealthPercent >= Menu["Offensive"]["Cutlass.s"].GetValue<MenuSlider>().Value
                        && t.IsValidTarget(Player.GetRealAutoAttackRange()))
                    {
                        Items.UseItem(3144, t);
                    }

                    if (Menu["Offensive"]["Botrk"] && Items.HasItem(3153)
                        && (t.HealthPercent >= Menu["Offensive"]["Botrk.s"].GetValue<MenuSlider>().Value
                        && Player.HealthPercent < 70) && t.IsValidTarget(Player.GetRealAutoAttackRange()))
                    {
                        Items.UseItem(3153, t);
                    }
                }
            }
        }
    }
}