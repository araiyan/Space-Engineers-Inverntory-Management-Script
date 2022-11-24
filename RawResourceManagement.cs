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
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<IMyCockpit> cockpits = new List<IMyCockpit>();
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        List<IMyShipDrill> drills = new List<IMyShipDrill>();

        List<IMyTerminalBlock> sourceCargo = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> componentCargo = new List<IMyTerminalBlock>();

        Dictionary<string, int> rawResourcesDict = new Dictionary<string, int>();
        Dictionary<string, int> componentsDict = new Dictionary<string, int>();
        Dictionary<string, int> componentsRequestDict = new Dictionary<string, int>();
        Dictionary<string, string> subIdToBlueprintsDict = new Dictionary<string, string>();

        IMyTextPanel resouceMonitor, componentMonitor;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GridTerminalSystem.GetBlocksOfType(cargoContainers);
            GridTerminalSystem.GetBlocksOfType(refineries);
            GridTerminalSystem.GetBlocksOfType(cockpits);
            GridTerminalSystem.GetBlocksOfType(connectors);
            GridTerminalSystem.GetBlocksOfType(drills);
            GridTerminalSystem.GetBlocksOfType(assemblers);

            

            sourceCargo.AddRange(cargoContainers);
            sourceCargo.AddRange(cockpits);
            sourceCargo.AddRange(connectors);
            sourceCargo.AddRange(drills);

            componentCargo.AddRange(cargoContainers);
            componentCargo.AddRange(connectors);

            resouceMonitor = GridTerminalSystem.GetBlockWithName("Resource Monitor") as IMyTextPanel;
            resouceMonitor.FontSize = 2.5f;
            resouceMonitor.ContentType = ContentType.TEXT_AND_IMAGE;

            componentMonitor = GridTerminalSystem.GetBlockWithName("Component Monitor") as IMyTextPanel;
            componentMonitor.FontSize = 1;
            componentMonitor.ContentType = ContentType.TEXT_AND_IMAGE;

            subIdToBlueprintsDict = new Dictionary<string, string>()
            {
                { "Computer",  "MyObjectBuilder_BlueprintDefinition/ComputerComponent"},
                { "Construction", "MyObjectBuilder_BlueprintDefinition/ConstructionComponent" },
                { "Display", "MyObjectBuilder_BlueprintDefinition/Display" },
                { "Girder", "MyObjectBuilder_BlueprintDefinition/GirderComponent" },
                { "InteriorPlate", "MyObjectBuilder_BlueprintDefinition/InteriorPlate" },
                { "LargeTube", "MyObjectBuilder_BlueprintDefinition/LargeTube" },
                { "MetalGrid", "MyObjectBuilder_BlueprintDefinition/MetalGrid" },
                { "Motor", "MyObjectBuilder_BlueprintDefinition/MotorComponent"  },
                { "PowerCell", "MyObjectBuilder_BlueprintDefinition/PowerCell" },
                { "SmallTube", "MyObjectBuilder_BlueprintDefinition/SmallTube" },
                { "SteelPlate", "MyObjectBuilder_BlueprintDefinition/SteelPlate" }
            };

            // Change the value of this dictionary to change the
            // amount of reserve components you would like to have
            // Just make sure to add the blueprint of your
            // component to the dictionary above
            componentsRequestDict = new Dictionary<string, int>()
            {
                { "Computer", 150 },
                { "Construction", 200 },
                { "Display", 20 },
                { "Girder", 50 },
                { "InteriorPlate", 200 },
                { "LargeTube", 10 },
                { "MetalGrid", 10 },
                { "Motor", 50  },
                { "PowerCell", 100 },
                { "SmallTube", 50 },
                { "SteelPlate", 400 }
            };
        }

        public void Main(string argument, UpdateType updateSource)
        {   
            // Looks through cargo containers to find Ores
            fillDictWithResources(sourceCargo, ref rawResourcesDict, "Ore");
            fillDictWithResources(refineries.ConvertAll(x=>(IMyTerminalBlock)x), ref rawResourcesDict, "Ore");

            // Looks through cargo containers to find all components
            fillDictWithResources(componentCargo, ref componentsDict, "Component");
            fillDictWithResourcesTwoInventory(assemblers.ConvertAll(x=>(IMyTerminalBlock)x), ref componentsDict, "Component", 1);

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

                    int amountToObtain = (rawResourcesDict[item.Type.SubtypeId] / refineries.Count) + 1;
                    int amountToMove = item.Amount.ToIntSafe() - amountToObtain;

                    if (amountToMove > 0)
                        sourceInventory.TransferItemTo(destinationInventory, item, amountToMove);
                }
            }

            // Adds a cargo reserve for certain components
            addMissingComponentToAssemblerQueue(componentsDict, componentsRequestDict, assemblers);

            // Display all Raw Resources
            resouceMonitor.WriteText("", false);
            foreach (string name in rawResourcesDict.Keys)
                resouceMonitor.WriteText(name + ": " + rawResourcesDict[name] + "\n", true);

            // Display all Components Resources
            componentMonitor.WriteText("", false);
            foreach (KeyValuePair<string, int> component in componentsDict)
                componentMonitor.WriteText(component.Key + ": " + component.Value + "\n", true);

            // Resets all the value of dicts
            rawResourcesDict.Keys.ToList().ForEach(x => rawResourcesDict[x] = 0);
            componentsDict.Keys.ToList().ForEach(x => componentsDict[x] = 0);

        }

    }
}
