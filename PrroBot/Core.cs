using MapAssist.Helpers;
using MapAssist.Types;
using System.Collections.Generic;
using System.Threading;

namespace PrroBot
{
    internal class Core
    {
        private static GameData GameData;
        private static readonly object GameData_lock = new object();
        private static bool lastGameDataWasNull = false;

        private static AreaData AreaData;
        private static readonly object AreaData_lock = new object(); 
        private static bool lastAreaDataWasNull = false;

        private static List<PointOfInterest> PointsOfInterest;
        private static readonly object PointsOfInterest_lock = new object();

        private bool run = true;
        private readonly GameDataReader _reader;
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public Core(GameDataReader gameDataReader)
        {
            _reader = gameDataReader;
        }

        public static bool LastGameDataWasNull()
        {
            return lastGameDataWasNull;
        }

        public static bool LastAreaDataWasNull()
        {
            return lastAreaDataWasNull;
        }

        public static GameData GetGameData()
        {
            lock (GameData_lock)
            {
                return GameData?.ShallowCopy();
            }
        }

        public static AreaData GetAreaData()
        {
            lock (AreaData_lock)
            {
                return AreaData?.ShallowCopy();
            }
        }
        public static List<PointOfInterest> GetPois()
        {
            lock (PointsOfInterest_lock)
            {
                return PointsOfInterest != null ? new List<PointOfInterest>(PointsOfInterest) : null;
            }
        }

        public void Start()
        {
            run = true;
            while (run)
            {
                var (gameData, areaData, _) = _reader.Get();

                var pointsOfInterest = areaData?.PointsOfInterest;

                if (gameData != null)
                {
                    lock (GameData_lock)
                    {
                        GameData = gameData;
                        lastGameDataWasNull = false;
                    }
                }
                else
                {
                    lastGameDataWasNull = true;
                    //_log.Info("GameData null");
                }

                if(areaData != null)
                {
                    lock (AreaData_lock)
                    {
                        AreaData = areaData;
                        lastAreaDataWasNull = false;
                    }
                }
                else
                {
                    lastAreaDataWasNull = true;
                    //_log.Info("AreaData null");
                }

                if (pointsOfInterest != null)
                {
                    lock (PointsOfInterest_lock)
                    {
                        PointsOfInterest = pointsOfInterest;
                    }
                }
                else
                {
                    //_log.Info("PointsOfInterest null");
                }

                Thread.Sleep(10);
            }
        }

        public void Stop()
        {
            run = false;
        }

    }
}
