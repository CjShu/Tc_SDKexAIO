namespace Tc_SDKexAIO.Common
{

    using LeagueSharp;
    using LeagueSharp.SDK;

    using SharpDX;

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    
    public class TcCommon
    {
        private static Obj_AI_Hero Player => ObjectManager.Player;
        private static int LastAATick = Variables.TickCount;
        public static bool YasuoInGame = false;
        public static bool Thunderlord = false;
        public static bool blockMove = false, blockAttack = false, blockSpells = false;

        private static List<Obj_AI_Hero> ChampionList = new List<Obj_AI_Hero>();
        private static YasuoWall yasuoWall = new YasuoWall();

        static TcCommon()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                ChampionList.Add(hero);
                if (hero.IsEnemy && hero.ChampionName == "Yasuo")
                    YasuoInGame = true;
            }

            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            Spellbook.OnCastSpell += OnCastSpell;
            Game.OnWndProc += OnWndProc;
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
                return true;

            return false;
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

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (blockSpells)
                args.Process = false;
        }

        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (blockMove && !args.IsAttackMove)
                args.Process = false;

            if (blockAttack && args.IsAttackMove)
                args.Process = false;
        }
    }
}