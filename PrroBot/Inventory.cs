using GameOverlay.Drawing;
using MapAssist.Helpers;
using MapAssist.Types;
using System.Collections.Generic;
using System.Linq;
using PrroBot.GameInteraction;

namespace PrroBot
{ 
    public static class Inventory
    {
        public static UnitItem[] GetAllItemsDamaged(UnitItem[] allItems, float damageThreshold = 0.6f)
        {
            var result = new List<UnitItem>();
            foreach (UnitItem item in allItems)
            {
                double durability = item.Stats.ContainsKey(Stats.Stat.Durability) ? item.Stats[Stats.Stat.Durability] : 100;
                double maxDurability = item.Stats.ContainsKey(Stats.Stat.MaxDurability) ? item.Stats[Stats.Stat.MaxDurability] : 100;

                if (durability / maxDurability < damageThreshold) result.Add(item);
            }
            return result.ToArray();
        }


        public static UnitItem[] GetAllItemsEquipped(UnitItem[] allItems) => allItems.Where(item => item.ItemMode == ItemMode.EQUIP).ToArray();

        public static UnitItem[] GetAllItemsInPlayerInventory(UnitItem[] allItems) => allItems.Where(item => item.ItemModeMapped == ItemModeMapped.Inventory).ToArray();

        public static UnitItem[] GetAllUnidentifiedItems(UnitItem[] allItems) => allItems.Where(item => !item.IsIdentified && item.ItemData.ItemQuality == ItemQuality.UNIQUE).ToArray();

        public static UnitItem[] GetAllItemsToKeep(UnitItem[] allItems)
        {
            var result = new List<UnitItem>();
            var gameData = Core.GetGameData();

            foreach (UnitItem item in allItems)
            {
                var (keep, _) = LootFilter.Filter(item, gameData.Area.Level(gameData.Difficulty), gameData.PlayerUnit.Level);
                if (keep)
                {
                    if (item.ItemModeMapped == ItemModeMapped.Inventory && BotConfig.InventoryMap[(int)item.Y][(int)item.X] > 0)
                    {
                        result.Add(item);
                    }
                    else if(item.ItemModeMapped == ItemModeMapped.Ground)
                    {
                        result.Add(item);
                    }
                }
            }
            return result.ToArray();
        }

        public static UnitItem[] GetAllItemsToSell(UnitItem[] allItems)
        {
            var result = new List<UnitItem>();
            var gameData = Core.GetGameData();

            foreach (UnitItem item in allItems)
            {
                var (keep, _) = LootFilter.Filter(item, gameData.Area.Level(gameData.Difficulty), gameData.PlayerUnit.Level);
                if (!keep && BotConfig.InventoryMap[(int)item.Y][(int)item.X] > 0) result.Add(item);
            }
            return result.ToArray();
        }

        
        public static Point GetItemScreenPos(GameData gameData, UnitItem item)
        {
            var rect = Common.GetGameBounds();

            var baseX = rect.Right * 0.1f;
            var baseY = rect.Bottom * 0.23f;

            if (item.ItemModeMapped == ItemModeMapped.Inventory)
            {
                // inventory
                baseX = rect.Right * 0.67f;
                baseY = rect.Bottom * 0.53f;
            }
            var cellSize = rect.Right * 0.0256f;
            var finalX = baseX + cellSize * item.X;
            var finalY = baseY + cellSize * item.Y;
            return new Point(finalX, finalY);
        }

        public static Point GetStashTabScreenPos(GameData gameData, StashTab tab)
        {
            return GetStashTabScreenPos(gameData, MapStashTabToInt(tab));
        }

        public static Point GetStashTabScreenPos(GameData gameData, int idx)
        {
            var rect = Common.GetGameBounds();

            var baseX = rect.Right * 0.115f;
            var baseY = rect.Bottom * 0.19f;

            var tabSize = rect.Right * 0.065f;

            var finalX = baseX + tabSize * idx;

            return new Point(finalX, baseY);
        }

        private static int MapStashTabToInt(StashTab stashTab)
        {
            switch (stashTab)
            {
                case StashTab.Personal: return 0;
                case StashTab.Shared1: return 1;
                case StashTab.Shared2: return 2;
                case StashTab.Shared3: return 3;
                case StashTab.None:
                default: return -1;
            }
        }

        public static bool IsStashtabAvailForRefillRejuvs(StashTab stashTab)
        {
            var mappedTab = MapStashTabToInt(stashTab);
            switch (stashTab)
            {
                case StashTab.Personal: return BotConfig.StashTabsForRefillRejuvs.Contains(mappedTab);
                case StashTab.Shared1: return BotConfig.StashTabsForRefillRejuvs.Contains(mappedTab);
                case StashTab.Shared2: return BotConfig.StashTabsForRefillRejuvs.Contains(mappedTab);
                case StashTab.Shared3: return BotConfig.StashTabsForRefillRejuvs.Contains(mappedTab);
                case StashTab.None:
                default: return false;
            }
        }
    }
}
