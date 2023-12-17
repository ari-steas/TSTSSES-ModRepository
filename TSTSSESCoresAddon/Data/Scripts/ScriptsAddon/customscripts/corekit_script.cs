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

namespace TSTSSESCoresAddon.Data.Scripts.ScriptsAddon.customscripts
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, new string[] {
        "CoreKit_1",
    })]
    public class corekit_script : MyGameLogicComponent
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

            MyAPIGateway.Utilities.ShowNotification("KILL MYSELF NOW!");
            var grid = block.CubeGrid;

            grid.RemoveBlock(block.SlimBlock);

            AddBlock(block, "TSTSSES_FrigateCore", block.Position);
        }

        private static void AddBlock(IMyCubeBlock block, string subtypeName, Vector3I position)
        {
            var nextBlockBuilder = new MyObjectBuilder_Beacon
            {
                SubtypeName = subtypeName,
                Min = position,
                BlockOrientation = block.Orientation,
                ColorMaskHSV = new SerializableVector3(0, -1, 0),
                Owner = block.OwnerId,
                EntityId = 0,
                ShareMode = MyOwnershipShareModeEnum.None
            };

            IMySlimBlock newBlock = block.CubeGrid.AddBlock(nextBlockBuilder, false);

            if (newBlock == null)
            {
                MyAPIGateway.Utilities.ShowNotification($"Failed to add {subtypeName}", 1000);
                return;
            }
            MyAPIGateway.Utilities.ShowNotification($"{subtypeName} added at {position}", 1000);
        }
    }
}
