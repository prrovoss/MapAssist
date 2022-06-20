using MapAssist.Types;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;

namespace PrroBot.GameInteraction
{
    public static class Lifeguard
    {
        //TODO expand to watchdog? monitor player position, state, etc and cancel when stuck
        private static bool run = true;
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static DateTime lastHealing;
        private static DateTime lastMana;
        private static Thread thread = null;
        public static bool PanicMode = false;

        private static bool ShouldExitGame(GameData gameData)
        {
            return gameData.PlayerUnit.LifePercentage < 25;
        }

        private static bool ShouldDrinkRejuv(GameData gameData)
        {
            return gameData.PlayerUnit.LifePercentage < 50;
        }

        private static bool ShouldDrinkHealing(GameData gameData)
        {
            var span = DateTime.Now - lastHealing;
            var ms = span.TotalMilliseconds;
            if(ms > 5000)
            {
                return gameData.PlayerUnit.LifePercentage < 75;
            }
            else
            {
                return false;
            }
        }

        private static bool ShouldDrinkMana(GameData gameData)
        {
            var span = DateTime.Now - lastMana;
            var ms = span.TotalMilliseconds;
            if (ms > 5000)
            {
                return gameData.PlayerUnit.ManaPercentage < 50;
            }
            else
            {
                return false;
            }
        }

        public static void CheckInteractionPossible()
        {
            if(PanicMode) throw new LifeguardException("Panic Mode active");
        }

        private static bool GetPotionKey(GameData gameData, PotionType type, out Keys key)
        {
            key = 0;
            for (var i = 0; i < 4; i++)
            {
                if (BotConfig.BeltConfig[i] == type && gameData.PlayerUnit.BeltItems[i][0] != null)
                {
                    key = MapBeltIdxToKey(i);
                    return true;
                }
            }
            return false;
        }

        private static Keys MapBeltIdxToKey(int idx)
        {
            switch(idx)
            {
                case 0: return Keys.D1;
                case 1: return Keys.D2;
                case 2: return Keys.D3;
                case 3: return Keys.D4;
                default: return Keys.D5;
            }
        }

        public static void Run()
        {
            _log.Info("Lifeguard started");
            GameData gameData;
            while (run)
            {
                Thread.Sleep(250);
                gameData = Core.GetGameData();

                if (Core.LastGameDataWasNull() || gameData.Area == Area.None || gameData.Area.IsTown()) continue;

                if (ShouldExitGame(gameData))
                {
                    Common.ExitGame();
                    PanicMode = true;
                    BotStats.Chicken++;
                    continue;
                }

                if (ShouldDrinkRejuv(gameData))
                {
                    if (GetPotionKey(gameData, PotionType.RejuvenationPotion, out var key))
                    {
                        Input.KeyPress(key);
                    }
                    continue;
                }

                if (ShouldDrinkHealing(gameData))
                {
                    if (GetPotionKey(gameData, PotionType.HealingPotion, out var key))
                    {
                        Input.KeyPress(key);
                        lastHealing = DateTime.Now;
                    }
                }

                if (ShouldDrinkMana(gameData))
                {
                    if (GetPotionKey(gameData, PotionType.ManaPotion, out var key))
                    {
                        Input.KeyPress(key);
                        lastMana = DateTime.Now;
                    }
                }

                //TODO also check if merc needs potion
            }
            _log.Info("Lifeguard stopped");
        }

        public static void Stop()
        {
            PanicMode = false;
            run = false;
            thread = null;
        }

        public static void Start()
        {
            PanicMode = false;
            run = true;
            if (thread == null)
            {
                thread = new Thread(new ThreadStart(Run));
                thread.Start();
            }
            else
            {
                _log.Info("Lifeguard already running");
            }
        }
    }

    [Serializable]
    public class LifeguardException : Exception
    {
        public LifeguardException()
        {
        }

        public LifeguardException(string message) : base(message)
        {
        }

        public LifeguardException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LifeguardException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
