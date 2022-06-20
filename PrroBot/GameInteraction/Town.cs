using MapAssist.Helpers;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using GameOverlay.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.Serialization;

namespace PrroBot.GameInteraction
{
    //TODO add crafting for gems
    //TODO add crafting for rejuvs
    public static class Town
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static AreaData _areaData;

        private static readonly TownConfig[] configs = { new TownConfig { shopNpc = Npc.Akara,   shopDialogueOption = 1, repairNpc = Npc.Charsi, healNpc = Npc.Akara  , identifyNpc = Npc.DeckardCain5, mercNpcs = new Npc[]{Npc.Kashya                            } },
                                                         new TownConfig { shopNpc = Npc.Drognan, shopDialogueOption = 1, repairNpc = Npc.Fara  , healNpc = Npc.Fara   , identifyNpc = Npc.DeckardCain2, mercNpcs = new Npc[]{Npc.Greiz                             } },
                                                         new TownConfig { shopNpc = Npc.Ormus,   shopDialogueOption = 1, repairNpc = Npc.Hratli, healNpc = Npc.Ormus  , identifyNpc = Npc.DeckardCain3, mercNpcs = new Npc[]{Npc.Asheara                           } },
                                                         new TownConfig { shopNpc = Npc.Jamella, shopDialogueOption = 0, repairNpc = Npc.Halbu , healNpc = Npc.Jamella, identifyNpc = Npc.DeckardCain4, mercNpcs = new Npc[]{Npc.Tyrael  , Npc.Tyrael2, Npc.Tyrael3} },
                                                         new TownConfig { shopNpc = Npc.Malah,   shopDialogueOption = 1, repairNpc = Npc.Larzuk, healNpc = Npc.Malah  , identifyNpc = Npc.DeckardCain6, mercNpcs = new Npc[]{Npc.QualKehk                          } },
                                                       };

        private struct TownConfig
        {
            public Npc shopNpc;
            public int shopDialogueOption;
            public Npc repairNpc;
            public Npc healNpc;
            public Npc identifyNpc;
            public Npc[] mercNpcs;
        }

        private static int MapTownAreaToAct(Area area)
        {
            switch(area)
            {
                case Area.RogueEncampment: return 0;
                case Area.LutGholein: return 1;
                case Area.KurastDocks: return 2;
                case Area.ThePandemoniumFortress: return 3;
                case Area.Harrogath: return 4;
                default: return -1;
            }
        }

        private static TownConfig GetTownConfig(Area area)
        {
            return configs[MapTownAreaToAct(area)];
        }

        public static void DoTownChores()
        {
            _log.Info("Starting Town Chores");

             _areaData = Core.GetAreaData();

            if (!_areaData.Area.IsTown()) throw new TownException("Not in town");

            var townConfig = GetTownConfig(_areaData.Area);
            bool tmp, success = false;

            try
            {
                _log.Info("DoPickUpCorpseTask");
                DoPickUpCorpseTask();
                _log.Info("Done");
            }
            catch (Exception ex)
            {
                //  catch only specific exceptions and propagate all other exceptions to the caller
                if (!(ex is TownException) && !(ex is MovementException))
                {
                    throw ex;
                }
            }

            try
            {
                _log.Info("DoIdentifyTask");
                tmp = DoIdentifyTask(townConfig);
                success &= tmp;
                _log.Info("Done, " + tmp);
            }
            catch (Exception ex)
            {
                //  catch only specific exceptions and propagate all other exceptions to the caller
                if (!(ex is TownException) && !(ex is MovementException))
                {
                    throw ex;
                }
            }

            try
            {
                _log.Info("DoShoppingTask");
                tmp = DoShoppingTask(townConfig);
                success &= tmp;
                _log.Info("Done, " + tmp);
            }
            catch (Exception ex)
            {
                //  catch only specific exceptions and propagate all other exceptions to the caller
                if (!(ex is TownException) && !(ex is MovementException))
                {
                    throw ex;
                }
            }

            try
            {
                _log.Info("DoStoreItemsTask");
                tmp = DoStoreItemsTask();
                success &= tmp;
                _log.Info("Done, " + tmp);
            }
            catch (Exception ex)
            {
                //  catch only specific exceptions and propagate all other exceptions to the caller
                if (!(ex is TownException) && !(ex is MovementException))
                {
                    throw ex;
                }
            }

            try
            {
                _log.Info("DoRepairTask");
                tmp = DoRepairTask(townConfig);
                success &= tmp;
                _log.Info("Done, " + tmp);
            }
            catch (Exception ex)
            {
                //  catch only specific exceptions and propagate all other exceptions to the caller
                if (!(ex is TownException) && !(ex is MovementException))
                {
                    throw ex;
                }
            }

            try
            {
                _log.Info("DoReviveMercTask");
                tmp = DoReviveMercTask(townConfig);
                success &= tmp;
                _log.Info("Done, " + tmp);
            }
            catch (Exception ex)
            {
                //  catch only specific exceptions and propagate all other exceptions to the caller
                if (!(ex is TownException) && !(ex is MovementException))
                {
                    throw ex;
                }
            }

            try
            {
                _log.Info("DoHealTask");
                tmp = DoHealTask(townConfig);
                success &= tmp;
                _log.Info("Done, " + tmp);
            }
            catch (Exception ex)
            {
                //  catch only specific exceptions and propagate all other exceptions to the caller
                if (!(ex is TownException) && !(ex is MovementException))
                {
                    throw ex;
                }
            }


            Thread.Sleep(150);

            Common.CloseAllMenus();

            _log.Info("Finished Town Chores, result: " + success);
        }

        private static bool DoShoppingTask(TownConfig townConfig)
        {
            var gameData = Core.GetGameData();
            if(gameData == null)
            {
                _log.Info("GameData null");
                return false;
            }

            UnitItem[] itemsToSell;
            (var sellItems, var buyPots, var buyPortal) = ShoppingNeeded(gameData, out itemsToSell, out var potionsToBuy);

            var success = false;

            if (sellItems || buyPots || buyPortal)
            {
                success = MoveAndInteractWithTownNpc(townConfig.shopNpc, true, townConfig.shopDialogueOption);

                if (!success)
                {
                    _log.Info("Interact with NPC failed");
                    return false;
                }

                Thread.Sleep(500);

                gameData = Core.GetGameData();
            }
            else
            {
                return true;
            }


            if (sellItems)
            {
                foreach(UnitItem item in itemsToSell)
                {
                    Input.KeyDown(Keys.ControlKey);
                    Thread.Sleep(250);
                    Input.LeftMouseClick(Inventory.GetItemScreenPos(gameData, item));
                    Thread.Sleep(250);
                    Input.KeyUp(Keys.ControlKey);
                    Thread.Sleep(500);
                }
            }

            if(buyPortal)
            {
                var vendorPortalScroll = gameData.AllItems.FirstOrDefault(x => x.Item == Item.ScrollOfTownPortal && x.ItemModeMapped == ItemModeMapped.Vendor);
                if(vendorPortalScroll != null)
                {
                    Input.KeyDown(Keys.LShiftKey);
                    Thread.Sleep(250);
                    Input.RightMouseClick(Inventory.GetItemScreenPos(gameData, vendorPortalScroll));
                    Thread.Sleep(250);
                    Input.KeyUp(Keys.LShiftKey);
                    Thread.Sleep(500);
                }
                else
                {
                    success = false;
                }
            }

            if(buyPots)
            {
                var vendorManaPotions = gameData.AllItems.Where(x => x.ItemModeMapped == ItemModeMapped.Vendor && x.Item.IsManaPotion() && x.VendorOwner == townConfig.shopNpc);
                var vendorHealthPotions = gameData.AllItems.Where(x => x.ItemModeMapped == ItemModeMapped.Vendor && x.Item.IsHealthPotion() && x.VendorOwner == townConfig.shopNpc);

                if (vendorManaPotions.Count() == 0 || vendorHealthPotions.Count() == 0) return false;

                var maxVendorManaQuality = vendorManaPotions.Max(x => x.Item);
                var maxVendorHealthQuality = vendorHealthPotions.Max(x => x.Item);

                var bestManaPotion = vendorManaPotions.FirstOrDefault(x => x.Item == maxVendorManaQuality);
                var bestHealthPotion = vendorHealthPotions.FirstOrDefault(x => x.Item == maxVendorHealthQuality);

                if (bestManaPotion != null && bestHealthPotion != null)
                {
                    var manaScreenPos = Inventory.GetItemScreenPos(gameData, bestManaPotion);
                    var healthScreenPos = Inventory.GetItemScreenPos(gameData, bestHealthPotion);
                    foreach (var potType in potionsToBuy)
                    {
                        if (potType == PotionType.ManaPotion)
                        {
                            Input.RightMouseClick(manaScreenPos);
                            Thread.Sleep(500);
                        }
                        else if(potType == PotionType.HealingPotion)
                        {
                            Input.RightMouseClick(healthScreenPos);
                            Thread.Sleep(500);
                        }
                    }
                }
            }

            Input.KeyPress(Keys.Escape);
            Thread.Sleep(500);

            return success;
        }

        private static (bool, bool, bool) ShoppingNeeded(GameData gameData, out UnitItem[] itemsToSell, out List<PotionType> potionsToBuy)
        {
            var sellItems = true;
            var itemsInInventory = Inventory.GetAllItemsInPlayerInventory(gameData.AllItems);
            itemsToSell = Inventory.GetAllItemsToSell(itemsInInventory);
            if (itemsToSell.Length == 0) sellItems = false;

            //TODO sometimes buys a town portal even though the tome is still full
            var buyPortal = true;
            var tomeItem = itemsInInventory.FirstOrDefault(x => x.Item == Item.TomeOfTownPortal);
            if(tomeItem != null)
            {
                tomeItem.Stats.TryGetValue(Stats.Stat.Quantity, out var quantity);
                if(quantity > 10) buyPortal = false;
            }

            potionsToBuy = GetMissingPotions(gameData, new PotionType[] { PotionType.ManaPotion, PotionType.HealingPotion });
            var buyPotions = potionsToBuy.Count>0;

            return (sellItems, buyPotions, buyPortal);
        }

        private static List<PotionType> GetMissingPotions(GameData gameData, PotionType[] potionTypes)
        {
            var beltConfig = BotConfig.BeltConfig;
            var missingPotions = new List<PotionType>();
            for (var i = 0; i < 4; i++)
            {
                var items = gameData.PlayerUnit.BeltItems[i].Where(x => x != null).ToArray();
                if (items.Length < gameData.PlayerUnit.BeltSize && potionTypes.Contains(beltConfig[i]))
                {
                    for (var j = items.Length; j < gameData.PlayerUnit.BeltSize; j++)
                    {
                        missingPotions.Add(beltConfig[i]);
                    }
                }
            }
            return missingPotions;
        }

        private static bool DoStoreItemsTask()
        {
            var gameData = Core.GetGameData();
            var areaData = Core.GetAreaData();
            var success = false;

            if (gameData == null || areaData == null) return false;
            if (!areaData.Objects.ContainsKey(GameObject.Bank)) return false;

            var itemsInInventory = Inventory.GetAllItemsInPlayerInventory(gameData.AllItems);
            var itemsToKeep = Inventory.GetAllItemsToKeep(itemsInInventory);

            var rejuvsMissing = GetMissingPotions(gameData, new PotionType[] { PotionType.RejuvenationPotion }).Count();

            if (itemsToKeep.Length == 0 && rejuvsMissing == 0) return true; //TODO also check for rejuvs in stash and return if there are none

            var chest = areaData.Objects[GameObject.Bank][0];
            Movement.MoveToPoint(chest);

            Movement.Interact(chest, UnitType.Object);

            Thread.Sleep(500);

            gameData = Core.GetGameData();

            if (!gameData.MenuOpen.Stash) return false;

            if (itemsToKeep.Length != 0)
            {
                foreach (var tab in BotConfig.StashTabsForStoring)
                {
                    var screenCoord = Inventory.GetStashTabScreenPos(gameData, tab);
                    Input.LeftMouseClick(screenCoord);
                    Thread.Sleep(500);

                    foreach (var item in itemsToKeep)
                    {
                        Input.KeyDown(Keys.ControlKey);
                        Thread.Sleep(250);
                        Input.LeftMouseClick(Inventory.GetItemScreenPos(gameData, item));
                        Thread.Sleep(250);
                        Input.KeyUp(Keys.ControlKey);
                        Thread.Sleep(1000);
                    }

                    gameData = Core.GetGameData();

                    itemsInInventory = Inventory.GetAllItemsInPlayerInventory(gameData.AllItems);
                    itemsToKeep = Inventory.GetAllItemsToKeep(itemsInInventory);
                    if(itemsToKeep.Length == 0)
                    {
                        success = true;
                        break;
                    }
                }
            }

            if(rejuvsMissing != 0)
            {
                // find all rejuvenation potions that are currently stored in a stash tab that is available for refilling rejuv potions
                var rejuvsInStash = gameData.AllItems.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.Item == Item.FullRejuvenationPotion && Inventory.IsStashtabAvailForRefillRejuvs(x.StashTab)).ToList();
                var currentStashTab = StashTab.None;

                if(rejuvsInStash != null && rejuvsInStash.Count() > 0)
                {
                    for(var i = 0; i<rejuvsMissing && i < rejuvsInStash.Count(); i++)
                    {
                        var item = rejuvsInStash.ElementAt(i);

                        if (item.StashTab != currentStashTab)
                        {
                            var screenCoord = Inventory.GetStashTabScreenPos(gameData, item.StashTab);
                            Input.LeftMouseClick(screenCoord);
                            Thread.Sleep(500);
                            currentStashTab = item.StashTab;
                        }

                        Input.KeyDown(Keys.LShiftKey);
                        Thread.Sleep(250);
                        Input.LeftMouseClick(Inventory.GetItemScreenPos(gameData, item));
                        Thread.Sleep(250);
                        Input.KeyUp(Keys.LShiftKey);
                        Thread.Sleep(500);
                    }
                }
            }

            Input.KeyPress(Keys.Escape);
            Thread.Sleep(500);

            return success;
        }

        private static bool DoReviveMercTask(TownConfig townConfig)
        {
            var gameData = Core.GetGameData();

            var merc = gameData.Mercs.FirstOrDefault(m => !m.IsCorpse && m.IsPlayerOwned && m.HealthPercentage > 0);

            // if we have found a valid and living merc, return true
            if (merc != null) return true;

            var success = false;
            var successNpc = Npc.NpcNotApplicable;
            foreach (var npc in townConfig.mercNpcs)
            {
                success = MoveAndInteractWithTownNpc(npc, false);
                if (success)
                {
                    successNpc = npc;
                    break;
                }
            }

            if (!success) return false;

            var dialogueOption = 1;
            if(successNpc == Npc.Tyrael2) dialogueOption = 2;

            SelectNpcDialogueOption(dialogueOption);

            Thread.Sleep(500);
            Input.KeyPress(Keys.Escape);
            Thread.Sleep(500);

            return true;
        }

        private static void DoPickUpCorpseTask()
        {
            var gameData = Core.GetGameData();

            foreach (var corpse in gameData.Corpses)
            {
                if(corpse.Name == gameData.PlayerUnit.Name)
                {
                    BotStats.Deaths++;
                    if(Pathing.CalculateDistance(corpse.Position, gameData.PlayerPosition) >= 10)
                    {
                        Movement.MoveToPoint(corpse.Position);
                        Thread.Sleep(500);
                    }
                    Movement.Interact(corpse.Position, UnitType.Player);
                    Thread.Sleep(500);
                }
            }
        }

        private static bool DoHealTask(TownConfig townConfig)
        {
            if (!HealNeeded()) return true;

            var success = MoveAndInteractWithTownNpc(townConfig.healNpc, false);

            if (!success) return false;

            Thread.Sleep(500);
            Input.KeyPress(Keys.Escape);
            Thread.Sleep(500);

            return success;
        }

        public static bool HealNeeded()
        {
            //TODO also check merc health
            UnitPlayer currPlayer = GameMemory.GetCurrentPlayerUnit();
            return currPlayer != null && currPlayer.LifePercentage != 100;
        }

        private static bool DoRepairTask(TownConfig townConfig)
        {
            if (!RepairNeeded()) return true;

            var success = MoveAndInteractWithTownNpc(townConfig.repairNpc);

            if (!success) return false;

            Thread.Sleep(500);
            var rect = Common.GetGameBounds();
            Input.LeftMouseClick(rect.Right * 0.3f, rect.Bottom * 0.72f);
            Thread.Sleep(1000);

            Input.KeyPress(Keys.Escape);
            Thread.Sleep(500);

            return success;
        }



        private static bool RepairNeeded()
        {
            var gameData = Core.GetGameData();

            var euippedItems = Inventory.GetAllItemsEquipped(gameData.AllItems);

            var damagedItems = Inventory.GetAllItemsDamaged(euippedItems);

            return damagedItems.Length > 0;
        }

        private static bool DoIdentifyTask(TownConfig townConfig)
        {
            if (!IdentifyNeeded()) return true;

            var success = MoveAndInteractWithTownNpc(townConfig.identifyNpc, false);

            if (!success) return false;

            SelectNpcDialogueOption(1);

            Thread.Sleep(500);
            Input.KeyPress(Keys.Escape);
            Thread.Sleep(500);

            return success;
        }

        private static bool IdentifyNeeded()
        {
            var gameData = Core.GetGameData();
            if (gameData == null) return false;

            var itemsInInventory = Inventory.GetAllItemsInPlayerInventory(gameData.AllItems);

            var unidentifiedItems = Inventory.GetAllUnidentifiedItems(itemsInInventory);

            return unidentifiedItems.Length > 0;
        }

        private static void SelectNpcDialogueOption(int option)
        {
            Input.SetCursorPos(new Point(0, 0));
            Thread.Sleep(200);

            for (var i = 0; i <= option; i++)
            {
                Input.KeyPress(Keys.Down);
                Thread.Sleep(150);
            }

            Input.KeyPress(Keys.Enter);
            Thread.Sleep(150);
        }


        private static bool MoveAndInteractWithTownNpc(Npc npc, bool tryOpenShop = true, int dialogueOption = 1)
        {
            _log.Info("Trying to move to and interact with " + npc);

            var tryCount = 0;
            GameData gameData;

            bool success;
            do
            {
                tryCount++;

                success = Movement.MoveToNpc(npc);
                if (!success)
                {
                    _log.Info("MoveToNpc failed");
                    continue;
                };


                gameData = Core.GetGameData();
                var monster = gameData.Monsters.FirstOrDefault(m => m.Npc == npc);
                if(monster == null)
                {
                    _log.Info("Could not find the monster unit");
                }

                try
                {
                    Movement.Interact(monster.Position, UnitType.Monster);
                }
                catch(MovementException)
                {
                    _log.Info("Interact failed");
                    continue;
                };


                Thread.Sleep(200);

                gameData = Core.GetGameData();

                if (gameData.LastNpcInteracted != npc || !gameData.MenuOpen.NpcInteract)
                {
                    _log.Info("NpcInteract menu not open");
                    success = false;
                    continue;
                };

                
                _log.Info("Interacted with Town NPC");

                if (tryOpenShop)
                {
                    SelectNpcDialogueOption(dialogueOption);
                    Thread.Sleep(200);
                    
                    gameData = Core.GetGameData();

                    success = gameData.MenuOpen.NpcShop;
                }

            } while (success == false && tryCount < 4);

            return success;
        }
    }

    [Serializable]
    internal class TownException : Exception
    {
        public TownException()
        {
        }

        public TownException(string message) : base(message)
        {
        }

        public TownException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TownException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
