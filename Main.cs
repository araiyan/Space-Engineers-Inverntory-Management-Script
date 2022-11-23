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
    partial class Program : MyGridProgram
    {
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
        List<IMyRefinery> refineries = new List<IMyRefinery>();
        List<IMyCockpit> cockpits = new List<IMyCockpit>();
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        List<IMyShipDrill> drills = new List<IMyShipDrill>();
        List<IMyTerminalBlock> sourceCargo = new List<IMyTerminalBlock>();

        Dictionary<string, int> rawResources = new Dictionary<string, int>();
        IMyTextPanel display;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GridTerminalSystem.GetBlocksOfType(cargoContainers);
            GridTerminalSystem.GetBlocksOfType(refineries);
            GridTerminalSystem.GetBlocksOfType(cockpits);
            GridTerminalSystem.GetBlocksOfType(connectors);
            GridTerminalSystem.GetBlocksOfType(drills);

            sourceCargo.AddRange(cargoContainers);
            sourceCargo.AddRange(cockpits);
            sourceCargo.AddRange(connectors);
            sourceCargo.AddRange(drills);

            display = GridTerminalSystem.GetBlockWithName("Resource Monitor") as IMyTextPanel;
            display.FontSize = 2.5f;
            display.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        public void Main(string argument, UpdateType updateSource)
        {   
            // Looks through cargo containers to find Ores
            fillDictWithRawResources(sourceCargo, ref rawResources);
            fillDictWithRawResources(refineries.ConvertAll(x=>(IMyTerminalBlock)x), ref rawResources);

            //Move the raw resrouces to Refineries
            foreach (var cargo in sourceCargo)
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
                        foreach(IMyRefinery refinery in refineries)
                        {
                            IMyInventory refineryInventory = refinery.GetInventory();
                            refineryInventory.TransferItemFrom(cargoInventory, item.Value, (itemAmount / refineries.Count));
                        }
                    }

                }
            }

            //Evenly Distribute raw resources in refineries
            for(int i = 0; i < refineries.Count; i++)
            {
                IMyInventory sourceInventory = refineries[i].GetInventory();
                IMyInventory destinationInventory = refineries[(i + 1) % refineries.Count].GetInventory();

                for (int j = 0; j < sourceInventory.ItemCount; j++)
                {
                    MyInventoryItem item = sourceInventory.GetItemAt(j).Value;

                    int amountToObtain = (rawResources[item.Type.SubtypeId] / refineries.Count) + 1;
                    int amountToMove = item.Amount.ToIntSafe() - amountToObtain;

                    if (amountToMove > 0)
                        sourceInventory.TransferItemTo(destinationInventory, item, amountToMove);
                }
            }


            // Display all Raw Resources
            display.WriteText("", false);
            foreach (string name in rawResources.Keys)
                display.WriteText(name + ": " + rawResources[name] + "\n", true);

            // Resets all the value of Ore
            rawResources.Keys.ToList().ForEach(x => rawResources[x] = 0);

            foreach(IMyTerminalBlock cargo in sourceCargo)
            {
                IMyInventory cargoInventory = cargo.GetInventory();
                int itemCount = cargoInventory.ItemCount;

                Echo("Name: " + cargo.CustomName);
                Echo("Item Count: " + itemCount);
            }

        }

    }
}
