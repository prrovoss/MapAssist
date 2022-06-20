using GameOverlay.Drawing;
using MapAssist.Structs;
using MapAssist.Types;
using PrroBot.Builds;
using PrroBot.GameInteraction;
using System.Linq;
using System.Threading;

namespace PrroBot.Runs
{
    internal class Diablo : Run
    {
        private Build _build;

        public override void Execute(Build build)
        {
            //TODO has problems clearing the area around the seals and over non-walkable areas (the ghosts floating over the lava). adapt KillSingleTarget function
            //TODO implement ability to revive merc mid-run, if necessary

            _build = build;
            Town.DoTownChores();

            Movement.TakeWaypoint(3, 2);

            _build.DoPrebuffs();

            Movement.MoveToNextArea();

            var areaData = Core.GetAreaData();

            var Seal1 = areaData.Objects[GameObject.DiabloSeal1][0];
            var Seal2 = areaData.Objects[GameObject.DiabloSeal2][0];
            var Seal3 = areaData.Objects[GameObject.DiabloSeal3][0];
            var Seal4 = areaData.Objects[GameObject.DiabloSeal4][0];
            var Seal5 = areaData.Objects[GameObject.DiabloSeal5][0];
            var DiabloStart = areaData.Objects[GameObject.DiabloStartPoint][0];

            // Seal 1
            ClearSeal(Seal1);
            WaitForMonster(Npc.VenomLord, true);
            _build.KillSingleTargetSuperUnique(Npc.VenomLord);
            _build.ClearArea(15);
            Movement.LootItemsOnGround();

            // Seal 2
            ClearSeal(Seal2);

            // Seal 3
            ClearSeal(Seal3);
            Movement.MoveToPoint(new Point(7773, 5173));
            WaitForMonster(Npc.OblivionKnight, true);
            _build.KillSingleTargetSuperUnique(Npc.OblivionKnight);
            _build.ClearArea(15);
            Movement.LootItemsOnGround();

            // Seal 4
            ClearSeal(Seal4);

            // Seal 5
            ClearSeal(Seal5);
            WaitForMonster(Npc.StormCaster, true);
            _build.KillSingleTargetSuperUnique(Npc.StormCaster);
            _build.ClearArea(15);
            Movement.LootItemsOnGround();

            // Diablo
            Movement.MoveToPoint(DiabloStart);
            WaitForMonster(Npc.Diablo, false);
            _build.KillSingleTarget(Npc.Diablo);
            Thread.Sleep(500);
            Movement.LootItemsOnGround();

            Movement.ToTownViaPortal(_build);
            Thread.Sleep(5000);
        }

        //TODO maybe move to Common
        private void WaitForMonster(Npc npc, bool superUnique)
        {            
            UnitMonster monster = null;
            var counter = 0;

            while (monster == null && counter < 150)
            {
                Thread.Sleep(100);
                monster = Core.GetGameData().Monsters.FirstOrDefault(x => x.Npc == npc && (!superUnique || x.MonsterType.HasFlag(MonsterTypeFlags.SuperUnique)));

                if (monster == null && counter % 10 == 0)
                {
                    _build.ClearArea(15);
                }
            }
        }

        private void ClearSeal(Point seal)
        {
            Movement.MoveToPoint(seal);
            _build.ClearArea(15);
            Movement.MoveToPoint(seal);
            Movement.Interact(seal, UnitType.Object);
            Movement.LootItemsOnGround();
        }

        public override string GetName()
        {
            return "Diablo";
        }
    }
}
