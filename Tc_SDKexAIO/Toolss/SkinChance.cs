namespace Tc_SDKexAIO.Toolss
{

    using LeagueSharp;
    using LeagueSharp.SDK.UI;
    using System;
    using System.Collections.Generic;
    using Config;

    using Menu = LeagueSharp.SDK.UI.Menu;

    internal class SkinChance
    {

        private static Obj_AI_Hero Player => PlaySharp.Player;
        private static Menu Menu => Tools.Menu;

        private static int SkinID;

        #region

        internal static void Init()
        {
            SkinID = Player.BaseSkinId;

            var SkinMenu = Menu.Add(new Menu("SkinChance", "Skin | 造型皮膚"));
            {
                SkinMenu.GetBool("Eanble", "Enabled", false);

                switch (Player.ChampionName)
                {
                    case "Jinx":
                        SkinMenu.Add(new MenuList<string>("SkinName", "Skin Name", Jinx));
                        break;
                    default:
                        SkinMenu.Add(new MenuList<string>("SkinName", "Skin Name", new[] { "Classic", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" }));
                        break;
                }
            }
            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            if (Menu["SkinChance"]["Eanble"].GetValue<MenuBool>())
            {
                Player.SetSkin(Player.ChampionName, Menu["SkinChance"]["SkinName"].GetValue<MenuList>().Index);
            }
            else if (!Menu["SkinChance"]["Eanble"].GetValue<MenuBool>())
            {
                Player.SetSkin(Player.ChampionName, SkinID);
            }
        }

        #endregion

        #region 造型名稱 SkinNama

        private static IEnumerable<string> Aatrox = new[]
        {
            "Classic", "Justicar Aatrox", "Mecha Aatrox", "Sea Hunter Aatrox"
        };

        private static IEnumerable<string> Ahri = new[]
        {
            "Classic", "Dynasty Ahri", "Midnight Ahri", "Foxfire Ahri", "Popstar Ahri", "Challenger Ahri", "Academy Ahri"
        };

        private static IEnumerable<string> Akali = new[]
        {
            "Classic", "Stinger Akali", "Crimson Akali", "All-star Akali", "Nurse Akali", "Blood Moon Akali", "Silverfang Akali", "Headhunter Akali"
        };

        private static IEnumerable<string> Alistar = new[]
        {
            "Classic", "Black Alistar", "Golden Alistar", "Matador Alistar", "Longhorn Alistar", "Unchained Alistar", "Infernal Alistar", "Sweeper Alistar", "Marauder Alistar"
        };

        private static IEnumerable<string> Amumu = new[]
        {
            "Classic", "Pharaoh Amumu", "Vancouver Amumu", "Emumu", "Re-Gifted Amumu", "Almost-Prom King Amumu", "Little Knight Amumu", "Sad Robot Amumu", "Surprise Party Amumu"
        };

        private static IEnumerable<string> Anivia = new[]
        {
            "Classic", "Team Spirit Anivia", "Bird of Prey Anivia", "Noxus Hunter Anivia", "Hextech Anivia", "Blackfrost Anivia", "Prehistoric Anivia"
        };

        private static IEnumerable<string> Annie = new[]
        {
            "Classic", "Goth Annie", "Red Riding Annie", "Annie in Wonderland", "Prom Queen Annie", "Frostfire Annie", "Reverse Annie", "FrankenTibbers Annie", "Panda Annie", "Sweetheart Annie", "Hextech Annie"
        };

        private static IEnumerable<string> Ashe = new[]
        {
            "Classic", "Freljord Ashe", "Sherwood Forest Ashe", "Woad Ashe", "Queen Ashe", "Amethyst Ashe", "Heartseeker Ashe", "Marauder Ashe"
        };

        private static IEnumerable<string> AurelionSol = new[]
        {
            "Classic", "Ashen Lord Aurelion Sol"
        };

        private static IEnumerable<string> Azir = new[]
        {
            "Classic", "Galactic Azir", "Gravelord Azir"
        };

        private static IEnumerable<string> Bard = new[]
        {
            "Classic", "Elderwood Bard", "Snow Day Bard"
        };

        private static IEnumerable<string> Blitzcrank = new[]
        {
            "Classic", "Definitely Not", "Boom Boom", "Rusty", "Goalkeeper", "Piltover Customs", "iBlitzcrank", "Riot", "Battle Boss"
        };

        private static IEnumerable<string> Brand = new[]
        {
            "Classic", "Apocalyptic Brand", "Vandal Brand", "Cryocore Brand", "Zombie Brand", "Spirit Fire Brand"
        };

        private static IEnumerable<string> Braum = new[]
        {
            "Classic", "Dragonslayer Braum", "El Tigre Braum", "Braum Lionheart"
        };

        private static IEnumerable<string> Caitlyn = new[]
        {
            "Classic", "Resistance Caitlyn", "Sheriff Caitlyn", "Safari Caitlyn", "Arctic Warfare Caitlyn", "Officer Caitlyn", "Headhunter Caitlyn", "Lunar Wraith Caitlyn"
        };

        private static IEnumerable<string> Cassiopeia = new[]
        {
            "Classic", "Desperada Cassiopeia", "Siren Cassiopeia", "Mythic Cassiopeia", "Jade Fang Cassiopeia"
        };

        private static IEnumerable<string> ChoGath = new[]
        {
            "Classic", "Nightmare Cho'Gath", "Gentleman Cho'Gath", "Loch Ness Cho'Gath", "Jurassic Cho'Gath", "Battlecast Prime Cho'Gath", "Prehistoric Cho'Gath"
        };

        private static IEnumerable<string> Corki = new[]
        {
            "Classic", "UFO Corki", "Ice Toboggan Corki", "Red Baron Corki", "Hot Rod Corki", "Urfrider Corki", "Dragonwing Corki", "Fnatic Corki"
        };

        private static IEnumerable<string> Darius = new[]
        {
            "Classic", "Lord", "Bioforge", "Woad King", "Dunkmaster", "Academy"
        };

        private static IEnumerable<string> Diana = new[]
        {
            "Classic", "Dark Valkyrie Diana", "Lunar Goddess Diana", "Infernal Diana"
        };

        private static IEnumerable<string> Jinx = new[]
        {
            "Classic", "Mafia Jinx", "Firecracker Jinx", "Slayer Jinx"
        };

        #endregion
    }
}