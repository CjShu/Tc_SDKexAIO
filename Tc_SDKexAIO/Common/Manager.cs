namespace Tc_SDKexAIO.Common
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.Enumerations;

    using SharpDX;
    using SharpDX.Direct3D9;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Manager
    {

        #region 預測

        private static Obj_AI_Hero Me => PlaySharp.Player;

        public static void SpellCast(Spell spell, Obj_AI_Base e)
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

            var predInput2 = new PredictionInput
            {
                AoE = aoe2,
                Collision = spell.Collision,
                Speed = spell.Speed,
                Delay = spell.Delay,
                Range = spell.Range,
                From = Me.ServerPosition,
                Radius = spell.Width,
                Unit = e,
                Type = CoreType
            };
            var poitput2 = Movement.GetPrediction(predInput2);

            if (spell.Speed != float.MaxValue && TcCommon.CollisionYasuo(Me.ServerPosition, poitput2.CastPosition))
                return;

            if (poitput2.Hitchance >= HitChance.VeryHigh)
                spell.Cast(poitput2.CastPosition);
            else if (predInput2.AoE && poitput2.AoeTargetsHitCount > 1 && poitput2.Hitchance >= HitChance.High)
            {
                spell.Cast(poitput2.CastPosition);
            }
        }
    
        #endregion

        #region 目標

        /// <summary>
        /// 來自花邊
        /// </summary>
        /// <param name="Range"></param>
        /// <param name="DamageType"></param>
        /// <returns></returns>
        public static Obj_AI_Hero GetTarget(float Range, DamageType DamageType = DamageType.Physical)
        {
            return Variables.TargetSelector.GetTarget(Range, DamageType);
        }

        /// <summary>
        /// 來自花邊
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        public static bool CheckTarget(Obj_AI_Hero Target)
        {
            if (Target != null && !Target.IsDead && !Target.IsZombie && Target.IsHPBarRendered)
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// 來自花邊
        /// </summary>
        /// <param name="Spell"></param>
        /// <param name="Ignote"></param>
        /// <returns></returns>
        public static Obj_AI_Hero GetTarget(Spell Spell, bool Ignote = true)
        {
            return Variables.TargetSelector.GetTarget(Spell, Ignote);
        }


        #endregion

        #region 其他


        /// <summary>
        /// Judge Target MoveMent Status (This Part From SebbyLib) (來自SebbyLib)
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

        /// <summary>
        /// 來自花邊
        /// </summary>
        /// <param name="Range">Search Enemies Range</param>
        /// <returns></returns>
        public static List<Obj_AI_Hero> GetEnemies(float Range)
        {
            return GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Range) && !x.IsZombie && !x.IsDead).ToList();
        }
       
        public static bool InAutoAttackRange(AttackableUnit target)
        {
            var baseTarget = (Obj_AI_Base)target;
            var myRange = GetAttackRange(GameObjects.Player);

            if (baseTarget != null)
            {
                return baseTarget.IsHPBarRendered &&
                    Vector2.DistanceSquared(baseTarget.ServerPosition.ToVector2(),
                    GameObjects.Player.ServerPosition.ToVector2()) <= myRange * myRange;
            }

            return target.IsValidTarget() &&
                Vector2.DistanceSquared(target.Position.ToVector2(),
                GameObjects.Player.ServerPosition.ToVector2())
                <= myRange * myRange;
        }

        /// <summary>
        /// 來自花邊
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
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
        /// 來自花邊
        /// </summary>
        /// <param name="target"></param>
        /// <param name="buffName"></param>
        /// <returns></returns>
        public static float GetTargetBuffTime(this Obj_AI_Base target, string buffName)
        {
            return target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time).Where(buff => buff.Name == buffName).Select(buff => buff.EndTime).FirstOrDefault() - Game.Time;
        }

        public static int GetMana(SpellSlot slot, AMenuComponent viue)
        {
            return viue.GetValue<MenuSlider>().Value + (int)(ObjectManager.Player.Spellbook.GetSpell(slot).ManaCost / ObjectManager.Player.MaxMana * 100);
        }

        public static int GetSliderButtonMana(SpellSlot slot, AMenuComponent viue)
        {
            return viue.GetValue<MenuSliderButton>().SValue + (int)(ObjectManager.Player.Spellbook.GetSpell(slot).ManaCost / ObjectManager.Player.MaxMana * 100);
        }

        #endregion

        #region 小兵

        /// <summary>
        /// 來自花邊
        /// </summary>
        /// <param name="From"></param>
        /// <param name="Range"></param>
        /// <returns></returns>
        public static List<Obj_AI_Minion> GetMinions(Vector3 From, float Range)
        {
            return GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Range, false, @From)).ToList();
        }

        #endregion

        #region 幹死對面目標

        public static bool IsAttackableTarget(this Obj_AI_Hero target)
        {
            return !target.HasUndyingBuff() && !target.HasSpellShield() && !target.IsInvulnerable;
        }

        public static bool HasSpellShield(this Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.SpellShield) || target.HasBuffOfType(BuffType.SpellImmunity);
        }

        private static Obj_AI_Base GetNearObject(String name, Vector3 pos, int maxDistance)
        {
            return GameObjects.Get<Obj_AI_Base>()
                .FirstOrDefault(x => x.Name == name && x.Distance(pos) <= maxDistance);
        }

        private static Vector3 GetWardPos(Vector3 lastPos, int radius = 165, int precision = 3)
        {
            var count = precision;
            while (count > 0)
            {
                var vertices = radius;

                var wardLocations = new WardLocation[vertices];
                var angle = 2 * Math.PI / vertices;

                for (var i = 0; i < vertices; i++)
                {
                    var th = angle * i;
                    var pos = new Vector3((float)(lastPos.X + radius * Math.Cos(th)),
                        (float)(lastPos.Y + radius * Math.Sin(angle * i)), 0);
                    wardLocations[i] = new WardLocation(pos, NavMesh.IsWallOfGrass(pos, 50));
                }

                var grassLocations = new List<GrassLocation>();

                for (var i = 0; i < wardLocations.Length; i++)
                {
                    if (!wardLocations[i].Grass)
                    {
                        continue;
                    }
                    if (i != 0 && wardLocations[i - 1].Grass)
                    {
                        grassLocations.Last().Count++;
                    }
                    else
                    {
                        grassLocations.Add(new GrassLocation(i, 1));
                    }
                }

                var grassLocation = grassLocations.OrderByDescending(x => x.Count).FirstOrDefault();

                if (grassLocation != null)
                {
                    var midelement = (int)Math.Ceiling(grassLocation.Count / 2f);
                    lastPos = wardLocations[grassLocation.Index + midelement - 1].Pos;
                    radius = (int)Math.Floor(radius / 2f);
                }

                count--;
            }

            return lastPos;
        }

        public static void DrawText(Font aFont, String aText, int aPosX, int aPosY, Color aColor)
        {
            aFont.DrawText(null, aText, aPosX + 2, aPosY + 2, aColor != Color.Black ? Color.Black : Color.White);
            aFont.DrawText(null, aText, aPosX, aPosY, aColor);
        }

        #endregion

        #region 打野JG

        #region Types

        internal enum FromMobClass
        {
            ByName,
            ByType
        }

        internal enum MobTypes
        {
            None,
            Small,
            Red,
            Blue,
            Baron,
            Dragon,
            Big
        }

        #endregion

        #region mobTeams

        private static Dictionary<Vector2, GameObjectTeam> mobTeams;

        #endregion

        #region GetMobTeam 打野位置

        public static GameObjectTeam GetMobTeam(Obj_AI_Base mob, float range)
        {
            mobTeams = new Dictionary<Vector2, GameObjectTeam>();
            if (Game.MapId == (GameMapId)11)
            {
                mobTeams.Add(new Vector2(7756f, 4118f), GameObjectTeam.Order); // blue team :red;
                mobTeams.Add(new Vector2(3824f, 7906f), GameObjectTeam.Order); // blue team :blue
                mobTeams.Add(new Vector2(8356f, 2660f), GameObjectTeam.Order); // blue team :golems
                mobTeams.Add(new Vector2(3860f, 6440f), GameObjectTeam.Order); // blue team :wolfs
                mobTeams.Add(new Vector2(6982f, 5468f), GameObjectTeam.Order); // blue team :wariaths
                mobTeams.Add(new Vector2(2166f, 8348f), GameObjectTeam.Order); // blue team :Frog jQuery

                mobTeams.Add(new Vector2(4768, 10252), GameObjectTeam.Neutral); // Baron
                mobTeams.Add(new Vector2(10060, 4530), GameObjectTeam.Neutral); // Dragon

                mobTeams.Add(new Vector2(7274f, 11018f), GameObjectTeam.Chaos); // Red team :red;
                mobTeams.Add(new Vector2(11182f, 6844f), GameObjectTeam.Chaos); // Red team :Blue
                mobTeams.Add(new Vector2(6450f, 12302f), GameObjectTeam.Chaos); // Red team :golems
                mobTeams.Add(new Vector2(11152f, 8440f), GameObjectTeam.Chaos); // Red team :wolfs
                mobTeams.Add(new Vector2(7830f, 9526f), GameObjectTeam.Chaos); // Red team :wariaths
                mobTeams.Add(new Vector2(12568, 6274), GameObjectTeam.Chaos); // Red team : Frog jQuery

                return mobTeams.Where(hp => mob.Distance(hp.Key) <= (range)).Select(hp => hp.Value).FirstOrDefault();
            }

            return GameObjectTeam.Unknown;
        }

        #endregion

        #region GetMobType

        public static MobTypes GetMobType(Obj_AI_Base mob, FromMobClass fromMobClass = FromMobClass.ByName)
        {
            if (mob == null)
            {
                return MobTypes.None;
            }
            if (fromMobClass == FromMobClass.ByName)
            {
                if (mob.SkinName.Contains("SRU_Baron") || mob.SkinName.Contains("SRU_RiftHerald"))
                {
                    return MobTypes.Baron;
                }

                if (mob.SkinName.Contains("SRU_Dragon"))
                {
                    return MobTypes.Dragon;
                }

                if (mob.SkinName.Contains("SRU_Blue"))
                {
                    return MobTypes.Blue;
                }

                if (mob.SkinName.Contains("SRU_Red"))
                {
                    return MobTypes.Red;
                }

                if (mob.SkinName.Contains("SRU_Red"))
                {
                    return MobTypes.Red;
                }
            }

            if (fromMobClass == FromMobClass.ByType)
            {
                Obj_AI_Base oMob = (from fBigBoys in new[]
                        {
                            "SRU_Baron", "SRU_Dragon", "SRU_RiftHerald", "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf",
                            "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "Sru_Crab"
                        }
                     where fBigBoys == mob.SkinName

                     select mob).FirstOrDefault();

                if (oMob != null)
                {
                    return MobTypes.Big;
                }
            }
            return MobTypes.Small;
        }

        #endregion

        #region GetMobsType2

        /// <summary>
        /// 來自花邊
        /// </summary>
        /// <param name="From"></param>
        /// <param name="Range"></param>
        /// <param name="OnlyBig"></param>
        /// <returns></returns>
        public static List<Obj_AI_Minion> GetMobs(Vector3 From, float Range, bool OnlyBig = false)
        {
            if (OnlyBig)
            {
                return GameObjects.Jungle.Where(x => x.IsValidTarget(Range, false, @From) && !GameObjects.JungleSmall.Contains(x)).ToList();
            }
            else
                return GameObjects.Jungle.Where(x => x.IsValidTarget(Range, false, @From)).ToList();
        }

        #endregion

        #endregion

        #region 走坎模式

        public static bool Combo { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo; } }

        public static bool Farm { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LaneClear || Variables.Orbwalker.ActiveMode == OrbwalkingMode.Hybrid; } }

        public static bool LaneClear { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LaneClear; } }

        public static bool None { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.None; } }

        public static bool Harass { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.Hybrid; } }

        public static bool LasHit { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LastHit; } }

        #endregion
    }

    internal static class ComBuffs
    {

        public static bool HasBuffInst(this Obj_AI_Base obj, string buffName)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == buffName);
        }

        public static bool HasBlueBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "CrestoftheAncientGolem");
        }

        public static bool HasRedBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.DisplayName == "BlessingoftheLizardElder");
        }

        public static bool HasSheenBuff(this Obj_AI_Base obj)
        {
            return obj.Buffs.Any(buff => buff.Name == "Sheen");
        }

        public static bool HasUndyingBuff(this Obj_AI_Hero target)
        {
            // Various buffs
            if (target.Buffs.Any(
                b => b.IsValid &&
                     (b.DisplayName == "Chrono Shift" /* Zilean R */||
                      b.DisplayName == "JudicatorIntervention" /* Kayle R */||
                      b.DisplayName == "Undying Rage" /* Tryndamere R */)))
            {
                return true;
            }

            return target.IsInvulnerable;
        }

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

    internal class WardLocation
    {
        public readonly Vector3 Pos;

        public readonly bool Grass;

        public WardLocation(Vector3 pos, bool grass)
        {
            Pos = pos;
            Grass = grass;
        }
    }

    internal class GrassLocation
    {
        public readonly int Index;

        public int Count;

        public GrassLocation(int index, int count)
        {
            Index = index;
            Count = count;
        }
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

    internal class EnemyHeros
    {
        public Obj_AI_Hero Player;
        public int LastSeen;
        public int LastSeenForE;


        public EnemyHeros(Obj_AI_Hero player)
        {
            Player = player;
        }
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