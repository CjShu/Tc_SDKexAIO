namespace Tc_SDKexAIO.Common
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.Data.Enumerations;

    using SharpDX;
    using SharpDX.Direct3D9;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Manager
    {
        private static Obj_AI_Hero Player => PlaySharp.Player;

        public static List<Obj_AI_Minion> GetMinions(Vector3 From, float Range)
        {
            return GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Range, false, @From)).ToList();
        }

        public static List<Obj_AI_Minion> GetMobs(Vector3 From, float Range, bool OnlyBig = false)
        {
            if (OnlyBig)
            {
                return GameObjects.Jungle.Where(x => x.IsValidTarget(Range, false, @From) && !GameObjects.JungleSmall.Contains(x)).ToList();
            }
            else
                return GameObjects.Jungle.Where(x => x.IsValidTarget(Range, false, @From)).ToList();
        }

        public static List<Obj_AI_Hero> GetEnemies(float Range)
        {
            return GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Range) && x.IsEnemy && !x.IsZombie && !x.IsDead).ToList();
        }

        public static Obj_AI_Hero GetTarget(float Range, DamageType DamageType = DamageType.Physical)
        {
            return Variables.TargetSelector.GetTarget(Range, DamageType);
        }

        public static Obj_AI_Hero GetTarget(Spell Spell, bool Ignote = true)
        {
            return Variables.TargetSelector.GetTarget(Spell, Ignote);
        }

        /// <summary>
        /// (This Part From SebbyLib)
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="e"></param>
        public static void SpellCast(Spell spell, Obj_AI_Base Target)
        {
            SkillshotType CoreType = SkillshotType.SkillshotLine;
            bool aoe2 = false;

            if (spell.Type == SkillshotType.SkillshotCircle)
            {
                CoreType = SkillshotType.SkillshotCircle;
                aoe2 = true;
            }

            if (spell.Width > 80 && !spell.Collision)
                aoe2 = true;

            var predInput2 = new Core.PredictionInput
            {
                AoE = aoe2,
                Collision = spell.Collision,
                Speed = spell.Speed,
                Delay = spell.Delay,
                Range = spell.Range,
                From = Player.ServerPosition,
                Radius = spell.Width,
                Unit = Target,
                Type = CoreType
            };
            var poutput2 = Core.Prediction.GetPrediction(predInput2);

            if (spell.Speed != float.MaxValue && Core.TCommon.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                return;

            if (poutput2.Hitchance >= HitChance.VeryHigh)
                spell.Cast(poutput2.CastPosition);
            else if (predInput2.AoE && poutput2.AoeTargetsHitCount > 1 && poutput2.Hitchance >= HitChance.High)
            {
                spell.Cast(poutput2.CastPosition);
            }
        }

        public static bool InAutoAttackRange(AttackableUnit target)
        {
            var baseTarget = (Obj_AI_Base)target;
            var myRange = GetAttackRange(GameObjects.Player);

            if (baseTarget != null)
            {
                return baseTarget.IsHPBarRendered &&
                    Vector2.DistanceSquared(baseTarget.ServerPosition.ToVector2(),
                    ObjectManager.Player.ServerPosition.ToVector2()) <= myRange * myRange;
            }

            return target.IsValidTarget() &&
                Vector2.DistanceSquared(target.Position.ToVector2(),
                ObjectManager.Player.ServerPosition.ToVector2())
                <= myRange * myRange;
        }

        public static float GetAttackRange(Obj_AI_Base Target)
        {
            if (Target != null)
            {
                return Target.GetRealAutoAttackRange();
            }
            else
                return 0f;
        }

        /// <summary>
        /// Judge Target MoveMent Status (This Part From SebbyLib)
        /// </summary>
        /// <param name="Target">Target</param>
        /// <returns></returns>
        public static bool CanMove(Obj_AI_Hero Target)
        {
            if (Target.MoveSpeed < 50 || Target.IsStunned || Target.HasBuffOfType(BuffType.Stun) ||
                Target.HasBuffOfType(BuffType.Fear) || Target.HasBuffOfType(BuffType.Snare) ||
                Target.HasBuffOfType(BuffType.Knockup) || Target.HasBuff("Recall") ||
                Target.HasBuffOfType(BuffType.Knockback) || Target.HasBuffOfType(BuffType.Charm) ||
                Target.HasBuffOfType(BuffType.Taunt) || Target.HasBuffOfType(BuffType.Suppression)
                || (Target.IsCastingInterruptableSpell() && !Target.IsMoving))
            {
                return false;
            }
            else
                return true;
        }

        public static bool CanKill(Obj_AI_Base Target)
        {
            if (Target.HasBuffOfType(BuffType.PhysicalImmunity) || Target.HasBuffOfType(BuffType.SpellImmunity) || Target.IsZombie
                || Target.IsInvulnerable || Target.HasBuffOfType(BuffType.Invulnerability) || Target.HasBuffOfType(BuffType.SpellShield)
                || Target.HasBuff("deathdefiedbuff") || Target.HasBuff("Undying Rage") || Target.HasBuff("Chrono Shift"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool CheckTarget(Obj_AI_Hero Target)
        {
            if (Target != null && !Target.IsDead && !Target.IsZombie && Target.IsHPBarRendered)
            {
                return true;
            }
            else
                return false;
        }

        #region 模式

        public static bool Combo
        {
            get
            {
                return Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo;
            }
        }

        public static bool Harass
        {
            get
            {
                return Variables.Orbwalker.ActiveMode == OrbwalkingMode.Hybrid;
            }
        }

        public static bool LaneClear
        {
            get
            {
                return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LaneClear;
            }
        }

        public static bool LastHit
        {
            get
            {
                return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LastHit;
            }
        }

        public static bool None
        {
            get
            {
                return Variables.Orbwalker.ActiveMode == OrbwalkingMode.None;
            }
        }

        #endregion

        #region BUFF

        public static bool HasSheenBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.Name == "Sheen");
        }

        public static bool CanKillableWith(this Obj_AI_Base t, Spell spell)
        {
            return t.Health < spell.GetDamage(t) - 5;
        }

        public static bool HasBuffInst(this Obj_AI_Base obj, string buffName)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == buffName);
        }

        public static bool HasPassive(this Obj_AI_Base obj)
        {
            return obj.PassiveCooldownEndTime - (Game.Time - 15.5) <= 0;
        }

        public static bool HasBlueBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "CrestoftheAncientGolem");
        }

        public static bool HasRedBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "BlessingoftheLizardElder");
        }

        #endregion

    }

    internal class BlueBuff
    {
        public static float StartTime { get; set; }
        public static float EndTime { get; set; }
    }

    internal class RedBuff
    {
        public static float StartTime { get; set; }
        public static float EndTime { get; set; }
    }

    internal class OnDamageEvent
    {
        public int Time;
        public float Damage;

        internal OnDamageEvent(int time, float damage)
        {
            Time = time;
            Damage = damage;
        }
    }
}