﻿namespace Tc_SDKexAIO.Core
{
    using LeagueSharp;
    using LeagueSharp.SDK;
    using SharpDX;

    public class WallDashLogic
    {
        #region Constructors and Destructors

        public WallDashLogic()
        {
        }

        #endregion

        #region Enums

        /// <summary>
        ///     The dash range
        /// </summary>
        private enum DashRange
        {
            Fixed,

            Dynamic
        }

        /// <summary>
        ///     The dash type
        /// </summary>
        private enum DashType
        {
            Unit,

            Dynamic,

            Static,

            NoDash
        }

        /// <summary>
        ///     The unit type
        /// </summary>
        private enum UnitType
        {
            All,

            Allied,

            NotAllied,

            Enemy,

            NotAllyForEnemy,

            Neutral
        }

        #endregion

        #region Public Methods and Operators

        // BUG Navmesh seems broken
        /// <summary>
        ///     Gets the first wall point.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="step">The step.</param>
        /// <returns></returns>
        public static Vector3 GetFirstWallPoint(Vector3 start, Vector3 end, int step = 1)
        {
            if (start.IsValid() && end.IsValid())
            {
                var distance = start.Distance(end);
                for (var i = 0; i < distance; i = i + step)
                {
                    var newPoint = start.Extend(end, i);

                    if (NavMesh.GetCollisionFlags(newPoint) == CollisionFlags.Wall || newPoint.IsWall())
                    {
                        return newPoint;
                    }
                }
            }

            return Vector3.Zero;
        }

        /// <summary>
        ///     Gets the width of the wall.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="maxWallWidth">Maximum width of the wall.</param>
        /// <param name="step">The step.</param>
        /// <returns></returns>
        public static float GetWallWidth(Vector3 start, Vector3 direction, int maxWallWidth = 1000, int step = 1)
        {
            var thickness = 0f;

            if (!start.IsValid() || !direction.IsValid())
            {
                return thickness;
            }

            for (var i = 0; i < maxWallWidth; i = i + step)
            {
                if (NavMesh.GetCollisionFlags(start.Extend(direction, i)) == CollisionFlags.Wall
                    || start.Extend(direction, i).IsWall())
                {
                    thickness += step;
                }
                else
                {
                    return thickness;
                }
            }

            return thickness;
        }

        /// <summary>
        ///     Determines whether dash is walljump.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="dashRange">The dash range.</param>
        /// <param name="minWallWidth">Minimum width of the wall.</param>
        /// <returns></returns>
        public static bool IsWallDash(Obj_AI_Base unit, float dashRange, float minWallWidth = 50)
        {
            return IsWallDash(unit.ServerPosition, dashRange, minWallWidth);
        }

        /// <summary>
        ///     Determines whether dash is walljump.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="dashRange">The dash range.</param>
        /// <param name="minWallWidth">Minimum width of the wall.</param>
        /// <returns></returns>
        public static bool IsWallDash(Vector3 position, float dashRange, float minWallWidth = 50)
        {
            var dashEndPos = GameObjects.Player.Position.Extend(position, dashRange);
            var firstWallPoint = GetFirstWallPoint(ObjectManager.Player.Position, dashEndPos);

            if (firstWallPoint.Equals(Vector3.Zero))
            {
                // No Wall
                return false;
            }

            if (dashEndPos.IsWall())
            // End Position is in Wall
            {
                var wallWidth = GetWallWidth(firstWallPoint, dashEndPos);

                if (wallWidth > minWallWidth && wallWidth - firstWallPoint.Distance(dashEndPos) < wallWidth * 0.4f)
                {
                    return true;
                }
            }
            else
            // End Position is not a Wall
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
