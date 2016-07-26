namespace Tc_SDKexAIO.Core
{
    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using SharpDX;
    using System.Collections.Generic;
    using System.Linq;
    using Color = System.Drawing.Color;
    using Geometry = LeagueSharp.Common.Geometry;

    public class Dash
    {
        
        #region Fields

        public static WallDashLogic ProviderWallDash = new WallDashLogic();

        /// <summary>
        ///     The direction
        /// </summary>
        public static Vector3 Direction;

        /// <summary>
        ///     The distance
        /// </summary>
        public static float Distance;

        /// <summary>
        ///     The end position
        /// </summary>
        public static Vector3 EndPosition;

        /// <summary>
        ///     Indicates if dash through skillshot
        /// </summary>
        public static bool InSkillshot;

        /// <summary>
        ///     The knocked up heroes
        /// </summary>
        public static List<Obj_AI_Hero> KnockUpHeroes = new List<Obj_AI_Hero>();

        /// <summary>
        ///     The knocked up minions
        /// </summary>
        public static List<Obj_AI_Base> KnockUpMinions = new List<Obj_AI_Base>();

        /// <summary>
        ///     The start position
        /// </summary>
        public static Vector3 StartPosition;

        /// <summary>
        ///     The Unit
        /// </summary>
        public static Obj_AI_Base Unit;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Dash" /> class.
        /// </summary>
        /// <param name="unit">The unit.</param>
        public Dash(Obj_AI_Base unit)
        {
            Unit = unit;

            StartPosition = GameObjects.Player.ServerPosition;
            EndPosition = Geometry.Extend(
                StartPosition,
                unit.ServerPosition,
                PlaySharp.E.Range);

            SetDashLength();
            SetDangerValue();

            CheckWallDash();

            Distance = Geometry.Distance(StartPosition, EndPosition);

            CheckKnockups();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the danger value.
        /// </summary>
        /// <value>
        ///     The danger value.
        /// </value>
        public static int DangerValue { get; private set; }

        /// <summary>
        ///     Gets the dash lenght.
        /// </summary>
        /// <value>
        ///     The dash lenght.
        /// </value>
        public static float DashLenght { get; private set; }

        /// <summary>
        ///     Gets the dash time.
        /// </summary>
        /// <value>
        ///     The dash time.
        /// </value>
        public static float DashTime { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this dash is wall dash.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is wall dash; otherwise, <c>false</c>.
        /// </value>
        public static bool IsWallDash { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether wall dash saves time.
        /// </summary>
        /// <value>
        ///     <c>true</c> if [wall dash saves time]; otherwise, <c>false</c>.
        /// </value>
        public static bool WallDashSavesTime { get; protected internal set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Draws this instance.
        /// </summary>
        public void Draw()
        {
            var color = Color.White;

            if (EndPosition.CountEnemyHeroesInRange(375) > 0)
            {
                color = Color.Red;
            }
            Drawing.DrawLine(
                Drawing.WorldToScreen(StartPosition),
                Drawing.WorldToScreen(EndPosition),
                4f,
                color);

            LeagueSharp.SDK.Utils.Render.Circle.DrawCircle(EndPosition, 350, color);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Checks for knockups.
        /// </summary>
        private static void CheckKnockups()
        {
            foreach (var enemy in GameObjects.EnemyHeroes)
            {
                if (Geometry.Distance(enemy, EndPosition) <= 375 && enemy.IsValid)
                {
                    KnockUpHeroes.Add(enemy);
                }
            }

            foreach (var minion in GameObjects.Minions.Where(m => !m.IsAlly && m.Distance(EndPosition) <= 350))
            {
                if (minion.IsValid)
                {
                    KnockUpMinions.Add(minion);
                }
            }
        }

        /// <summary>
        ///     Checks for skillshots.
        /// </summary>
        private static void CheckSkillshots()
        {
            var skillshotDict = new Dictionary<Skillshot, Geometry.Polygon>();

            foreach (var skillshot in Tracker.DetectedSkillshots)
            {
                var Polygon = new Geometry.Polygon();

                switch (skillshot.SData.SpellType)
                {
                    case SpellType.SkillshotLine:
                        Polygon = new Geometry.Polygon.Rectangle(
                            skillshot.StartPosition,
                            skillshot.EndPosition,
                            skillshot.SData.Radius);
                        break;
                    case SpellType.SkillshotCircle:
                        Polygon = new Geometry.Polygon.Circle(skillshot.EndPosition, skillshot.SData.Radius);
                        break;
                    case SpellType.SkillshotArc:
                        Polygon = new Geometry.Polygon.Sector(
                            skillshot.StartPosition,
                            skillshot.Direction,
                            skillshot.SData.Angle,
                            skillshot.SData.Radius);
                        break;
                }

                skillshotDict.Add(skillshot, Polygon);
            }

            foreach (var skillshot in skillshotDict)
            {
                var clipperpath = skillshot.Value.ToClipperPath();
                var connectionPolygon = new Geometry.Polygon.Line(StartPosition, EndPosition);
                var connectionclipperpath = connectionPolygon.ToClipperPath();

                if (clipperpath.Intersect(connectionclipperpath).Any())
                {
                    InSkillshot = true;
                }
            }
        }

        /// <summary>
        ///     Checks for wall dash.
        /// </summary>
        /// <param name="minWallWidth">Minimum width of the wall.</param>
        private static void CheckWallDash(float minWallWidth = 50)
        {
            if (WallDashLogic.IsWallDash(Direction, DashLenght, minWallWidth))
            {
                IsWallDash = true;
            }
        }

        // TODO: Add Path in Skillshot (Based on Skillshot Danger value) , Add Enemies Around (Based on Priority), Add Allies Around, Add Minions Around (?)
        /// <summary>
        ///     Sets the danger value.
        /// </summary>
        private static void SetDangerValue()
        {
            DangerValue = 0;
        }

        /// <summary>
        ///     Sets the length of the dash.
        /// </summary>
        private static void SetDashLength()
        {
            if (EndPosition.IsWall() && !IsWallDash)
            {
                var newEndPosition = WallDashLogic.GetFirstWallPoint(StartPosition, EndPosition);

                // BUG: Navmesh seems broken and just returns Vector.Zero sometimes
                // Fixed with Broscience...
                if (Geometry.Distance(EndPosition, newEndPosition) <= PlaySharp.E.Range
                    && newEndPosition != Vector3.Zero)
                {
                    EndPosition = newEndPosition;
                }
            }
            DashLenght = Geometry.Distance(StartPosition, EndPosition);
        }

        /// <summary>
        ///     Sets the dash time.
        /// </summary>
        private static void SetDashTime()
        {
            DashTime = DashLenght / PlaySharp.E.Speed;
        }

        #endregion
    }
}