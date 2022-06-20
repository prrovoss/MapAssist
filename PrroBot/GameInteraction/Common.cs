using GameOverlay.Drawing;
using GameOverlay.Windows;
using MapAssist.Helpers;
using MapAssist.Structs;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PrroBot.GameInteraction
{
    public static class Common
    {
        private static readonly float _rotateRadians = (float)(45 * Math.PI / 180f);
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public static (bool, Point) WorldToScreen(GameData gameData, AreaData areaData, Point targetLocation, Point playerLocation)
        {
            // calculate transform matrices
            var localScaleWidth = 54.25f;
            var localScaleHeight = 27.125f;

            var localMapTransformMatrix = Matrix3x2.CreateTranslation(areaData.Origin.ToVector())
            * Matrix3x2.CreateTranslation(Vector2.Negate(playerLocation.ToVector()))
            * Matrix3x2.CreateRotation(_rotateRadians)
            * Matrix3x2.CreateScale(localScaleWidth, localScaleHeight);

            var rect = GetGameBounds();

            localMapTransformMatrix *= Matrix3x2.CreateTranslation(new Vector2(rect.Right / 2, rect.Bottom / 2))
            * Matrix3x2.CreateTranslation(new Vector2(2, -8)); // Brute forced to perfectly line up with the in game map;

            var localAreaTransformMatrix = Matrix3x2.CreateTranslation(Vector2.Negate(areaData.Origin.ToVector()));
            localAreaTransformMatrix *= localMapTransformMatrix;

            // check if the target location is out of bounds
            var resizeScale = 1f;

            var bounds = new Rectangle(0, 0, rect.Right, rect.Bottom * 0.78f);

            var startScreenCoord = Vector2.Transform(playerLocation.ToVector(), localAreaTransformMatrix);
            var endScreenCoord = Vector2.Transform(targetLocation.ToVector(), localAreaTransformMatrix);

            if (endScreenCoord.X < bounds.Left) resizeScale = Math.Min(resizeScale, (bounds.Left - startScreenCoord.X) / (endScreenCoord.X - startScreenCoord.X));
            if (endScreenCoord.X > bounds.Right) resizeScale = Math.Min(resizeScale, (bounds.Right - startScreenCoord.X) / (endScreenCoord.X - startScreenCoord.X));
            if (endScreenCoord.Y < bounds.Top) resizeScale = Math.Min(resizeScale, (bounds.Top - startScreenCoord.Y) / (endScreenCoord.Y - startScreenCoord.Y));
            if (endScreenCoord.Y > bounds.Bottom) resizeScale = Math.Min(resizeScale, (bounds.Bottom - startScreenCoord.Y) / (endScreenCoord.Y - startScreenCoord.Y));

            if (resizeScale < 1)
            {
                targetLocation = targetLocation.Subtract(playerLocation).Multiply(resizeScale).Add(playerLocation);
            }

            return (resizeScale < 1, Vector2.Transform(targetLocation.ToVector(), localAreaTransformMatrix).ToPoint());
        }

        internal static void CloseAllMenus()
        {
            var menuData = GetCurrentMenuData();
            if (menuData.IsAnyMenuOpen() || menuData.NpcInteract)
            {
                Input.KeyPress(Keys.Escape);
                Thread.Sleep(50);
            }
        }

        private static Point Substract(Point current, Point other)
        {
            return new Point((current.X - other.X), (current.Y - other.Y));
        }

        private static Point Normalize(Point point)
        {
            var length = Math.Sqrt(Math.Pow(point.X, 2.0) + Math.Pow(point.Y, 2.0));
            return new Point((float)(point.X / length), (float)(point.Y / length));
        }

        public static Point GetPointPastPointInSameDirection(Point current, Point other, double distance)
        {
            var difference = Substract(current, other);
            var normalized = Normalize(difference);
            return new Point((ushort)(other.X - normalized.X * distance), (ushort)(other.Y - normalized.Y * distance));
        }

        public static List<Point> GetPointsSurroundingPoint(Point startPoint, int distance)
        {
            var offsets = new List<(int, int)>();

            for (var xOffset = -distance; xOffset <= distance; xOffset++)
            {
                for (var yOffset = -distance; yOffset <= distance; yOffset++)
                {
                    offsets.Add((xOffset, yOffset));
                }
            }

            offsets = offsets.OrderBy(x => Math.Abs(x.Item1) + Math.Abs(x.Item2)).ToList();

            var offsetPoints = new List<Point>();

            foreach (var offset in offsets)
            {
                offsetPoints.Add(new Point(startPoint.X + offset.Item1, startPoint.Y + offset.Item2));
            }

            return offsetPoints;
        }

        public static void ExitGame() //TODO throw exception if failed?
        {
            var menuData = GetCurrentMenuData();
            if (!menuData.InGame) return;

            CloseAllMenus();

            var rect = GetGameBounds();
            Input.KeyPress(Keys.Escape);
            Thread.Sleep(100);
            Input.LeftMouseClick(new Point(rect.Right * 0.5f, rect.Bottom * 0.44f));

            var maxWaitCounter = 0;

            do
            {
                maxWaitCounter++;
                Thread.Sleep(100);
                menuData = GetCurrentMenuData();
            } while ((menuData.LoadingScreen || menuData.InGame) && maxWaitCounter < 150);
        }

        public static void WaitForLoading(Area startingArea) 
        {
            GameData gameData;
            var maxWaitCounter = 0;

            do
            {
                maxWaitCounter++;
                Thread.Sleep(100);
                gameData = Core.GetGameData();
            } while ((Core.LastGameDataWasNull() || gameData.Area == startingArea || gameData.MenuOpen.LoadingScreen) && maxWaitCounter < 150);
        }

        public static WindowBounds GetGameBounds()
        {
            WindowHelper.GetWindowClientBounds(GameManager.MainWindowHandle, out WindowBounds rect);
            return rect;
        }

        public static bool StartGame(int difficulty)
        {
            var menuData = GetCurrentMenuData();
            if (menuData.InGame) return true;

            var rect = GetGameBounds();
            Input.LeftMouseClick(rect.Right * 0.47f, rect.Bottom * 0.90f);
            Thread.Sleep(250);

            var x = rect.Right * 0.5f;
            var y = rect.Bottom * 0.42f;
            y += rect.Bottom * 0.06f * difficulty;

            Input.LeftMouseClick(x, y);

            WaitForLoading(Area.None);

            menuData = GetCurrentMenuData();
            if (menuData.InGame) return true;

            return false;
        }


        public static UnitObject GetPlayerPortal()
        {
            var gameData = Core.GetGameData();
            var portal = gameData.Objects.FirstOrDefault(x => x.IsPortal && Encoding.UTF8.GetString(x.ObjectData.Owner).TrimEnd((char)0).Equals(gameData.PlayerName));
            if (portal == null)
            {
                _log.Info("Could not find a player portal, falling back to any TownPortal");
                portal = gameData.Objects.FirstOrDefault(x => x.GameObject == GameObject.TownPortal);
                if (portal == null)
                {
                    _log.Info("Could not find any TownPortal nearby");
                }
            }
            return portal;
        }

        public static List<UnitMonster> GetMonstersInRange(Point startPos, int range)
        {
            var gameData = Core.GetGameData();
            /* get all monsters in range ordered by current distance to the player unit */
            return gameData.Monsters.Where(x => !x.IsCorpse && Pathing.CalculateDistance(startPos, x.Position) < range).OrderBy(x => Pathing.CalculateDistance(x.Position, gameData.PlayerPosition)).ToList();
        }

        public static MenuData GetCurrentMenuData()
        {

            var processContext = GameManager.GetProcessContext();

            if (processContext == null)
            {
                return new MenuData();
            }

            return processContext.Read<MapAssist.Structs.MenuData>(GameManager.MenuDataOffset);
        }
    }
}
