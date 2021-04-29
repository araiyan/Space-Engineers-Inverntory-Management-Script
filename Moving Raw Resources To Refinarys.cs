using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using Sandbox.Game.Gui;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        List<IMyTerminalBlock> all_cargo_container = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> all_connectors = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> all_cockpits = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> all_refineries = new List<IMyTerminalBlock>();
        

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.

            Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            //Defines all the sources where raw resources could be
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(all_cargo_container, x => x.CustomName.Contains("Cargo"));
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(all_connectors, x => x.CustomName.Contains("Connector"));
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(all_cockpits, x => x.CustomName.Contains("Cockpit"));
            //Where the raw resources will end up at
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(all_refineries, x => x.CustomName.Contains("Refinery"));
            //Ejector for ejecting Gravel
            IMyShipConnector gravel_ejector = GridTerminalSystem.GetBlockWithName("Base 1 Ejector 1") as IMyShipConnector;

            List<string> raw_resource_names = new List<string> { "MyObjectBuilder_Ore/Cobalt", "MyObjectBuilder_Ore/Gold", "MyObjectBuilder_Ore/Stone",
                "MyObjectBuilder_Ore/Iron", "MyObjectBuilder_Ingot/Scrap", "MyObjectBuilder_Ore/Magnesium", "MyObjectBuilder_Ore/Nickel",
                "MyObjectBuilder_Ore/Platinum", "MyObjectBuilder_Ore/Scrap", "MyObjectBuilder_Ore/Silicon", "MyObjectBuilder_Ore/Silver"};

            if (all_refineries == null)
            {
                Echo("Oh no! Could not find refineries :(");
            }

            for (int i = 0; i < all_cargo_container.Count; i++)
            {
                var container = all_cargo_container[i];
                var items = new List<MyInventoryItem>();
                container.GetInventory(0).GetItems(items);

                for (int z = 0; z < items.Count; z++)
                {
                    //Searches through every item to find raw meterials
                    if (raw_resource_names.Contains(items[z].Type.ToString()))
                    {
                        float resource_amount = float.Parse(items[z].Amount.ToString());

                        Echo(items[z].Type.SubtypeId + " found in " + all_cargo_container[i].ToString());
                        Echo("Amount: " + resource_amount + "\n");

                        //Moves all the raw meterials to the refineries
                        for (int r = 0; r < all_refineries.Count; r++)
                        {
                            IMyInventory destination_refinery = all_refineries[r].GetInventory(0);
                            IMyInventory source_cargo = all_cargo_container[i].GetInventory(0);

                            source_cargo.TransferItemTo(destination_refinery, z, null, true, (int)(resource_amount / all_refineries.Count));

                        }

                    }

                }

            }

            for (int i = 0; i < all_connectors.Count; i++)
            {
                var container = all_connectors[i];
                var items = new List<MyInventoryItem>();
                container.GetInventory(0).GetItems(items);

                for (int z = 0; z < items.Count; z++)
                {
                    if (raw_resource_names.Contains(items[z].Type.ToString()))
                    {
                        float resource_amount = float.Parse(items[z].Amount.ToString());

                        Echo(items[z].Type.SubtypeId + " found in " + all_connectors[i].ToString());
                        Echo("Amount: " + resource_amount + "\n");

                        for (int r = 0; r < all_refineries.Count; r++)
                        {
                            IMyInventory destination_refinery = all_refineries[r].GetInventory(0);
                            IMyInventory source_cargo = all_connectors[i].GetInventory(0);

                            source_cargo.TransferItemTo(destination_refinery, z, null, true, (int)(resource_amount / all_refineries.Count));
                        }

                    }

                }

            }

            for (int i = 0; i < all_cockpits.Count; i++)
            {
                var container = all_cockpits[i];
                var items = new List<MyInventoryItem>();
                container.GetInventory(0).GetItems(items);

                for (int z = 0; z < items.Count; z++)
                {
                    if (raw_resource_names.Contains(items[z].Type.ToString()))
                    {
                        float resource_amount = float.Parse(items[z].Amount.ToString());

                        Echo(items[z].Type.SubtypeId + " found in " + all_cockpits[i].ToString());
                        Echo("Amount: " + resource_amount + "\n");

                        for (int r = 0; r < all_refineries.Count; r++)
                        {
                            IMyInventory destination_refinery = all_refineries[r].GetInventory(0);
                            IMyInventory source_cargo = all_cockpits[i].GetInventory(0);

                            source_cargo.TransferItemTo(destination_refinery, z, null, true, (int)(resource_amount / all_refineries.Count));
                        }

                    }

                }

            }

            for(int i = 0; i < all_refineries.Count; i++)
            {
                IMyInventory refinery_cargo = all_refineries[i].GetInventory(1);
                IMyInventory trash_pile = gravel_ejector.GetInventory(0);

                var container = all_refineries[i];
                var items = new List<MyInventoryItem>();
                container.GetInventory(1).GetItems(items);

                for(int z = 0; z < items.Count; z++)
                {
                    //Searches through every item to find raw meterials
                    if (raw_resource_names.Contains(items[z].Type.ToString()))
                    {
                        float resource_amount = float.Parse(items[z].Amount.ToString());

                        Echo(items[z].Type.SubtypeId + " found in " + all_refineries[i].ToString());
                        Echo("Amount: " + resource_amount + "\n");

                        //Moves all the raw meterials to the refineries
                        for (int r = 0; r < all_refineries.Count; r++)
                        {
                            IMyInventory destination_refinery = all_refineries[r].GetInventory(0);
                            IMyInventory source_cargo = all_refineries[i].GetInventory(0);

                            source_cargo.TransferItemTo(destination_refinery, z, null, true, (int)(resource_amount / all_refineries.Count));

                        }

                    }

                    //Removes Gravel from Refineries
                    if (items[z].Type.ToString() == "MyObjectBuilder_Ingot/Stone")
                    {
                        float gravel_amount = float.Parse(items[z].Amount.ToString());
                        if(gravel_amount > 4000)
                        {
                            Echo("Too much Gravel. \nRemoving access Gravel now...");
                            refinery_cargo.TransferItemTo(trash_pile, z, null, true, (int)(gravel_amount - 2000));
                        }
                        
                    }
                }
            }

        }
    }
}
