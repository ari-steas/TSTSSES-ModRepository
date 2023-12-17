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
        private const string FrigateReactorSubtype = "FrigateCore_Reactor";
        private const int MaxDistance = 1;
        private const int RequiredReactorCount = 2; // Specify the required number of reactors
        private bool hasErrors = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;

            AddFrigateReactor(new Vector3I(0, 0, 1));
            AddFrigateReactor(new Vector3I(0, 0, -1));

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
            bool isAssemblyIntact = IsAssemblyIntact();

            if (isAssemblyIntact)
            {
                hasErrors = false;
            }
            else
            {
                hasErrors = !hasErrors;
            }
        }

        private bool IsAssemblyIntact()
        {
            var grid = block.CubeGrid;
            var reactorPosition = block.Position;

            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeId == FrigateReactorSubtype);

            // Check if the required number of FrigateReactor blocks are present
            int reactorCount = blocks.Count(b => Vector3I.DistanceManhattan(b.Position, reactorPosition) <= MaxDistance);

            // Adjust the number based on how many reactors are expected
            if (reactorCount == RequiredReactorCount)
            {
                return true;
            }
            else if (reactorCount > RequiredReactorCount)
            {
                // Handle the case where there are more than the required number of reactors
                MyAPIGateway.Utilities.ShowNotification("Too many FrigateReactors on the grid!", 5000, MyFontEnum.Red);
                return false;
            }
            else
            {
                return false;
            }
        }

        public override void Close()
        {
            base.Close();
        }
    }
}
