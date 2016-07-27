namespace Tc_SDKexAIO.Common
{

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Utils;
    using LeagueSharp.SDK.Enumerations;
    using SharpDX;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    internal static class Manager
    {

        public static Obj_AI_Hero GetTarget(float Range, DamageType DamageType = DamageType.Physical)
        {
            return Variables.TargetSelector.GetTarget(Range, DamageType);
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





        public static bool Combo { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.Combo; } }

        public static bool Farm { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LaneClear || Variables.Orbwalker.ActiveMode == OrbwalkingMode.Hybrid; } }

        public static bool LaneClear { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LaneClear; } }

        public static bool None { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.None; } }

        public static bool Harass { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.Hybrid; } }

        public static bool LasHit { get { return Variables.Orbwalker.ActiveMode == OrbwalkingMode.LastHit; } }
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