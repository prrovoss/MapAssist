using MapAssist.Types;
using System.Collections.Generic;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;
using System.Linq;
using GameOverlay.Drawing;
using System;
using Roy_T.AStar.Grids;
using System.Runtime.Serialization;
using PrroBot.GameInteraction;

namespace PrroBot
{
    public enum MovementMode
    {
        Teleport,
        Walking
    }

    public struct MovementPath
    {
        public MovementMode Mode;
        public List<Point> Path;
    }

    public static class Pathing
    {
        private static Grid Grid;

        // Stuff for teleport pathing
        private static readonly short RangeInvalid = 10000;
        private static readonly short TpRange = 20;
        private static readonly short BlockRange = 2;
        private static short[,] m_distanceMatrix;
        private static int m_rows;
        private static int m_columns;
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public static List<Point> currentPath = new List<Point>();

        public static MovementMode GetMovementMode()
        {
            var gameData = Core.GetGameData();

            var teleSkill = gameData.PlayerUnit.Skills.AllSkills.FirstOrDefault(x => x.Skill == Skill.Teleport || x.Skill == Skill.Teleport2);

            var inTown = gameData.Area.IsTown();

            return (!inTown && teleSkill != null ? MovementMode.Teleport : MovementMode.Walking);

        }

        public static MovementPath GetPathToLocation(Point fromLocation, Point toLocation)
        {
            var areaData = Core.GetAreaData();

            Grid = areaData.MapToGrid();

            var path = new List<Point>();
            var resultMovementPath = new MovementPath
            {
                Mode = GetMovementMode()
            };

            // cancel if the provided points dont map into the collisionMap of the AreaData
            if (!areaData.TryMapToPointInMap(fromLocation, out var fromPosition) || !areaData.TryMapToPointInMap(toLocation, out var toPosition))
            {
                throw new PathingException("The provided points dont map into the collisionMap");
            }

            if (resultMovementPath.Mode == MovementMode.Teleport)
            {
                var teleportPath = GetTeleportPath(areaData, fromPosition, toPosition, out var pathFound);
                if (pathFound)
                {
                    /* the TeleportPather returns the Points on the path withoug the Origin offset. 
                     * The Compositor expects this offset so we have to add it before we can return the result */
                    path = teleportPath.Select(p => new Point(p.X += areaData.Origin.X, p.Y += areaData.Origin.Y)).ToList();
                }
            }
            else
            {
                var pathFinder = new PathFinder();
                var toGridPosition = new GridPosition((int)toPosition.X, (int)toPosition.Y);

                var startingPoints = Common.GetPointsSurroundingPoint(fromPosition, 2);
                var pathFound = false;
                var cnt = 0;

                do
                {
                    var fromGridPosition = new GridPosition((int)startingPoints[cnt].X, (int)startingPoints[cnt].Y);
                    var navPath = pathFinder.FindPath(fromGridPosition, toGridPosition, Grid);

                    if (navPath.Edges.Count > 0)
                    {
                        var endPosition = navPath.Edges.LastOrDefault()?.End.Position;
                        var endPositionMapped = areaData.MapToPoint(endPosition.Value);
                        if (endPosition.HasValue && CalculateDistance(endPositionMapped, toLocation) < 5)
                        {
                            path = navPath.Edges.Where((p, i) => i % 3 == 0 || i == navPath.Edges.Count - 1).Select(e => areaData.MapToPoint(e.End.Position)).ToList();
                            pathFound = true;
                        }
                    }

                    cnt++;
                } while (!pathFound && cnt < startingPoints.Count());

            }

            currentPath = path;
            resultMovementPath.Path = path;
            return resultMovementPath;
        }

        internal static void ClearCurrentPath()
        {
            currentPath = new List<Point>();
        }

        private static List<Point> GetTeleportPath(AreaData areaData, Point fromLocation, Point toLocation, out bool pathFound)
        {
            MakeDistanceTable(areaData, toLocation);
            var path = new List<Point>
            {
                fromLocation
            };
            var idxPath = 1;

            var bestMove = new BestMove
            {
                Move = fromLocation,
                Result = PathingResult.DestinationNotReachedYet
            };

            var move = GetBestMove(bestMove.Move, toLocation, BlockRange);
            while (move.Result != PathingResult.Failed && idxPath < 100)
            {
                // Reached?
                if (move.Result == PathingResult.Reached)
                {
                    AddToListAtIndex(path, toLocation, idxPath);
                    idxPath++;
                    pathFound = true;
                    return path.GetRange(0, idxPath);
                }

                // Perform a redundancy check
                var nRedundancy = GetRedundancy(path, idxPath, move.Move);
                if (nRedundancy == -1)
                {
                    // no redundancy
                    AddToListAtIndex(path, move.Move, idxPath);
                    idxPath++;
                }
                else
                {
                    // redundancy found, discard all redundant steps
                    idxPath = nRedundancy + 1;
                    AddToListAtIndex(path, move.Move, idxPath);
                }

                move = GetBestMove(move.Move, toLocation, BlockRange);
            }

            pathFound = false;
            return null;
        }

        private static void MakeDistanceTable(AreaData areaData, Point toLocation)
        {
            m_rows = areaData.CollisionGrid.GetLength(0);
            m_columns = areaData.CollisionGrid[0].GetLength(0);
            m_distanceMatrix = new short[m_columns, m_rows];
            for (var i = 0; i < m_columns; i++)
            {
                for (var k = 0; k < m_rows; k++)
                {
                    m_distanceMatrix[i, k] = (short)areaData.CollisionGrid[k][i];
                }
            }

            for (var x = 0; x < m_columns; x++)
            {
                for (var y = 0; y < m_rows; y++)
                {
                    if ((m_distanceMatrix[x, y] % 2) == 0)
                        m_distanceMatrix[x, y] = (short)CalculateDistance(x, y, toLocation.X, toLocation.Y);
                    else
                        m_distanceMatrix[x, y] = RangeInvalid;
                }
            }

            m_distanceMatrix[(int)toLocation.X, (int)toLocation.Y] = 1;
        }


        private static void AddToListAtIndex(List<Point> list, Point point, int index)
        {
            if (index < list.Count)
            {
                list[index] = point;
                return;
            }
            else if (index == list.Count)
            {
                list.Add(point);
                return;
            }

            throw new InvalidOperationException();
        }

        private static BestMove GetBestMove(Point position, Point toLocation, int blockRange)
        {
            if (CalculateDistance(toLocation, position) <= TpRange)
            {
                return new BestMove
                {
                    Result = PathingResult.Reached,
                    Move = toLocation
                };
            }

            if (!IsValidIndex(position.X, position.Y))
            {
                return new BestMove
                {
                    Result = PathingResult.Failed,
                    Move = new Point(0, 0)
                };
            }

            Block(position, blockRange);

            var best = new Point(0, 0);
            int value = RangeInvalid;

            for (var x = position.X - TpRange; x <= position.X + TpRange; x++)
            {
                for (var y = position.Y - TpRange; y <= position.Y + TpRange; y++)
                {
                    if (!IsValidIndex(x, y))
                        continue;

                    var p = new Point((ushort)x, (ushort)y);

                    if (m_distanceMatrix[(int)p.X, (int)p.Y] < value && CalculateDistance(p, position) <= TpRange)
                    {
                        value = m_distanceMatrix[(int)p.X, (int)p.Y];
                        best = p;
                    }
                }
            }

            if (value >= RangeInvalid || best == null)
            {
                return new BestMove
                {
                    Result = PathingResult.Failed,
                    Move = new Point(0, 0)
                };
            }

            Block(best, blockRange);
            return new BestMove
            {
                Result = PathingResult.DestinationNotReachedYet,
                Move = best
            };
        }


        private static void Block(Point position, int nRange)
        {
            nRange = Math.Max(nRange, 1);

            for (var i = position.X - nRange; i < position.X + nRange; i++)
            {
                for (var j = position.Y - nRange; j < position.Y + nRange; j++)
                {
                    if (IsValidIndex(i, j))
                        m_distanceMatrix[(int)i, (int)j] = RangeInvalid;
                }
            }
        }

        private static int GetRedundancy(List<Point> currentPath, int idxPath, Point position)
        {
            // step redundancy check
            for (var i = 1; i < idxPath; i++)
            {
                if (CalculateDistance(currentPath[i].X, currentPath[i].Y, position.X, position.Y) <= TpRange / 2.0)
                    return i;
            }

            return -1;
        }

        private static bool IsValidIndex(int x, int y)
        {
            return x >= 0 && x < m_columns && y >= 0 && y < m_rows;
        }

        private static bool IsValidIndex(float x, float y)
        {
            return IsValidIndex((int)x, (int)y);
        }

        private static double CalculateDistance(long x1, long y1, long x2, long y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }
        private static double CalculateDistance(float x1, float y1, float x2, float y2)
        {
            return CalculateDistance((int)x1, (int)y1, (int)x2, (int)y2);
        }

        public static double CalculateDistance(Point point1, Point point2)
        {
            return CalculateDistance(point1.X, point1.Y, point2.X, point2.Y);
        }

        private struct BestMove
        {
            public PathingResult Result { get; set; }

            public Point Move { get; set; }
        }

    }

    [Serializable]
    internal class PathingException : Exception
    {
        public PathingException()
        {
        }

        public PathingException(string message) : base(message)
        {
        }

        public PathingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PathingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    enum PathingResult
    {
        Failed = 0,     // Failed, error occurred or no available path
        DestinationNotReachedYet,      // Path OK, destination not reached yet
        Reached // Path OK, destination reached(Path finding completed successfully)
    };

}
