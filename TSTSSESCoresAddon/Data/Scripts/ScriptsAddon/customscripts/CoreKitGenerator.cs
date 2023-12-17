using Sandbox.Definitions;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace TSTSSESCoresAddon.Data.Scripts.ScriptsAddon.customscripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class CoreKitGenerator : MySessionComponentBase
    {
        //public override void LoadData()
        //{
        //    // Define the block's physical properties
        //    MyCubeBlockDefinition blockDefinition = new MyCubeBlockDefinition
        //    {
        //        Id = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "YourMod_CustomBlock"),
        //        DisplayNameString = "Custom Block", // Display name
        //        //Icon = "Path/To/Your/Icon.png", // Path to your block's icon
        //        CubeSize = MyCubeSize.Large, // Set to MyCubeSize.Small for small grid block
        //        BlockTopology = MyBlockTopology.TriangleMesh,
        //        Size = new Vector3I(1f, 1f, 1f), // Size of the block
        //        GuiVisible = true,
        //    };
        //
        //    MyCubeBlockDefinition existingDefinition = null;
        //    // Add the block to the game
        //    if (!MyDefinitionManager.Static.TryGetCubeBlockDefinition(blockDefinition.Id, out existingDefinition))
        //    {
        //        MyAPIGateway.Utilities.ShowNotification("" + MyDefinitionManager.Static.Definitions.Definitions.Count);
        //        MyDefinitionManager.Static.Definitions.AddDefinition(blockDefinition);
        //        MyAPIGateway.Utilities.ShowNotification("DID THE THING " + blockDefinition.DisplayNameText);
        //        MyAPIGateway.Utilities.ShowNotification("" + MyDefinitionManager.Static.Definitions.Definitions.Count);
        //    }
        //    else
        //        MyAPIGateway.Utilities.ShowNotification("DID NOT THE THING");
        //}
    }
}
