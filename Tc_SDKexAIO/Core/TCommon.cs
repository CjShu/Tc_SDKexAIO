namespace Tc_SDKexAIO.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.SDK;
    using SharpDX;

    /// <summary>
    /// (This Part From SebbyLib)
    /// </summary>
   internal static class TCommon
   {
        private static Obj_AI_Hero Player => PlaySharp.Player;

        private static int LastAATick = Variables.TickCount;
        public static bool YasuoInGame = false;
        public static bool Thunderlord = false;
        public static bool blockMove = false, blockAttack = false, blockSpells = false;

        private static List<UnitIncomingDamage> IncomingDamageList = new List<UnitIncomingDamage>();
        private static List<Obj_AI_Hero> ChampionList = new List<Obj_AI_Hero>();
        private static YasuoWall yasuoWall = new YasuoWall();

        static TCommon()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                ChampionList.Add(hero);
                if (hero.IsEnemy && hero.ChampionName == "Yasuo")
                    YasuoInGame = true;
            }
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            Game.OnUpdate += OnUpdate;
            Game.OnWndProc += OnWndProc;
        }

        public static double GetIncomingDamage(Obj_AI_Hero target, float time = 0.5f, bool skillshots = true)
        {
            double totalDamage = 0;

            foreach (var damage in IncomingDamageList.Where(damage => damage.TargetNetworkId == target.NetworkId && Game.Time - time < damage.Time))
            {
                if (skillshots)
                {
                    totalDamage += damage.Damage;
                }
                else
                {
                    if (!damage.Skillshot)
                        totalDamage += damage.Damage;
                }
            }

            return totalDamage;
        }

        public static bool CollisionYasuo(Vector3 from, Vector3 to)
        {
            if (!YasuoInGame)
                return false;

            if (Game.Time - yasuoWall.CastTime > 4)
                return false;

            var level = yasuoWall.WallLvl;
            var wallWidth = (350 + 50 * level);
            var wallDirection = (yasuoWall.CastPosition.ToVector2() - yasuoWall.YasuoPosition.ToVector2()).Normalized().Perpendicular();
            var wallStart = yasuoWall.CastPosition.ToVector2() + wallWidth / 2f * wallDirection;
            var wallEnd = wallStart - wallWidth * wallDirection;

            if (wallStart.Intersection(wallEnd, to.ToVector2(), from.ToVector2()).Intersects)
            {
                return true;
            }
            return false;

        }

        public static List<Vector3> CirclePoints(float CircleLineSegmentN, float radius, Vector3 position)
        {
            List<Vector3> points = new List<Vector3>();
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
                points.Add(point);
            }
            return points;
        }

        private static void OnWndProc(WndEventArgs args)
        {
            if (args.Msg == 123 && blockMove)
            {
                blockMove = false;
                blockAttack = false;
                Variables.Orbwalker.AttackState = true;
                Variables.Orbwalker.MovementState = true;
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            float time = Game.Time - 2;
            IncomingDamageList.RemoveAll(damage => time < damage.Time);
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (blockSpells)
            {
                args.Process = false;
            }
        }

        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (blockMove && args.Order != GameObjectOrder.AttackUnit)
            {
                args.Process = false;
            }
            if (blockAttack && args.Order == GameObjectOrder.AttackUnit)
            {
                args.Process = false;
            }
        }

    }

   internal class UnitIncomingDamage
    {
        public int TargetNetworkId { get; set; }
        public float Time { get; set; }
        public double Damage { get; set; }
        public bool Skillshot { get; set; }
    }

   internal class YasuoWall
    {
        public Vector3 YasuoPosition { get; set; }
        public float CastTime { get; set; }
        public Vector3 CastPosition { get; set; }
        public float WallLvl { get; set; }

        public YasuoWall()
        {
            CastTime = 0;
        }
    }
}