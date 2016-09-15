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
    using Collision = LeagueSharp.SDK.Collision;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Manager
    {
        private static Obj_AI_Hero Player => PlaySharp.Player;

        public static string[] AutoEnableList =
        {
             "Annie", "Ahri", "Akali", "Anivia", "Annie", "Brand", "Cassiopeia", "Diana", "Evelynn", "FiddleSticks", "Fizz", "Gragas", "Heimerdinger", "Karthus",
             "Kassadin", "Katarina", "Kayle", "Kennen", "Leblanc", "Lissandra", "Lux", "Malzahar", "Mordekaiser", "Morgana", "Nidalee", "Orianna",
             "Ryze", "Sion", "Swain", "Syndra", "Teemo", "TwistedFate", "Veigar", "Viktor", "Vladimir", "Xerath", "Ziggs", "Zyra", "Velkoz", "Azir", "Ekko",
             "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "Jayce", "Jinx", "KogMaw", "Lucian", "MasterYi", "MissFortune", "Quinn", "Shaco", "Sivir",
             "Talon", "Tristana", "Twitch", "Urgot", "Varus", "Vayne", "Yasuo", "Zed", "Kindred", "AurelionSol"
        };

        public static string[] Marksman =
        {
            "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Jinx", "Kalista",
            "KogMaw", "Lucian", "MissFortune", "Quinn", "Sivir", "Teemo", "Tristana", "Twitch", "Urgot", "Varus",
            "Vayne"
        };

        public static List<Obj_AI_Minion> GetMinions(Vector3 From, float Range)
        {
            return GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Range, false, From)).ToList();
        }

        public static List<Obj_AI_Minion> GetMobs(Vector3 From, float Range, bool OnlyBig = false)
        {
            if (OnlyBig)
            {
                return GameObjects.Jungle.Where(x => x.IsValidTarget(Range, false, @From) && (x.Name.Contains("Crab") || !GameObjects.JungleSmall.Contains(x))).ToList();
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

        public static int GetMana(SpellSlot slot, AMenuComponent value) => value.GetValue<MenuSliderButton>().SValue + (int)(GameObjects.Player.Spellbook.GetSpell(slot).ManaCost / GameObjects.Player.MaxMana * 100);

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
            return !(Target.MoveSpeed < 50) && !Target.IsStunned && !Target.HasBuffOfType(BuffType.Stun) && !Target.HasBuffOfType(BuffType.Fear) && !Target.HasBuffOfType(BuffType.Snare) && !Target.HasBuffOfType(BuffType.Knockup) && !Target.HasBuff("Recall") && !Target.HasBuffOfType(BuffType.Knockback) && !Target.HasBuffOfType(BuffType.Charm) && !Target.HasBuffOfType(BuffType.Taunt) && !Target.HasBuffOfType(BuffType.Suppression) && (!Target.IsCastingInterruptableSpell() || Target.IsMoving);
        }


        public static bool CanKill(Obj_AI_Base Target)
        {
            if (Target.HasBuffOfType(BuffType.PhysicalImmunity)
                || Target.HasBuffOfType(BuffType.SpellImmunity)
                || Target.IsZombie
                || Target.IsInvulnerable
                || Target.HasBuffOfType(BuffType.Invulnerability)
                || Target.HasBuffOfType(BuffType.SpellShield)
                || Target.HasBuff("deathdefiedbuff")
                || Target.HasBuff("Undying Rage")
                || Target.HasBuff("Chrono Shift"))
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


        public static double GetDamage(Obj_AI_Hero Target, bool CalCulateAttackDamage = true,
            bool CalCulateQDamage = true, bool CalCulateWDamage = true,
            bool CalCulateEDamage = true, bool CalCulateRDamage = true)
        {
            if (CheckTarget(Target))
            {
                double Damage = 0d;

                if (CalCulateAttackDamage)
                {
                    Damage += GameObjects.Player.GetAutoAttackDamage(Target);
                }

                if (CalCulateQDamage)
                {
                    Damage += GameObjects.Player.Spellbook.GetSpell(SpellSlot.Q).IsReady() ? GameObjects.Player.GetSpellDamage(Target, SpellSlot.Q) : 0d;
                }

                if (CalCulateWDamage)
                {
                    Damage += GameObjects.Player.Spellbook.GetSpell(SpellSlot.W).IsReady() ? GameObjects.Player.GetSpellDamage(Target, SpellSlot.W) : 0d;
                }

                if (CalCulateEDamage)
                {
                    Damage += GameObjects.Player.Spellbook.GetSpell(SpellSlot.E).IsReady() ? GameObjects.Player.GetSpellDamage(Target, SpellSlot.E) : 0d;
                }

                if (CalCulateRDamage)
                {
                    Damage += GameObjects.Player.Spellbook.GetSpell(SpellSlot.R).IsReady() ? GameObjects.Player.GetSpellDamage(Target, SpellSlot.R) : 0d;
                }

                if (GameObjects.Player.GetSpellSlot("SummonerDot") != SpellSlot.Unknown && GameObjects.Player.GetSpellSlot("SummonerDot").IsReady())
                {
                    Damage += 50 + 20 * GameObjects.Player.Level - (Target.HPRegenRate / 5 * 3);
                }

                if (Target.ChampionName == "Moredkaiser")
                    Damage -= Target.Mana;

                // exhaust
                if (GameObjects.Player.HasBuff("SummonerExhaust"))
                    Damage = Damage * 0.6f;

                // blitzcrank passive
                if (Target.HasBuff("BlitzcrankManaBarrierCD") && Target.HasBuff("ManaBarrier"))
                    Damage -= Target.Mana / 2f;

                // kindred r
                if (Target.HasBuff("KindredRNoDeathBuff"))
                    Damage = 0;

                // tryndamere r
                if (Target.HasBuff("UndyingRage") && Target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                    Damage = 0;

                // kayle r
                if (Target.HasBuff("JudicatorIntervention"))
                    Damage = 0;

                // zilean r
                if (Target.HasBuff("ChronoShift") && Target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                    Damage = 0;

                // fiora w
                if (Target.HasBuff("FioraW"))
                    Damage = 0;

                return Damage;
            }
            else
            {
                return 0d;
            }
        }


        public static void DrawEndScene(float range)
        {
            var pointList = new List<Vector3>();
            for (var i = 0; i < 30; i++)
            {
                var angle = i * Math.PI * 2 / 30;
                pointList.Add(
                    new Vector3(
                        GameObjects.Player.Position.X + range * (float)Math.Cos(angle), GameObjects.Player.Position.Y + range * (float)Math.Sin(angle),
                        GameObjects.Player.Position.Z));
            }

            for (var i = 0; i < pointList.Count; i++)
            {
                var a = pointList[i];
                var b = pointList[i == pointList.Count - 1 ? 0 : i + 1];

                var aonScreen = Drawing.WorldToMinimap(a);
                var bonScreen = Drawing.WorldToMinimap(b);
                var aon1Screen = Drawing.WorldToScreen(a);
                var bon1Screen = Drawing.WorldToScreen(b);

                Drawing.DrawLine(aon1Screen.X, aon1Screen.Y, bon1Screen.X, bon1Screen.Y, 1, System.Drawing.Color.White);
                Drawing.DrawLine(aonScreen.X, aonScreen.Y, bonScreen.X, bonScreen.Y, 1, System.Drawing.Color.White);
            }
        }


        public static int GetCustomDamage(this Obj_AI_Hero source, string auraname, Obj_AI_Hero target)
        {
            if (auraname == "sheen")
            {
                return
                    (int)
                        source.CalculateDamage(target, DamageType.Physical,
                            1.0 * source.FlatPhysicalDamageMod + source.BaseAttackDamage);
            }

            if (auraname == "lichbane")
            {
                return
                    (int)
                        source.CalculateDamage(target, DamageType.Magical,
                            (0.75 * source.FlatPhysicalDamageMod + source.BaseAttackDamage) +
                            (0.50 * source.FlatMagicDamageMod));
            }

            return 0;
        }


        public static bool SpellCollision(Obj_AI_Hero t, Spell spell, int extraWith = 50)
        {
            foreach (var hero in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(spell.Range + spell.Width, true, spell.RangeCheckFrom) && t.NetworkId != hero.NetworkId))
            {
                var prediction = spell.GetPrediction(hero);
                var powCalc = Math.Pow((spell.Width + extraWith + hero.BoundingRadius), 2);
                if (prediction.UnitPosition.ToVector2().DistanceSquared(spell.From.ToVector2(), spell.GetPrediction(t).CastPosition.ToVector2(), true) <= powCalc)
                {
                    return true;
                }
                else if (prediction.UnitPosition.ToVector2().Distance(spell.From.ToVector2(), t.ServerPosition.ToVector2(), true) <= powCalc)
                {
                    return true;
                }

            }
            return false;
        }


        /// <summary>
        /// (Sebby Lib)
        /// </summary>
        /// <returns></returns>
        public static bool CanHarras()
        {
            if (!Player.IsWindingUp && !Player.IsUnderEnemyTurret() && Variables.Orbwalker.CanMove)
                return true;
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

        public static bool Hit
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

        public static readonly Dictionary<int, List<OnDamageEvent>> DamagesOnTime = new Dictionary<int, List<OnDamageEvent>>();

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