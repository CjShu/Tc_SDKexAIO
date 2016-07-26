// Copyright 2014 - 2014 Esk0r
// Geometry.cs is part of Evade.
// 
// Evade is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Evade is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Evade. If not, see <http://www.gnu.org/licenses/>.

namespace Tc_SDKexAIO.Common
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Clipper;
    using LeagueSharp.SDK.Utils;

    using SharpDX;


    using Path = System.Collections.Generic.List<LeagueSharp.SDK.Clipper.IntPoint>;
    using Paths = System.Collections.Generic.List<System.Collections.Generic.List<LeagueSharp.SDK.Clipper.IntPoint>>;
    using GamePath = System.Collections.Generic.List<SharpDX.Vector2>;

    using Color = System.Drawing.Color;

    #endregion

    public static class Geometry
    {
        #region Constants

        private const int CircleLineSegmentN = 22;

        #endregion

        #region Public Methods and Operators

        public static Paths ClipPolygons(List<Polygon> polygons)
        {
            var subj = new Paths(polygons.Count);
            var clip = new Paths(polygons.Count);

            foreach (var polygon in polygons)
            {
                subj.Add(polygon.ToClipperPath());
                clip.Add(polygon.ToClipperPath());
            }

            var solution = new Paths();
            var c = new Clipper();
            c.AddPaths(subj, PolyType.PtSubject, true);
            c.AddPaths(clip, PolyType.PtClip, true);
            c.Execute(ClipType.CtUnion, solution, PolyFillType.PftPositive, PolyFillType.PftEvenOdd);

            return solution;
        }

        public static void DrawLineInWorld(Vector3 start, Vector3 end, int width, Color color)
        {
            var from = Drawing.WorldToScreen(start);
            var to = Drawing.WorldToScreen(end);
            Drawing.DrawLine(from[0], from[1], to[0], to[1], width, color);
        }

        /// <summary>
        ///     Returns the position on the path after t milliseconds at speed speed.
        /// </summary>
        public static Vector2 PositionAfter(this GamePath self, int t, int speed, int delay = 0)
        {
            var distance = Math.Max(0, t - delay) * speed / 1000;
            for (var i = 0; i <= self.Count - 2; i++)
            {
                var from = self[i];
                var to = self[i + 1];
                var d = (int)to.Distance(from);
                if (d > distance)
                {
                    return from + distance * (to - from).Normalized();
                }
                distance -= d;
            }
            return self[self.Count - 1];
        }

        public static Vector3 SwitchYZ(this Vector3 v)
        {
            return new Vector3(v.X, v.Z, v.Y);
        }

        public static Polygon ToPolygon(this Path v)
        {
            var polygon = new Polygon();
            foreach (var point in v)
            {
                polygon.Add(new Vector2(point.X, point.Y));
            }
            return polygon;
        }

        //Clipper
        public static List<Polygon> ToPolygons(this Paths v)
        {
            var result = new List<Polygon>();

            foreach (var path in v)
            {
                result.Add(path.ToPolygon());
            }

            return result;
        }

        #endregion


        public class Arc
        {

            #region Fields

            public float Distance;

            public Vector2 End;

            public int HitBox;

            public Vector2 Start;

            #endregion

            #region Constructors and Destructors

            public Arc(Vector2 start, Vector2 end, int hitbox)
            {
                this.Start = start;
                this.End = end;
                this.HitBox = hitbox;
                this.Distance = this.Start.Distance(this.End);
            }

            #endregion

            #region Public Methods and Operators

            public Polygon ToPolygon(int offset = 0)
            {
                offset += this.HitBox;
                var result = new Polygon();
                var innerRadius = -0.1562f * this.Distance + 687.31f;
                var outerRadius = 0.35256f * this.Distance + 133f;
                outerRadius = outerRadius / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN);
                var innerCenters = this.Start.CircleCircleIntersection(this.End, innerRadius, innerRadius);
                var outerCenters = this.Start.CircleCircleIntersection(this.End, outerRadius, outerRadius);
                var innerCenter = innerCenters[0];
                var outerCenter = outerCenters[0];
                Render.Circle.DrawCircle(innerCenter.ToVector3(), 100, Color.White);
                var direction = (this.End - outerCenter).Normalized();
                var end = (this.Start - outerCenter).Normalized();
                var maxAngle = (float)(direction.AngleBetween(end) * Math.PI / 180);
                var step = -maxAngle / CircleLineSegmentN;
                for (var i = 0; i < CircleLineSegmentN; i++)
                {
                    var angle = step * i;
                    var point = outerCenter + (outerRadius + 15 + offset) * direction.Rotated(angle);
                    result.Add(point);
                }
                direction = (this.Start - innerCenter).Normalized();
                end = (this.End - innerCenter).Normalized();
                maxAngle = (float)(direction.AngleBetween(end) * Math.PI / 180);
                step = maxAngle / CircleLineSegmentN;
                for (var i = 0; i < CircleLineSegmentN; i++)
                {
                    var angle = step * i;
                    var point = innerCenter + Math.Max(0, innerRadius - offset - 100) * direction.Rotated(angle);
                    result.Add(point);
                }
                return result;
            }

            #endregion


        }

        public class Circle
        {
            #region Fields

            public Vector2 Center;

            public float Radius;

            #endregion

            #region Constructors and Destructors

            public Circle(Vector2 center, float radius)
            {
                this.Center = center;
                this.Radius = radius;
            }

            #endregion

            #region Public Methods and Operators

            public Polygon ToPolygon(int offset = 0, float overrideWidth = -1)
            {
                var result = new Polygon();
                var outRadius = (overrideWidth > 0
                    ? overrideWidth
                    : (offset + this.Radius) / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN));

                for (var i = 1; i <= CircleLineSegmentN; i++)
                {
                    var angle = i * 2 * Math.PI / CircleLineSegmentN;
                    var point = new Vector2(
                        this.Center.X + outRadius * (float)Math.Cos(angle),
                        this.Center.Y + outRadius * (float)Math.Sin(angle));
                    result.Add(point);
                }

                return result;
            }

            #endregion
        }

        public class Circle2
        {
            #region Fields

            public Vector2 Center;

            public float Radius;

            public float CurrentLineSegmentN;

            public float MaxLineSegmentN;

            #endregion

            #region Constructors and Destructors

            public Circle2(Vector2 center, float radius, float currentLineSegmentN, float maxLineSegmentN)
            {
                this.Center = center;
                this.Radius = radius;
                this.CurrentLineSegmentN = currentLineSegmentN;
                this.MaxLineSegmentN = maxLineSegmentN;
            }

            #endregion

            #region Public Methods and Operators

            public Polygon2 ToPolygon(int offset = 0, float overrideWidth = -1)
            {
                var result = new Polygon2();
                var outRadius = (overrideWidth > 0
                    ? overrideWidth
                    : (offset + this.Radius) / (float)Math.Cos(2 * Math.PI / MaxLineSegmentN));

                for (var i = MaxLineSegmentN; i >= CurrentLineSegmentN; i--)
                {
                    var angle = i * 2 * Math.PI / MaxLineSegmentN;
                    var point = new Vector2(this.Center.X + outRadius * (float)Math.Cos(angle),
                        this.Center.Y + outRadius * (float)Math.Sin(angle));
                    result.Add(point);
                }

                return result;
            }

            #endregion
        }

        public class Polygon
        {
            #region Fields

            public List<Vector2> Points = new List<Vector2>();

            #endregion

            #region Public Methods and Operators

            public void Add(Vector2 point)
            {
                this.Points.Add(point);
            }

            public void Draw(Color color, int width = 1)
            {
                for (var i = 0; i <= this.Points.Count - 1; i++)
                {
                    var nextIndex = (this.Points.Count - 1 == i) ? 0 : (i + 1);
                    DrawLineInWorld(this.Points[i].ToVector3(), this.Points[nextIndex].ToVector3(), width, color);
                }
            }

            /// <summary>
            /// Determines whether the specified point is inside.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns></returns>
            public bool IsInside(Vector2 point)
            {
                return !IsOutside(point);
            }

            public bool IsInside(List<Vector2> point)
            {
                return point.Select(p => !IsOutside(p)).FirstOrDefault();
            }

            /// <summary>
            /// Determines whether the specified point is inside.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns></returns>
            public bool IsInside(Vector3 point)
            {
                return !IsOutside(point.ToVector2());
            }

            /// <summary>
            /// Determines whether the specified point is inside.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns></returns>
            public bool IsInside(GameObject point)
            {
                return !IsOutside(point.Position.ToVector2());
            }

            public bool IsOutside(Vector2 point)
            {
                var p = new IntPoint(point.X, point.Y);
                return Clipper.PointInPolygon(p, this.ToClipperPath()) != 1;
            }

            public Path ToClipperPath()
            {
                var result = new Path(this.Points.Count);

                result.AddRange(this.Points.Select(point => new IntPoint(point.X, point.Y)));

                return result;
            }

            #endregion
        }

        public class Polygon2
        {
            #region Fields

            public List<Vector2> Points = new List<Vector2>();

            #endregion

            #region Public Methods and Operators

            public void Add(Vector2 point)
            {
                this.Points.Add(point);
            }

            public void Draw(Color color, int width = 1)
            {
                for (var i = 0; i <= this.Points.Count - 1; i++)
                {
                    var nextIndex = (this.Points.Count - 1 == i) ? i : (i + 1);
                    DrawLineInWorld(this.Points[i].ToVector3(), this.Points[nextIndex].ToVector3(), width, color);
                }
            }

            /// <summary>
            /// Determines whether the specified point is inside.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns></returns>
            public bool IsInside(Vector2 point)
            {
                return !IsOutside(point);
            }

            public bool IsInside(List<Vector2> point)
            {
                return point.Select(p => !IsOutside(p)).FirstOrDefault();
            }

            /// <summary>
            /// Determines whether the specified point is inside.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns></returns>
            public bool IsInside(Vector3 point)
            {
                return !IsOutside(point.ToVector2());
            }

            /// <summary>
            /// Determines whether the specified point is inside.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns></returns>
            public bool IsInside(GameObject point)
            {
                return !IsOutside(point.Position.ToVector2());
            }

            public bool IsOutside(Vector2 point)
            {
                var p = new IntPoint(point.X, point.Y);
                return Clipper.PointInPolygon(p, this.ToClipperPath()) != 1;
            }

            public Path ToClipperPath()
            {
                var result = new Path(this.Points.Count);

                result.AddRange(this.Points.Select(point => new IntPoint(point.X, point.Y)));

                return result;
            }

            #endregion
        }

        public class Rectangle
        {
            #region Fields

            public Vector2 Direction;

            public Vector2 Perpendicular;

            public Vector2 REnd;

            public Vector2 RStart;

            public float Width;

            #endregion

            #region Constructors and Destructors

            public Rectangle(Vector2 start, Vector2 end, float width)
            {
                this.RStart = start;
                this.REnd = end;
                this.Width = width;
                this.Direction = (end - start).Normalized();
                this.Perpendicular = this.Direction.Perpendicular();
            }

            #endregion

            #region Public Methods and Operators

            public Polygon ToPolygon(int offset = 0, float overrideWidth = -1)
            {
                var result = new Polygon();

                result.Add(
                    this.RStart + (overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular
                    - offset * this.Direction);
                result.Add(
                    this.RStart - (overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular
                    - offset * this.Direction);
                result.Add(
                    this.REnd - (overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular
                    + offset * this.Direction);
                result.Add(
                    this.REnd + (overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular
                    + offset * this.Direction);

                return result;
            }

            #endregion
        }

        public class Ring
        {
            #region Fields

            public Vector2 Center;

            public float Radius;

            public float RingRadius; //actually radius width.

            #endregion

            #region Constructors and Destructors

            public Ring(Vector2 center, float radius, float ringRadius)
            {
                this.Center = center;
                this.Radius = radius;
                this.RingRadius = ringRadius;
            }

            #endregion

            #region Public Methods and Operators

            public Polygon ToPolygon(int offset = 0)
            {
                var result = new Polygon();

                var outRadius = (offset + this.Radius + this.RingRadius)
                                / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN);
                var innerRadius = this.Radius - this.RingRadius - offset;

                for (var i = 0; i <= CircleLineSegmentN; i++)
                {
                    var angle = i * 2 * Math.PI / CircleLineSegmentN;
                    var point = new Vector2(
                        this.Center.X - outRadius * (float)Math.Cos(angle),
                        this.Center.Y - outRadius * (float)Math.Sin(angle));
                    result.Add(point);
                }

                for (var i = 0; i <= CircleLineSegmentN; i++)
                {
                    var angle = i * 2 * Math.PI / CircleLineSegmentN;
                    var point = new Vector2(
                        this.Center.X + innerRadius * (float)Math.Cos(angle),
                        this.Center.Y - innerRadius * (float)Math.Sin(angle));
                    result.Add(point);
                }

                return result;
            }

            #endregion
        }

        public class Sector
        {
            #region Fields

            public float Angle;

            public Vector2 Center;

            public Vector2 Direction;

            public float Radius;

            #endregion

            #region Constructors and Destructors

            public Sector(Vector2 center, Vector2 direction, float angle, float radius)
            {
                this.Center = center;
                this.Direction = direction;
                this.Angle = angle;
                this.Radius = radius;
            }

            #endregion

            #region Public Methods and Operators

            public Polygon ToPolygon(int offset = 0)
            {
                var result = new Polygon();
                var outRadius = (this.Radius + offset) / (float)Math.Cos(2 * Math.PI / CircleLineSegmentN);

                result.Add(this.Center);
                var Side1 = this.Direction.Rotated(-this.Angle * 0.5f);

                for (var i = 0; i <= CircleLineSegmentN; i++)
                {
                    var cDirection = Side1.Rotated(i * this.Angle / CircleLineSegmentN).Normalized();
                    result.Add(
                        new Vector2(this.Center.X + outRadius * cDirection.X, this.Center.Y + outRadius * cDirection.Y));
                }

                return result;
            }

            #endregion
        }
    }
}