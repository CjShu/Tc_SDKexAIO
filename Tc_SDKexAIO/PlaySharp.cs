namespace Tc_SDKexAIO
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Config;

    public class PlaySharp
    {

        #region

        public static Menu Menu { get; set; }

        public static Obj_AI_Hero Player { get; set; }

        public static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>();

        public static List<Obj_AI_Hero> Allies = new List<Obj_AI_Hero>();

        public static Spell Q { get; set; }

        public static Spell W { get; set; }

        public static Spell E { get; set; }

        public static Spell R { get; set; }

        #endregion

        #region

        internal static string[] AutoEnableList =
        {
             "Annie", "Ahri", "Akali", "Anivia", "Annie", "Brand", "Cassiopeia", "Diana", "Evelynn", "FiddleSticks", "Fizz", "Gragas", "Heimerdinger", "Karthus",
             "Kassadin", "Katarina", "Kayle", "Kennen", "Leblanc", "Lissandra", "Lux", "Malzahar", "Mordekaiser", "Morgana", "Nidalee", "Orianna",
             "Ryze", "Sion", "Swain", "Syndra", "Teemo", "TwistedFate", "Veigar", "Viktor", "Vladimir", "Xerath", "Ziggs", "Zyra", "Velkoz", "Azir", "Ekko",
             "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "Jayce", "Jinx", "KogMaw", "Lucian", "MasterYi", "MissFortune", "Quinn", "Shaco", "Sivir",
             "Talon", "Tristana", "Twitch", "Urgot", "Varus", "Vayne", "Yasuo", "Zed", "Kindred", "AurelionSol"
        };

        #endregion

        private static void Main(string[] args)
        {
            Bootstrap.Init(args);
            Events.OnLoad += Events_OnLoad;
        }

        private static void Events_OnLoad(object sender, EventArgs e)
        {

            Player = GameObjects.Player;

            foreach (var enemy in GameObjects.EnemyHeroes) { Enemies.Add(enemy); }

            foreach (var ally in GameObjects.AllyHeroes) { Allies.Add(ally); }

            Menu = new Menu("Top Aio", "Top AIO SDKEx", true).Attach();
            Menu.GetSeparator("By: CjShu");
            Menu.GetSeparator("Version : " + Assembly.GetExecutingAssembly().GetName().Version);

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Toolss.Tools.Init();

            switch (Player.ChampionName)
            {
                case "Jinx":
                    Champions.Jinx.Init();
                    break;
                default:
                    break;
            }
        }
    }
}