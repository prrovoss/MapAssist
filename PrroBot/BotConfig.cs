using MapAssist.Types;
using System.Windows.Forms;

namespace PrroBot
{
    public static class BotConfig
    {
		//TODO read config from file. maybe use the MapAssist config file/ui?
		
        /* configures which spaces in the inventory are available (1==available, 0==reserved).
         * the bot will sell or stash all items that are stored in slots configured with a 1.
         * so configure the slots that hold you anni, torch, tome etc with a 0 */
        public static readonly int[][] InventoryMap = new[]  { new[] { 1, 1, 1, 1, 1, 1, 1, 0, 0, 0 },                                         
                                                                new[] { 1, 1, 1, 1, 1, 1, 1, 0, 0, 0 },
                                                                new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 },
                                                                new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 }
                                                               };

        /* the bot will use these stash tabs to store found items (starting from 0) */
        public static readonly int[] StashTabsForStoring = { 1, 2, 3 };

        /* the bot will search in these stash tabs to refill the rejuv potions */
        public static readonly int[] StashTabsForRefillRejuvs = { 0, 1, 2, 3 };

        /* specifies which potion type should be present in the belt slots */
        public static readonly PotionType[] BeltConfig = { PotionType.HealingPotion, PotionType.HealingPotion, PotionType.ManaPotion, PotionType.RejuvenationPotion };

        public static bool ShowDebugInfo = false;

        public static class SkillConfig
        {
            /* these skills are needed for the hammerdin build. maybe move this to the build class later on */
            public const Keys BlessedHammer = Keys.F1;
            public const Keys Teleport = Keys.F2;
            public const Keys Concentration = Keys.F3;
            public const Keys HolyShield = Keys.F4;
            public const Keys BattleCommand = Keys.F5;
            public const Keys BattleOrders = Keys.F6;
            public const Keys TownPortal = Keys.F8;
        }
    }
}
