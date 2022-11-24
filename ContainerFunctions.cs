using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        private void fillDictWithResources(List<IMyTerminalBlock> cargoContainers,
            ref Dictionary<string, int> resources, string resourceName)
        {
            foreach (var cargo in cargoContainers)
            {
                IMyInventory cargoInventory = cargo.GetInventory();
                int itemCount = cargoInventory.ItemCount;

                for (int i = 0; i < itemCount; i++)
                {
                    MyInventoryItem? item = cargoInventory.GetItemAt(i);

                    string itemName = item.Value.Type.SubtypeId;
                    string itemSubtype = item.Value.Type.TypeId;
                    int itemAmount = item.Value.Amount.ToIntSafe();

                    // Adds up the total amount of a certain Ore
                    if (itemSubtype.Contains(resourceName))
                    {
                        if (resources.ContainsKey(itemName))
                            resources[itemName] += itemAmount;
                        else
                        {
                            resources.Add(itemName, itemAmount);
                            resources = resources.OrderBy(x => x.Key).ToDictionary(x =>x.Key, y=>y.Value);
                        }
                    }

                }
            }
        }

        private void fillDictWithResourcesTwoInventory(List<IMyTerminalBlock> cargoContainers,
            ref Dictionary<string, int> resources, string resourceName, int inventoryIndex)
        {
            foreach (var cargo in cargoContainers)
            {
                IMyInventory cargoInventory = cargo.GetInventory(inventoryIndex);
                int itemCount = cargoInventory.ItemCount;

                for (int i = 0; i < itemCount; i++)
                {
                    MyInventoryItem? item = cargoInventory.GetItemAt(i);

                    string itemName = item.Value.Type.SubtypeId;
                    string itemSubtype = item.Value.Type.TypeId;
                    int itemAmount = item.Value.Amount.ToIntSafe();

                    // Adds up the total amount of a certain Ore
                    if (itemSubtype.Contains(resourceName))
                    {
                        if (resources.ContainsKey(itemName))
                            resources[itemName] += itemAmount;
                        else
                        {
                            resources.Add(itemName, itemAmount);
                            resources = resources.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
                        }
                    }

                }
            }
        }

        private void addMissingComponentToAssemblerQueue(Dictionary<string, int> inventoryDict,
            Dictionary<string, int> requestDict, List<IMyAssembler> assemblers)
        {
            Dictionary<string, int> itemsToAddToQueue = new Dictionary<string, int>();
            IMyAssembler removeFromList = null;

            // Get the amount of items to add to the queue
            foreach (KeyValuePair<string, int> request in requestDict)
            {
                int requestAmount = request.Value - inventoryDict[request.Key];
                if (requestAmount > 0)
                    itemsToAddToQueue.Add(request.Key, requestAmount / assemblers.Count);
            }
        
            foreach(KeyValuePair<string, int> request in itemsToAddToQueue)
            {
                Echo($"Requesting fromeach: {request.Key} = {request.Value}");
            }

            // If assembler not producing the item - add it to the queue
            foreach(IMyAssembler assembler in assemblers)
            {
                if (assembler.CustomName.Contains("Survival"))
                    removeFromList = assembler;

                List<MyProductionItem> assemblerProductionQueue = new List<MyProductionItem>();
                
                assembler.GetQueue(assemblerProductionQueue);

                foreach(KeyValuePair<string, int> requestQueueItem in itemsToAddToQueue)
                {
                    float amountToAddToQueue;
                    int amountAlreadyInQueue = 0;

                    // If already in production queue then add a different amount to the queue
                    foreach(MyProductionItem productionQueueItem in assemblerProductionQueue)
                    {
                        if (productionQueueItem.BlueprintId.ToString().Contains(requestQueueItem.Key))
                            amountAlreadyInQueue += productionQueueItem.Amount.ToIntSafe();
                    }
                    amountToAddToQueue = requestQueueItem.Value - amountAlreadyInQueue;

                    if (amountToAddToQueue > 0)
                    {
                        MyDefinitionId definitionId = MyDefinitionId.Parse(subIdToBlueprintsDict[requestQueueItem.Key]);
                        assembler.AddQueueItem(definitionId, amountToAddToQueue);
                    }
                        
                }
                
               
            }

            // Removes survival Kits
            if(removeFromList != null)
                assemblers.Remove(removeFromList);
        }

    }
}
