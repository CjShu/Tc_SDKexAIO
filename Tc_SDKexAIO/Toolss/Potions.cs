namespace Tc_SDKexAIO.Toolss
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;

    using System;
    using System.Linq;

    using Config;

    using Menu = LeagueSharp.SDK.UI.Menu;

    internal static class Potions
    {

        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static Menu Menu => Tools.Menu;

        public static void Init()
        {
            var PotionsMenu = Menu.Add(new Menu("Potions", "Potions"));
            {
                PotionsMenu.GetSeparator("Health Potion");
                PotionsMenu.GetSliderButton("HealthPotion", "Player HealthPercent = ", 50, 35, 80, false);
                PotionsMenu.GetSeparator("Corrupting Potion");
                PotionsMenu.GetSliderButton("CorruptingPotion", "Player HealthPercent = ", 50, 35, 80, false);
                PotionsMenu.GetSeparator("Refillable Potion");
                PotionsMenu.GetSliderButton("RefillablePotion", "Player HealthPercent = ", 50, 35, 80, false);
                PotionsMenu.GetSeparator("Hunter's Potion");
                PotionsMenu.GetSliderButton("HuntersPotion", "Player HealthPercent = ", 50, 35, 80, false);
            }
            Game.OnUpdate += OnUpdate;


        }

        private static void OnUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
