using MapAssist.Types;
using PrroBot.Builds;
using PrroBot.GameInteraction;
using System.Threading;

namespace PrroBot.Runs
{
    internal class Summoner : Run
    {
        public override void Execute(Build build)
        {
            /* Summoner: */
            Town.DoTownChores();

            Movement.TakeWaypoint(1, 7);

            build.DoPrebuffs();

            Movement.MoveToQuest(false);

            build.KillSingleTarget(Npc.Summoner);

            build.ClearArea(25);
            Thread.Sleep(1000);

            Movement.LootItemsOnGround();
            Thread.Sleep(1000);

            Movement.ToTownViaPortal(build);
            Thread.Sleep(5000);
        }

        public override string GetName()
        {
            return "Summoner";
        }
    }
}
