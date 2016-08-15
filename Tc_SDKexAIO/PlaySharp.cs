namespace Tc_SDKexAIO
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using System;
    using System.Linq;
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

        public static Spell E { get; set; }

        #endregion

        #region 自動啟動列表英雄名單

        public static string[] AutoEnableList =
        {
             "Annie", "Ahri", "Akali", "Anivia", "Annie", "Brand", "Cassiopeia", "Diana", "Evelynn", "FiddleSticks", "Fizz", "Gragas", "Heimerdinger", "Karthus",
             "Kassadin", "Katarina", "Kayle", "Kennen", "Leblanc", "Lissandra", "Lux", "Malzahar", "Mordekaiser", "Morgana", "Nidalee", "Orianna",
             "Ryze", "Sion", "Swain", "Syndra", "Teemo", "TwistedFate", "Veigar", "Viktor", "Vladimir", "Xerath", "Ziggs", "Zyra", "Velkoz", "Azir", "Ekko",
             "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "Jayce", "Jinx", "KogMaw", "Lucian", "MasterYi", "MissFortune", "Quinn", "Shaco", "Sivir",
             "Talon", "Tristana", "Twitch", "Urgot", "Varus", "Vayne", "Yasuo", "Zed", "Kindred", "AurelionSol", "Kled"
        };

        #endregion

        #region 支持英雄

        private static string[] SupList =
        {
            "Jinx", "Jhin", "Teemo", "Ezreal", //"Diana"
        };

        #endregion

        static void Main(string[] args)
        {
            Bootstrap.Init(args);
            Events.OnLoad += Events_OnLoad;
        }

        private static void Events_OnLoad(object sender, EventArgs args)
        {

            Player = GameObjects.Player;

            foreach (var enemy in GameObjects.EnemyHeroes) { Enemies.Add(enemy); }

            foreach (var ally in GameObjects.AllyHeroes) { Allies.Add(ally); }

            if (!SupList.Contains(GameObjects.Player.ChampionName))
            {
                Write(GameObjects.Player.ChampionName + "Not Support!");
                {
                    return;
                }
            }

            Write(GameObjects.Player.ChampionName + "Load OK!");

            Menu = new Menu("TcAioSDK", "Tc AIO SDKEx", true).Attach();
            Menu.GetSeparator("By: CjShu");
            Menu.Add(new MenuSeparator("Version", "Version : " + Assembly.GetExecutingAssembly().GetName().Version));

            Toolss.Tools.Init();

            switch (Player.ChampionName)
            {
                case "Jinx":
                    Champions.Jinx.Init();
                    break;
                case "Jhin":
                    Champions.Jhin.Init();
                    break;
                case "Teemo":
                    Champions.Teemo.Init();
                    break;
                case "Ezreal":
                    Champions.Ezreal.Init();
                    break;
               // case "Diana":
               //     Champions.Diana.Init();
               //     break;
                default:
                    break;
            }
        }

        public static void Write(string mdg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Tc_SDKexAIO :" + mdg);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}