namespace Tc_SDKexAIO.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.SDK;
    using SharpDX;
    using Common;

    /// <summary>
    /// (This Part From SebbyLib)
    /// </summary>
   internal static class TCommon
   {
        private static readonly List<UnitIncomingDamage> IncomingDamageList = new List<UnitIncomingDamage>();
        private static readonly List<Obj_AI_Hero> ChampionList = new List<Obj_AI_Hero>();
        public static readonly Dictionary<int, List<Vector2>> StoredPaths = new Dictionary<int, List<Vector2>>();
        public static readonly Dictionary<int, int> StoredTick = new Dictionary<int, int>();

        private static Obj_AI_Hero Player => PlaySharp.Player;

        static TCommon()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                ChampionList.Add(hero);
            }

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            var time = Game.Time - 2;
            IncomingDamageList.RemoveAll(damage => time < damage.Time);
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData == null)
            {
                return;
            }

            var targed = args.Target as Obj_AI_Base;

            if (targed != null)
            {
                if (targed.Type == GameObjectType.obj_AI_Hero && targed.Team != sender.Team && sender.IsMelee)
                {
                    IncomingDamageList.Add(new UnitIncomingDamage
                    {
                        Damage = (sender as Obj_AI_Hero).GetSpellDamage(targed, args.Slot),
                        TargetNetworkId = args.Target.NetworkId,
                        Time = Game.Time,
                        Skillshot = false
                    });
                }
            }
            else
            {
                foreach (var champion in ChampionList.Where(champion => !champion.IsDead && champion.IsVisible && champion.Team != sender.Team && champion.Distance(sender) < 2000))
                {
                    if (CanHitSkillShot(champion, args))
                    {
                        IncomingDamageList.Add(new UnitIncomingDamage
                        {
                            Damage = champion.GetSpellDamage(targed, args.Slot),
                            TargetNetworkId = champion.NetworkId,
                            Time = Game.Time,
                            Skillshot = true
                        });
                    }
                }
            }
        }

        public static bool CanHitSkillShot(Obj_AI_Base target, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target == null && target.IsValidTarget(1000))
            {
                var pred = Movement.GetPrediction(target, 0.25f).CastPosition;

                if (args.SData.LineWidth > 0)
                {
                    var powCalc = Math.Pow(args.SData.LineWidth + target.BoundingRadius, 2);

                    if (pred.ToVector2().Distance(args.End.To2D(), args.Start.To2D(), true, true) <= powCalc ||
                        target.ServerPosition.To2D().Distance(args.End.To2D(), args.Start.To2D(), true, true) <= powCalc)
                    {
                        return true;
                    }
                }
                else if (target.Distance(args.End) < 50 + target.BoundingRadius || pred.Distance(args.End) < 50 + target.BoundingRadius)
                {
                    return true;
                }
            }
            return false;
        }

        public static Vector3 GetTrapPos(float range)
        {
            foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValid && enemy.Distance(Player.ServerPosition) < range && (enemy.HasBuff("zhonyasringshield") || enemy.HasBuff("BardRStasis"))))
            {
                return enemy.Position;
            }

            foreach (var obj in ObjectManager.Get<Obj_GeneralParticleEmitter>().Where(obj => obj.IsValid && obj.Position.Distance(Player.Position) < range))
            {
                var name = obj.Name.ToLower();

                if (name.Contains("GateMarker_red.troy".ToLower()) || name.Contains("global_ss_teleport_target_red.troy".ToLower())
                    || name.Contains("R_indicator_red.troy".ToLower()))
                    return obj.Position;
            }

            return Vector3.Zero;
        }
    }

    public class UnitIncomingDamage
    {
        public int TargetNetworkId { get; set; }
        public float Time { get; set; }
        public double Damage { get; set; }
        public bool Skillshot { get; set; }
    }
}