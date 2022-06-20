using MapAssist.Helpers;
using PrroBot.Builds;
using PrroBot.GameInteraction;
using PrroBot.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PrroBot
{
    public static class PrroBot
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static Thread _mainThread = null;
        private static bool _running = false;
        private static Build _build;
        private static List<Run> _runs = new List<Run>();


        public static Build GetBuild()
        {
            return _build;
        }

        private static void Run()
        {
            while(true)
            {

                while(_running)
                {
                    
                    if (_build.UseLifeguard) Lifeguard.Start();
                    var success = Common.StartGame(2);
                    if(!success)
                    {
                        _log.Error("Failed to start the game. Aborting");
                        _running = false;
                        break;
                    }

                    for (var i = 0; i < _runs.Count(); i++)
                    {
                        if (!_running) break;
                        BotStats.CurrentRunInSequence = i + 1;
                        BotStats.LogNewRun(_runs[i]);
                        try
                        {
                            _runs[i].Execute(_build);
                        }
                        catch(LifeguardException ex)
                        {
                            _log.Error(ex.ToString());
                            BotStats.LogFailedRun(_runs[i]);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.ToString());
                            BotStats.LogFailedRun(_runs[i]);
                            try
                            {
                                Movement.ToTownViaPortal(_build);
                            }
                            catch (Exception ex2)
                            {
                                _log.Error(ex2.ToString());
                                break;
                            }
                        }
                    }

                    if(_running) Common.ExitGame();
                    if (_build.UseLifeguard) Lifeguard.Stop();
                    Thread.Sleep(5000);
                }

                Thread.Sleep(3000);
            }
        }

        public static void Start(Build build, List<Run> runs)
        {
            if (_mainThread == null)
            {
                _mainThread = new Thread(new ThreadStart(Run));
                _mainThread.Start();
            }

            if (_running) return;

            _build = build;
            _runs = runs;

            _running = true;

            BotStats.Running = true;
            BotStats.NewRunSequence(runs);
        }

        public static void Stop()
        {
            _running = false;
            BotStats.Running = false;
        }


        public static void KeyDownHandler(object sender, KeyEventArgs args)
        {
            if (GameManager.IsGameInForeground)
            {
                if (args.KeyCode == Keys.F9)
                {
                    try
                    {
                        Movement.MoveToNextArea();
                    }
                    catch (Exception ex)
                    {
                        _log.Info(ex.Message);
                    }
                }

                if (args.KeyCode == Keys.F10)
                {
                    try
                    {
                        Movement.MoveToQuest(true);
                    }
                    catch (Exception ex)
                    {
                        _log.Info(ex.Message);
                    }
                }

                if (args.KeyCode == Keys.F11)
                {
                    try
                    {
                        Movement.MoveToWaypoint();
                    }
                    catch (Exception ex)
                    {
                        _log.Info(ex.Message);
                    }
                }

                if (args.KeyCode == Keys.F12)
                {
                    if (_running)
                    {
                        Stop();
                    }
                    else
                    {
                        var build = new Hammerdin();
                        var runs = new List<Run>
                        {
                            new Countess(),
                            new Andariel(),
                            new Summoner(),
                            new Mephisto(),
                            new Nihlathak(),
                            new Diablo()
                        };

                        Start(build, runs);
                    }
                }


                if (args.KeyCode == Keys.Delete)
                {
                    
                }
                
            }
        }

    }

}
