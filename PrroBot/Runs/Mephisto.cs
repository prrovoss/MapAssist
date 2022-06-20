using GameOverlay.Drawing;
using MapAssist.Types;
using PrroBot.Builds;
using PrroBot.GameInteraction;
using System.Threading;

namespace PrroBot.Runs
{
    internal class Mephisto : Run
    {
        public override void Execute(Build build)
        {
            Town.DoTownChores();

            Movement.TakeWaypoint(2, 8);

            build.DoPrebuffs();

            Movement.MoveToNextArea();

            Movement.MoveToPoint(17564, 8069);
            Thread.Sleep(1500);

            build.KillSingleTarget(Npc.Mephisto);

            Thread.Sleep(1000);

            Movement.LootItemsOnGround();

            Movement.MoveToPoint(17590, 8069);

            Thread.Sleep(3000);

            Movement.MoveToPoint(17602, 8069);

            Movement.Interact(new Point(17602, 8069), UnitType.Object);

            Common.WaitForLoading(Area.DuranceOfHateLevel3);

            Thread.Sleep(5000);
        }

        public override string GetName()
        {
            return "Mephisto";
        }
    }
}
