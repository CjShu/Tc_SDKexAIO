namespace Tc_SDKexAIO.Toolss
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using SharpDX;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;


    internal class AutoWard
    {

        private static Menu Menu => Tools.Menu;

        public static List<ChampionInfo> ChampionInfoList = new List<ChampionInfo>();

        private static Vector3 EnemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy).Position;

        private static Obj_AI_Hero Player = PlaySharp.Player, Vayne = null;

        private static bool Rengar = false;
     
        public static List<HiddenObj> HiddenObjList = new List<HiddenObj>();

        private static Items.Item Vision, OracleLens, WardN, TrinketN, SightStone, Oasis, Equinox, Watchers, FarsightOrb, ScryingOrb;


        internal static void Init()
        {

            Player = GameObjects.Player;
            InitItem();

            var AutoMenu = Menu.Add(new Menu("AutoWard", "Auto.Ward(自動守衛)"));
            {

            }
        }

        private static void InitItem()
        {
            Vision = new Items.Item(ItemId.Vision_Ward, 550f);
            OracleLens = new Items.Item(ItemId.Oracles_Lens_Trinket, 550f);
            WardN = new Items.Item(ItemId.Stealth_Ward, 600f);
            TrinketN = new Items.Item(ItemId.Warding_Totem_Trinket, 600f);
            SightStone = new Items.Item(ItemId.Sightstone, 600f);
            FarsightOrb = new Items.Item(ItemId.Farsight_Orb_Trinket, 4000f);
            ScryingOrb = new Items.Item(ItemId.Scrying_Orb_Trinket, 3500f);
            Oasis = new Items.Item(2302, 600f);
            Equinox = new Items.Item(2303, 600f);
            Watchers = new Items.Item(2301, 600f);
        }
        
    }


    internal class ChampionInfo
    {

        public int NetworkId { get; set; }
        public Vector3 LastVisablePos { get; set; }
        public float LastVisableTime { get; set; }
        public Vector3 PredictedPos { get; set; }
        public float StartRecallTime { get; set; }
        public float AbortRecallTime { get; set; }
        public float FinishRecallTime { get; set; }


        public ChampionInfo()
        {
            LastVisableTime = Game.Time;
            StartRecallTime = 0;
            AbortRecallTime = 0;
            FinishRecallTime = 0;
        }
    }


    internal class HiddenObj
    {
        public int type;
        public float endTime { get; set; }
        public Vector3 pos { get; set; }
    }
}