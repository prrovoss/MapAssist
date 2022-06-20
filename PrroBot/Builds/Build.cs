using GameOverlay.Drawing;
using MapAssist.Structs;
using MapAssist.Types;
using NLog;
using PrroBot.GameInteraction;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace PrroBot.Builds
{
    public abstract class Build
    {
        protected static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public bool UseMerc = true;
        public bool UseLifeguard = true;

        public abstract bool DoPrebuffs();
        public virtual void KillSingleTarget(Npc npc)
        {
            var monster = Core.GetGameData().Monsters.FirstOrDefault(x => x.Npc == npc);
            if (monster == null) throw new BuildException("Monster not found");
            KillSingleTarget(monster);
        }
        public abstract void KillSingleTarget(UnitMonster monster);
        public virtual void KillSingleTargetSuperUnique(Npc npc)
        {
            var monster = Core.GetGameData().Monsters.FirstOrDefault(x => x.Npc == npc && x.MonsterType.HasFlag(MonsterTypeFlags.SuperUnique));
            if (monster == null) throw new BuildException("Monster not found");
            KillSingleTarget(monster);
        }

        public virtual void ClearArea(int radius = 20)
        {
            ClearArea(Core.GetGameData().PlayerPosition, radius);
        }
        public virtual void ClearArea(Point startPos, int radius)
        {
            //TODO limit overall runtime to x minutes with a proper timer/timestamp
            var monstersInRange = Common.GetMonstersInRange(startPos, radius);

            var failedCount = 0;

            UnitMonster lastFailedMonster = null;

            while (monstersInRange != null && monstersInRange.Count > 0 && failedCount < 10)
            {
                var nextMonster = monstersInRange[0];
                if(lastFailedMonster != null && nextMonster.UnitId == lastFailedMonster.UnitId)
                {
                    /* we have failed to kill this monster in the previous try, so try another one now. */
                    var rnd = new Random();
                    nextMonster = monstersInRange[rnd.Next(monstersInRange.Count)];
                }

                if (nextMonster != null)
                {
                    try
                    {
                        KillSingleTarget(nextMonster);
                        lastFailedMonster = null;
                    }
                    catch (BuildException e)
                    {
                        _log.Info(e.ToString());
                        failedCount++;
                        lastFailedMonster = nextMonster;
                    }
                }

                monstersInRange = Common.GetMonstersInRange(startPos, radius);
            }

            Movement.LootItemsOnGround();
            Movement.MoveToPoint(startPos);
        }
    }

    [System.Serializable]
    internal class BuildException : System.Exception
    {
        public BuildException()
        {
        }

        public BuildException(string message) : base(message)
        {
        }

        public BuildException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        protected BuildException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
