namespace Tc_SDKexAIO.Common
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using System.Collections.Generic;
    using System.Linq;
    using static Manager;
    
    public static class Extensions
    {
        public static Obj_AI_Hero Player => PlaySharp.Player;
        public static List<UnitIncomingDamage> IncomingDamageList = new List<UnitIncomingDamage>();

        public static bool IfEnemyInGlass()
        {
            foreach (var hero in GameObjects.Get<Obj_AI_Hero>())
            {
                if (NavMesh.IsWallOfGrass(hero.ServerPosition, 10) && GameObjects.Player.ServerPosition.Distance(hero.ServerPosition) < 650)
                    return true;
            }
            return false;
        }

        public static bool IsCanCastUlt(this Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity) || target.HasBuffOfType(BuffType.SpellImmunity) || target.IsZombie || target.IsInvulnerable || target.HasBuffOfType(BuffType.Invulnerability) || target.HasBuffOfType(BuffType.SpellShield) || target.Health - GetIncomingDamage(target) < 1)
                return false;
            else
                return true;
        }

        public static bool IsCanMove(this Obj_AI_Hero target)
        {
            if (target.MoveSpeed < 50 || !Player.CanMove || target.IsStunned || target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) || (target.IsCastingInterruptableSpell() && !target.IsMoving))
                return false;
            else
                return true;
        }

        public static int GetTargetBuffCounts(this Obj_AI_Base target, string buffName)
        {
            foreach (var buff in target.Buffs.Where(buff => buff.Name == buffName))
            {
                if (buff.Count == 0)
                    return 1;
                else
                    return buff.Count;
            }
            return 0;
        }

        public static int GetMinionsCountsInRange(this Obj_AI_Base target, float range)
        {
            var allMinions = GetMinions(target.Position, range);

            if (allMinions != null)
                return allMinions.Count;
            else
                return 0;
        }

        public static float GetTargetBuffTime(this Obj_AI_Base target, string buffName)
        {
            return target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time).Where(buff => buff.Name == buffName).Select(buff => buff.EndTime).FirstOrDefault() - Game.Time;
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
    }
    public class UnitIncomingDamage
    {
        public int TargetNetworkId { get; set; }
        public float Time { get; set; }
        public double Damage { get; set; }
        public bool Skillshot { get; set; }
    }
}