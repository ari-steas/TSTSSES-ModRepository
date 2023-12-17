using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using EmptyKeys.UserInterface.Generated.ContractsBlockView_Gamepad_Bindings;
using CustomNamespace;

namespace TSTSSESCoresAddon.Data.Scripts.ScriptsAddon.customscripts
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, new string[] {
        "CoreKit_1",
    })]
    public class CoreKitReplacer : MyGameLogicComponent
    {
        private IMyCubeBlock block;

        // TODO: Only trigger block replacement when welded above functional.
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            block = (IMyCubeBlock)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (block?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }


        public override void UpdateAfterSimulation()
        {
            if (!block.IsFunctional)
                return;

            //MyAPIGateway.Utilities.ShowNotification("KILL MYSELF NOW!");
            var grid = block.CubeGrid;

            grid.RemoveBlock(block.SlimBlock);

            SimpleGridFiller.AddBlock<MyObjectBuilder_Beacon>(block, Vector3I.Zero, "TSTSSES_FrigateCore");
        }
    }
}
