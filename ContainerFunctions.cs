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
        private void fillDictWithRawResources(List<IMyTerminalBlock> cargoContainers, ref Dictionary<string, int> rawResources)
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
                    if (itemSubtype.Contains("Ore"))
                    {
                        if (rawResources.ContainsKey(itemName))
                            rawResources[itemName] += itemAmount;
                        else
                        {
                            rawResources.Add(itemName, itemAmount);
                            rawResources = rawResources.OrderBy(x => x.Key).ToDictionary(x =>x.Key, y=>y.Value);
                        }
                    }

                }
            }
        }
    }
}
