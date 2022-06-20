using PrroBot.Runs;
using System.Collections.Generic;

namespace PrroBot
{
    public static class BotStats
    {
        public static int TotalRuns = 0;
        public static int TotalFailedRuns = 0;
        public static int TotalSuccessRate = 0;
        public static int Deaths = 0;
        public static int Chicken = 0;

        public static int CurrentRunInSequence = 0;
        public static int TotalRunsInSequence = 0;

        public static bool Running = false;

        public static readonly Dictionary<Run, int[]> RunStats = new Dictionary<Run, int[]>();

        public static Run CurrentRun;


        public static void NewRunSequence(List<Run> runs)
        {
            RunStats.Clear();
            foreach(var run in runs)
            {
                RunStats.Add(run, new int[]{0,0,0});
            }

            TotalRunsInSequence = runs.Count;
        }

        public static void LogNewRun(Run run)
        {
            TotalRuns++;
            RunStats[run][0]++;
            CurrentRun = run;
            UpdateSuccessRates(run);
        }

        public static void LogFailedRun(Run run)
        {
            TotalFailedRuns++;
            RunStats[run][1]++;
            UpdateSuccessRates(run);
        }

        private static void UpdateSuccessRates(Run run)
        {
            var successfulRuns = (float)(TotalRuns - TotalFailedRuns);
            TotalSuccessRate = successfulRuns == 0 ? 0 : (int)(successfulRuns / TotalRuns  * 100f);

            successfulRuns = RunStats[run][0] - RunStats[run][1];
            RunStats[run][2] = successfulRuns == 0 ? 0 : (int)(successfulRuns / RunStats[run][0] * 100f);
        }
    }
}
