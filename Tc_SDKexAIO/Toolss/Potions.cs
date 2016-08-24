namespace Tc_SDKexAIO.Toolss
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using System;
    using System.Linq;

    using Config;

    using Menu = LeagueSharp.SDK.UI.Menu;

    using System.Collections.Generic;

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
            if (Player.IsDead || Player.InFountain())
            {
                return;
            }
        }

        #region Item

        static void CastHpPotion()
        {
            if (Items.HasItem(2003))
                Items.UseItem(2003);
        }

        static void CastBiscuit()
        {
            if (Items.HasItem(2009))
                Items.UseItem(2009);
        }

        static void CastBiscuit2()
        {
            if (Items.HasItem(2010))
                Items.UseItem(2010);
        }

        static void CastRefillable()
        {
            if (Items.HasItem(2031))
                Items.UseItem(2031);
        }

        static void CastHunter()
        {
            if (Items.HasItem(2032))
                Items.UseItem(2032);
        }

        static void CastCorrupting()
        {
            if (Items.HasItem(2033))
                Items.UseItem(2033);
        }

        #endregion
    }
}