using GameOverlay.Drawing;
using MapAssist.Helpers;
using MapAssist.Types;
using PrroBot.Builds;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PrroBot.GameInteraction
{
    public static class Movement
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public static void ToTownViaPortal(Build build = null)
        {
            _log.Info("ToTownViaPortal");
            var gameData = Core.GetGameData();
            if (gameData.Area.IsTown()) return;

            var portal = Common.GetPlayerPortal();
            Point portalPosition;
            bool success;
            if (portal != null && Pathing.CalculateDistance(gameData.PlayerPosition, portal.Position) < 50)
            {
                _log.Info("found existing portal close by");
                portalPosition = portal.Position;
            }
            else
            {
                _log.Info("Did not find an open portal close by, open a new one");
                success = OpenTownPortal(out portalPosition);
                if (!success && portal != null)
                {
                    _log.Info("failed to open new town portal, but can use a distant one");
                    portalPosition = portal.Position;
                }
                else if (!success && portal == null)
                {
                    throw new MovementException("failed to open new portal and did not find any existing portal");
                }
            }

            TakeTownPortal(portalPosition, build);
        }

        public static bool OpenTownPortal(out Point position)
        {
            position = new Point();
            var success = false;
            var counter = 0;

            do
            {
                counter++;
                _log.Info("Trying to open new portal (" + counter + ")");

                WaitForNeutral();
                Input.KeyPress(BotConfig.SkillConfig.TownPortal);
                WaitForNeutral();
                Thread.Sleep(1000);

                var portal = Common.GetPlayerPortal();

                if (portal != null)
                {
                    success = true;
                }
                else
                {
                    continue;
                }

                position = portal.Position;

            } while(!success && counter < 4);

            return success;
        }

        public static void TakeTownPortal(Point position, Build build)
        {
            /* using town portal */
            var gameData = Core.GetGameData();
            AreaData areaData;
            var success = false;
            var tryCounter = 0;

            do
            {
                if(tryCounter > 0)
                {
                    _log.Info("Clearing portal area");
                    if (build != null)
                    {
                        build.ClearArea(position, 15);
                    }
                }

                tryCounter++;

                var distance = Pathing.CalculateDistance(position, gameData.PlayerPosition);

                if (distance > 10)
                {
                    _log.Info("Portal too far away, moving");
                    try
                    {
                        MoveToPoint(position);
                    }
                    catch (MovementException e)
                    {
                        _log.Info(e.ToString());
                        continue;
                    }
                    gameData = Core.GetGameData();
                }

                _log.Info("Portal close enough, trying to interact");
                try
                {
                    Interact(position, UnitType.Object);
                }
                catch (MovementException e)
                {
                    _log.Info(e.ToString());
                    continue;
                }

                Common.WaitForLoading(gameData.Area);

                areaData = Core.GetAreaData();
                if (areaData.Area.IsTown()) success = true;

            } while (!success && tryCounter < 4);

            if (!success) throw new MovementException("Failed to take portal to town");
        }

        public static bool CanUseSkill(Skill skill)
        {
            return Core.GetGameData().PlayerUnit.Skills.AllSkills.FirstOrDefault(x => x.Skill == skill) != null;
        }

        public static void TakeWaypoint(int act, int area)
        {
            var gameData = Core.GetGameData();
            PointOfInterest poi = Core.GetPois().Find(x => x.Type == PoiType.Waypoint);
            if (poi == null) throw new MovementException("No waypoint in this area");
            if (Pathing.CalculateDistance(gameData.PlayerPosition, poi.Position) > 5)
            {
                MoveToWaypoint();
            }
            
            Interact(poi.Position, UnitType.Object);
            Thread.Sleep(100);
            gameData = Core.GetGameData();
            if (!gameData.MenuOpen.Waypoint) throw new MovementException("Failed to interact with waypoint");

            var rect = Common.GetGameBounds();

            var baseX = rect.Right * 0.125f;
            var baseY = rect.Bottom * 0.2f;

            var tabSize = rect.Right * 0.045f;

            var finalX = baseX + tabSize * act;

            Input.LeftMouseClick(finalX, baseY);
            Thread.Sleep(250);

            baseX = rect.Right * 0.192f;
            baseY = rect.Bottom * 0.25f;

            var rowSize = rect.Bottom * 0.056f;

            var finalY = baseY + rowSize * area;

            Input.LeftMouseClick(baseX, finalY);

            Common.WaitForLoading(gameData.Area);
        }

        public static void Interact(Point position, UnitType hoverUnitType)
        {
            Interact(position, new UnitType[] { hoverUnitType });
        }

        public static void Interact(Point position, UnitType[] hoverUnitTypes)
        {
            WaitForNeutral();

            var gameData = Core.GetGameData();
            var areaData = Core.GetAreaData();
            var currPlayer = gameData.PlayerUnit;

            var offsets = Common.GetPointsSurroundingPoint(position, 4);

            foreach (var point in offsets)
            {
                var (_, screenCoord) = Common.WorldToScreen(gameData, areaData, point, currPlayer.Position);

                Input.SetCursorPos(screenCoord);
                Thread.Sleep(40);

                var hoverData = GameMemory.GetCurrentHoverData();

                if (hoverUnitTypes.Contains(hoverData.UnitType) && hoverData.IsHovered)
                {
                    Input.LeftMouseClick(screenCoord);
                    Thread.Sleep(250);
                    return;
                }
            }

            throw new MovementException("Failed to interact");
        }


        public static void MoveToQuest(bool takeNextAreaPortal)
        {
            var success = false;
            var tryCount = 0;
            var pointsOfInterest = Core.GetPois();
            var currentAreaData = Core.GetAreaData();

            do
            {
                tryCount++;
                _log.Info("Trying to move to quest " + tryCount);

                PointOfInterest poiQuest = pointsOfInterest.FirstOrDefault(x => (x.IsQuest) && (x.Area == currentAreaData.Area));

                if (poiQuest == null) throw new MovementException("Could not find a Quest Poi in this area");

                try
                {
                    MoveToPoint(poiQuest.Position);
                }
                catch(MovementException e)
                {
                    _log.Info(e.ToString());
                    _log.Info("Movement to poi failed");
                    continue;
                }

                if(takeNextAreaPortal && poiQuest.NextArea != Area.None)
                {
                    try
                    {
                        Interact(poiQuest.Position, UnitType.Tile);
                    }
                    catch(MovementException e)
                    {
                        _log.Info(e.ToString());
                        _log.Info("Interact with quest nextAreaObject failed");
                        continue;
                    }
                }

                success = true;
            } while (success == false && tryCount < 4);

            if (!success) throw new MovementException("Failed to reach quest poi");
        }


        private static void MoveToNextAreaPortal(PointOfInterest poiNextArea)
        {
            var gameData = Core.GetGameData();
            var currentAreaData = Core.GetAreaData();

            var movementPath = Pathing.GetPathToLocation(gameData.PlayerPosition, poiNextArea.Position);

            if (movementPath.Path.Count == 0) throw new MovementException("No valid path found");

            MoveAlongPath(movementPath);
            WaitForNeutral();

            Thread.Sleep(100);

            Interact(poiNextArea.Position, new UnitType[] { UnitType.Tile, UnitType.Object });
            // wait for the next level to load
            Common.WaitForLoading(currentAreaData.Area);

            WaitForNeutral();
        }

        private static void MoveToNextAreaNoPortal(PointOfInterest poiNextArea)
        {
            var gameData = Core.GetGameData();
            var currentAreaData = Core.GetAreaData();
            var nextArea = poiNextArea.NextArea;
            var currentArea = currentAreaData.Area;

            var nextAreaData = currentAreaData.AdjacentAreas.FirstOrDefault(x => x.Key == nextArea).Value;
            var nextLevel = currentAreaData.AdjacentLevels.FirstOrDefault(x => x.Key == nextArea).Value;
            var currentLevel = nextAreaData.AdjacentLevels.FirstOrDefault(x => x.Key == currentArea).Value;

            var movementPath = Pathing.GetPathToLocation(gameData.PlayerPosition, poiNextArea.Position);

            if (movementPath.Path.Count == 0) throw new MovementException("No valid path found");

            if (nextLevel != null && currentLevel != null)
            {
                var startPoint = nextLevel.Exits.FirstOrDefault(x => Pathing.CalculateDistance(x, poiNextArea.Position) < 10);
                var endPoint = currentLevel.Exits.FirstOrDefault(x => Pathing.CalculateDistance(x, poiNextArea.Position) < 10);
                var pointInNextArea = Common.GetPointPastPointInSameDirection(startPoint, endPoint, 8);
                Pathing.currentPath.Add(pointInNextArea);
            }
            else
            {
                var startPoint = movementPath.Path[Math.Max(movementPath.Path.Count - 2, 0)];
                var endPoint = poiNextArea.Position;
                var pointInNextArea = Common.GetPointPastPointInSameDirection(startPoint, endPoint, 10);
                Pathing.currentPath.Add(pointInNextArea);
            }

            MoveAlongPath(movementPath);
            WaitForNeutral();
        }

        public static void MoveToNextArea()
        {
            var tryCount = 0;

            var gameData = Core.GetGameData();
            var currentAreaData = Core.GetAreaData();
            var pointsOfInterest = Core.GetPois();

            if (gameData == null || currentAreaData == null || pointsOfInterest == null) throw new MovementException("GameData/AreaData/Pois null");

            var currentArea = currentAreaData.Area;
            PointOfInterest poiNextArea = pointsOfInterest.Find(x => (x.Type == PoiType.NextArea) && (x.Area == currentArea));
            var nextArea = poiNextArea.NextArea;
            var nextLevel = currentAreaData.AdjacentLevels.FirstOrDefault(x => x.Key == nextArea).Value;

            if (poiNextArea == null) throw new MovementException("No NextArea poi found");

            do
            {
                tryCount++;

                _log.Info("Starting Movement ("+ tryCount + ") to next Area " + nextArea);

                try
                {
                    if(nextLevel != null && nextLevel.IsPortal)
                    {
                        MoveToNextAreaPortal(poiNextArea);
                    }
                    else
                    {
                        MoveToNextAreaNoPortal(poiNextArea);
                    }
                }
                catch (Exception e)
                {
                    _log.Info(e.ToString());
                    continue;
                }

                currentAreaData = Core.GetAreaData();

                if (currentAreaData.Area == nextArea) return;

            } while (tryCount < 4);

            throw new MovementException("Failed to reach next area");
        }

        public static bool LootItemsOnGround()
        {
            GameData gameData = Core.GetGameData();

            var itemsOnGround = Core.GetGameData().AllItems.Where(i => i.ItemModeMapped == ItemModeMapped.Ground).ToList().OrderBy(x => Pathing.CalculateDistance(x.Position, gameData.PlayerPosition));
            var itemsToPickup = Inventory.GetAllItemsToKeep(itemsOnGround.ToArray());

            var success = true;

            foreach (var item in itemsToPickup)
            {
                success &= PickUpItem(item);
            }

            return success;
        }

        internal static bool PickUpItem(UnitItem item)
        {
            _log.Info("Picking up item: " + item.HashString);
            //TODO clearArea before attempting to pick up the item

            GameData gameData = Core.GetGameData();
            AreaData areaData = Core.GetAreaData();
            var rect = Common.GetGameBounds(); 
            var xOffset = rect.Right * -0.01f;
            var yOffset = rect.Bottom * 0.01f;
            var success = false;
            var tryCounter = 0;

            do
            {
                tryCounter++;
                var distance = Pathing.CalculateDistance(item.Position, gameData.PlayerPosition);

                if (distance > 10)
                {
                    _log.Info("Item too far away, moving");
                    try
                    {
                        MoveToPoint(item.Position);
                    }
                    catch (MovementException e)
                    {
                        _log.Info(e.ToString());
                        continue;
                    }
                    gameData = Core.GetGameData();
                }

                _log.Info("Item close enough, trying to pick it up");

                Input.SetCursorPos(new Point(0, 0));
                Thread.Sleep(50);
                var (_, screenCoord) = Common.WorldToScreen(gameData, areaData, item.Position, gameData.PlayerPosition);

                var cursorPos = new Point(screenCoord.X + xOffset, screenCoord.Y + yOffset);
                Input.SetCursorPos(cursorPos);
                Thread.Sleep(50);
                var hoverData = GameMemory.GetCurrentHoverData();
                if(!hoverData.IsHovered || hoverData.UnitType != UnitType.Item)
                {
                    cursorPos = new Point(screenCoord.X, screenCoord.Y);
                    Input.SetCursorPos(cursorPos);
                    Thread.Sleep(50);
                }
                Input.LeftMouseClick(cursorPos);


                WaitForNeutral();
                Thread.Sleep(100);
                gameData = Core.GetGameData();

                var pickedUpItem = gameData.AllItems.FirstOrDefault(x => x.HashString == item.HashString && x.ItemModeMapped != ItemModeMapped.Ground);
                if (pickedUpItem == null)
                {
                    success = false;
                    continue;
                }
                else
                {
                    success = true;
                }

            } while (!success && tryCounter < 10);

            return success;
        }

        public static void MoveToWaypoint()
        {
            _log.Info("Starting Movement to Waypoint");

            PointOfInterest poi = Core.GetPois().Find(x => x.Type == PoiType.Waypoint);
            if (poi == null)
            {
                throw new MovementException("No waypoint found in this area");
            }

            MoveToPoint(poi.Position);
        }


        public static bool MoveToNpc(Npc npc)
        {
            var success = false;
            var tryCount = 0;
            MovementPath movementPath;

            do
            {
            tryCount++;

            var gameData  = Core.GetGameData();
            var areaData = Core.GetAreaData();

            var npcKeyValuePair = areaData.NPCs.FirstOrDefault(x => x.Key == npc && x.Value.Count() > 0);
            var monster = gameData.Monsters.FirstOrDefault(m => m.Npc == npc);

            if(monster == null)
            {
                if (!areaData.NPCs.ContainsKey(npc))
                {
                    success = false;
                    continue;
                }
                if (areaData.NPCs[npc].Length == 0)
                {
                    success = false;
                    continue;
                }
                var npcPosition = areaData.NPCs[npc][0];

                movementPath = Pathing.GetPathToLocation(gameData.PlayerPosition, npcKeyValuePair.Value[0]);
                if (movementPath.Path.Count() == 0)
                {
                    success = false;
                    continue;
                }
                movementPath.Path = movementPath.Path.Take((int)(movementPath.Path.Count() * 0.8)).ToList();

                try
                {
                    MoveAlongPath(movementPath);
                }
                catch(MovementException e)
                {
                        _log.Info(e.ToString());
                        continue;
                }

                gameData = Core.GetGameData();
                areaData = Core.GetAreaData();

                monster = gameData.Monsters.FirstOrDefault(m => m.Npc == npc);
                if (monster == null)
                {
                    success = false;
                    continue;
                }
            }


            gameData = Core.GetGameData();

            if (Pathing.CalculateDistance(monster.Position, gameData.PlayerPosition) < 4)
            {
                return true;
            }

             movementPath = Pathing.GetPathToLocation(gameData.PlayerPosition, monster.Position);

            if(movementPath.Path.Count() != 0)
            {
                movementPath.Path = movementPath.Path.Take(movementPath.Path.Count - 1).ToList();
                try
                {
                    MoveAlongPath(movementPath);
                }
                catch(MovementException e)
                {
                    _log.Info(e.ToString());
                    continue;
                }
            }

            gameData = Core.GetGameData();

            if (Pathing.CalculateDistance(monster.Position, gameData.PlayerPosition) > 3)
            {
                success = false;
                continue;
            }

            } while (success == false && tryCount < 4);


            return success;
        }

        public static void MoveToPoint(int x, int y)
        {
            MoveToPoint(new Point(x, y));
        }
        public static void MoveToPoint(Point destination)
        {
            var tryCnt = 0;

            if (Pathing.CalculateDistance(destination, Core.GetGameData().PlayerPosition) < 3) return;

            do
            {
                tryCnt++;
                var path = Pathing.GetPathToLocation(Core.GetGameData().PlayerPosition, destination);
                if (path.Path.Count() == 0) continue;

                try
                {
                    MoveAlongPath(path);
                }
                catch(MovementException e)
                {
                    _log.Info(e.ToString());
                    continue;
                }
                break;
            } while (tryCnt < 4);
        }

        public static void MoveAlongPath(MovementPath movementPath)
        {
            //TODO attack monster when its blocking the path
            //TODO cancel movement when the client window is not in foreground
            _log.Info("Starting to move along path");

            var path = movementPath.Path;

            if (path == null || path.Count() == 0) throw new MovementException("Path is invalid");

            var gameData = Core.GetGameData();
            var areaData = Core.GetAreaData();

            // TODO maybe replace currPlayer and use the current Core.GetGameData()
            UnitPlayer currPlayer = gameData.PlayerUnit;
            if (currPlayer == null) throw new MovementException("Could not get currPlayerPos");

            var pointNotReachedCounter = 0;

            switch (movementPath.Mode)
            {
                case MovementMode.Teleport:
                    for (var i = 1; i < path.Count; i++)
                    {
                        if (pointNotReachedCounter == 3) throw new MovementException("Failed to reach next point on path");
                        Point p = path[i];

                        var (outOfBounds, screenCoord) = Common.WorldToScreen(gameData, areaData, p, currPlayer.Position);
                        Input.SetCursorPos(screenCoord);
                        Thread.Sleep(15);
                        Input.KeyPress(BotConfig.SkillConfig.Teleport, 15);
                        do
                        {
                            Thread.Sleep(30);
                            gameData = Core.GetGameData();
                            currPlayer = gameData.PlayerUnit;
                        } while (currPlayer.Mode == Mode.Cast);
                        if (outOfBounds && Pathing.CalculateDistance(p, currPlayer.Position) > 9)
                        {
                            i--;
                            pointNotReachedCounter++;
                        }
                        else
                        {
                            pointNotReachedCounter = 0;
                        }
                    }
                    break;

                case MovementMode.Walking:
                    var currentNode = 0;
                    var destination = path.Last();

                    if (Pathing.CalculateDistance(destination, currPlayer.Position) < 7)
                    {
                        var (_, screenCoord) = Common.WorldToScreen(gameData, areaData, destination, currPlayer.Position);
                        Input.SetCursorPos(screenCoord);
                        Thread.Sleep(100);
                        Input.KeyPress(Keys.E);
                        WaitForNeutral();
                        Thread.Sleep(100);
                        gameData = Core.GetGameData();
                        currPlayer = gameData.PlayerUnit;
                        if (Pathing.CalculateDistance(destination, currPlayer.Position) < 3) return;
                    }

                    Input.KeyDown(Keys.E);
                    do
                    {
                        currentNode += 2;
                        Point p = path[Math.Min(currentNode, path.Count - 1)];
                        pointNotReachedCounter = 0;

                        do
                        {
                            pointNotReachedCounter++;
                            if (pointNotReachedCounter > 1000) 
                            {
                                Input.KeyUp(Keys.E);
                                throw new MovementException("Failed to reach next point on path");
                            }
                            if(pointNotReachedCounter % 100 == 0)
                            {
                                _log.Info("Path obstructed? ("+ pointNotReachedCounter+")");
                                Input.KeyUp(Keys.E);
                                Thread.Sleep(25);
                                Input.KeyDown(Keys.E);
                            }

                            var (_, screenCoord) = Common.WorldToScreen(gameData, areaData, p, currPlayer.Position);

                            Input.SetCursorPos(screenCoord);

                            Thread.Sleep(5);
                            gameData = Core.GetGameData();
                            currPlayer = gameData.PlayerUnit;

                            if (currentNode >= path.Count-1)
                            {
                                Input.KeyUp(Keys.E);
                            }

                        } while (Pathing.CalculateDistance(p, currPlayer.Position) > 2);
                    } while (currentNode < path.Count);
                    Input.KeyUp(Keys.E);
                    break;
            }
        }

        public static void WaitForNeutral()
        {
            GameData gameData;
            var counter = 0;
            do
            {
                counter++;
                Thread.Sleep(50);
                gameData = Core.GetGameData();
                if (gameData == null) continue;
            } while (gameData.PlayerUnit.Mode != Mode.TNeutral && gameData.PlayerUnit.Mode != Mode.Neutral && counter < 200);

        }
    }

    [Serializable]
    public class MovementException : Exception
    {
        public MovementException(string message) : base(message)
        {
        }

        public MovementException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
