using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ queries

namespace CustomNamespace
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "TSTSSES_FrigateCore")]
    public class SimpleGridFiller : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private const string FrigateReactorSubtype = "FrigateCore_Reactor"; // Subtype of the reactor

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;

            // Place FrigateReactor blocks forward and backward of the block
            AddFrigateReactor(new Vector3I(0, 0, 1)); // Forward
            AddFrigateReactor(new Vector3I(0, 0, -1)); // Backward

            // Periodic check to ensure the assembly is intact
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private void AddFrigateReactor(Vector3I direction)
        {
            var grid = block.CubeGrid;
            var position = block.Position + direction;

            var blockBuilder = new MyObjectBuilder_CubeBlock
            {
                SubtypeName = FrigateReactorSubtype,
                Min = position,
                BlockOrientation = new MyBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up),
                ColorMaskHSV = new SerializableVector3(0, -1, 0),
                Owner = block.OwnerId,
                EntityId = 0,
                ShareMode = MyOwnershipShareModeEnum.None
            };

            IMySlimBlock newBlock = grid.AddBlock(blockBuilder, false);
            if (newBlock == null)
            {
                MyAPIGateway.Utilities.ShowNotification($"Failed to add FrigateReactor at {position}", 1000);
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification($"FrigateReactor added at {position}", 1000);
            }
        }

        public override void UpdateAfterSimulation100()
        {
            // Check if all required blocks are present
            if (!IsAssemblyIntact())
            {
                MyAPIGateway.Utilities.ShowNotification("Part of the Frigate assembly is missing!", 5000, MyFontEnum.Red);
            }
        }

        private bool IsAssemblyIntact()
        {
            var grid = block.CubeGrid;
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeId == FrigateReactorSubtype);

            // Check if the required number of FrigateReactor blocks are present
            return blocks.Count == 2; // Adjust the number based on how many reactors are expected
        }

        public override void Close()
        {
            base.Close();
            // Additional cleanup if needed
        }
    }
}
