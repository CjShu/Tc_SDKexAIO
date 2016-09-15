﻿// Copyright 2014 - 2014 Esk0r
// Collision.cs is part of Evade.
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

namespace Tc_SDKexAIO.Common.Evade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.SDK;
    using SharpDX;
    using LeagueSharp.SDK.Utils;

    public enum CollisionObjectTypes
    {
        Minion,
        Champions,
        YasuoWall,
    }

    internal class FastPredResult
    {
        public Vector2 CurrentPos;
        public bool IsMoving;
        public Vector2 PredictedPos;
    }

    internal class DetectedCollision
    {
        public float Diff;
        public float Distance;
        public Vector2 Position;
        public CollisionObjectTypes Type;
        public Obj_AI_Base Unit;
    }

    internal static class Collision
    {
        private static int WallCastT;
        private static Vector2 YasuoWallCastedPos;

        public static void Init()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsValid || sender.Team != GameObjects.Player.Team || args.SData.Name != "YasuoWMovingWall")
            {
                return;
            }

            WallCastT = Utils.TickCount;
            YasuoWallCastedPos = sender.ServerPosition.ToVector2();
        }

        public static FastPredResult FastPrediction(Vector2 from, Obj_AI_Base unit, int delay, int speed)
        {
            var tDelay = delay / 1000f + (from.Distance(unit) / speed);
            var d = tDelay * unit.MoveSpeed;
            var path = unit.GetWaypoints();

            if (path.PathLength() > d)
            {
                return new FastPredResult
                {
                    IsMoving = true,
                    CurrentPos = unit.ServerPosition.ToVector2(),
                    PredictedPos = path.CutPath((int) d)[0],
                };
            }

            return new FastPredResult
            {
                IsMoving = false,
                CurrentPos = path[path.Count - 1],
                PredictedPos = path[path.Count - 1],
            };
        }

        public static Vector2 GetCollisionPoint(Skillshot skillshot)
        {
            var collisions = new List<DetectedCollision>();
            var from = skillshot.GetMissilePosition(0);
            skillshot.ForceDisabled = false;
            foreach (var cObject in skillshot.SpellData.CollisionObjects)
            {
                switch (cObject)
                {
                    case CollisionObjectTypes.Minion:

                        if (!Config.Menu["Collision"]["MinionCollision"])
                        {
                            break;
                        }

                        var minions = new List<Obj_AI_Minion>();

                        minions.AddRange(
                            GameObjects.Minions.Where(x =>
                            x.IsValidTarget(1200f, false, from.ToVector3()) && (
                            skillshot.Unit.Team == GameObjects.Player.Team ? 
                            x.Team != GameObjects.Player.Team :
                            x.Team == GameObjects.Player.Team)));

                        minions.AddRange(
                            GameObjects.Jungle.Where(x => 
                            x.IsValidTarget(1200f, false, from.ToVector3())));

                        foreach (var minion in minions)
                        {
                            var pred = FastPrediction(
                                from, minion,
                                Math.Max(0, skillshot.SpellData.Delay - (Utils.TickCount - skillshot.StartTick)),
                                skillshot.SpellData.MissileSpeed);
                            var pos = pred.PredictedPos;
                            var w = skillshot.SpellData.RawRadius + (!pred.IsMoving ? (minion.BoundingRadius - 15) : 0) -
                                    pos.Distance(from, skillshot.End, true);
                            if (w > 0)
                            {
                                collisions.Add(
                                    new DetectedCollision
                                    {
                                        Position =
                                            pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint +
                                            skillshot.Direction * 30,
                                        Unit = minion,
                                        Type = CollisionObjectTypes.Minion,
                                        Distance = pos.Distance(from),
                                        Diff = w,
                                    });
                            }
                        }

                        break;

                    case CollisionObjectTypes.Champions:
                        if (!Config.Menu["Collision"]["HeroCollision"])
                        {
                            break;
                        }
                        foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => (h.IsValidTarget(1200, false) && h.Team == GameObjects.Player.Team && !h.IsMe && h.Team != GameObjects.Player.Team)))
                        {
                            var pred = FastPrediction(
                                from, hero,
                                Math.Max(0, skillshot.SpellData.Delay - (Utils.TickCount - skillshot.StartTick)),
                                skillshot.SpellData.MissileSpeed);
                            var pos = pred.PredictedPos;

                            var w = skillshot.SpellData.RawRadius + 30 - pos.Distance(from, skillshot.End, true);
                            if (w > 0)
                            {
                                collisions.Add(
                                    new DetectedCollision
                                    {
                                        Position =
                                            pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint +
                                            skillshot.Direction * 30,
                                        Unit = hero,
                                        Type = CollisionObjectTypes.Minion,
                                        Distance = pos.Distance(from),
                                        Diff = w,
                                    });
                            }
                        }
                        break;

                    case CollisionObjectTypes.YasuoWall:
                        if (!Config.Menu["Collision"]["YasuoCollision"])
                        {
                            break;
                        }

                        if (!ObjectManager.Get<Obj_AI_Hero>().Any(hero => hero.IsValidTarget(float.MaxValue, false) &&
                                        hero.Team == GameObjects.Player.Team && hero.ChampionName == "Yasuo"))
                        {
                            break;
                        }
                        GameObject wall = null;
                        foreach (var gameObject in ObjectManager.Get<GameObject>())
                        {
                            if (gameObject.IsValid &&
                                System.Text.RegularExpressions.Regex.IsMatch(
                                    gameObject.Name, "_w_windwall.\\.troy",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            {
                                wall = gameObject;
                            }
                        }
                        if (wall == null)
                        {
                            break;
                        }
                        var level = wall.Name.Substring(wall.Name.Length - 6, 1);
                        var wallWidth = 300 + 50 * Convert.ToInt32(level);


                        var wallDirection = (wall.Position.ToVector2() - YasuoWallCastedPos).Normalized().Perpendicular();
                        var wallStart = wall.Position.ToVector2() + wallWidth / 2 * wallDirection;
                        var wallEnd = wallStart - wallWidth * wallDirection;
                        var wallPolygon = new Geometry.Rectangle(wallStart, wallEnd, 75).ToPolygon();
                        var intersections = new List<Vector2>();

                        for (var i = 0; i < wallPolygon.Points.Count; i++)
                        {
                            var inter =
                                wallPolygon.Points[i].Intersection(
                                    wallPolygon.Points[i != wallPolygon.Points.Count - 1 ? i + 1 : 0], from,
                                    skillshot.End);
                            if (inter.Intersects)
                            {
                                intersections.Add(inter.Point);
                            }
                        }

                        if (intersections.Count > 0)
                        {
                            var intersection = intersections.OrderBy(item => item.Distance(from)).ToList()[0];
                            var collisionT = Utils.TickCount +
                                             Math.Max(
                                                 0,
                                                 skillshot.SpellData.Delay -
                                                 (Utils.TickCount - skillshot.StartTick)) + 100 +
                                             1000 * intersection.Distance(from) / skillshot.SpellData.MissileSpeed;
                            if (collisionT - WallCastT < 4000)
                            {
                                if (skillshot.SpellData.Type != SkillShotType.SkillshotMissileLine)
                                {
                                    skillshot.ForceDisabled = true;
                                }
                                return intersection;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var result = collisions.Count > 0 ? collisions.OrderBy(c => c.Distance).ToList()[0].Position : new Vector2();

            return result;
        }
    }
}