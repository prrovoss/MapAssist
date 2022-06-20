using NLog;
using PrroBot.Builds;

namespace PrroBot.Runs
{
    public abstract class Run
    {
        protected static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public abstract void Execute(Build build);
        public abstract string GetName();
    }
}
