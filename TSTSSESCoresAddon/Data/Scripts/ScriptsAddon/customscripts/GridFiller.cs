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

namespace CustomNamespace
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "TSTSSES_FrigateCore")]
    public class SimpleGridFiller : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private List<long> addedBlockEntityIds; // List to track IDs of added blocks

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;
            addedBlockEntityIds = new List<long>();

            // Register event for block removal
            block.CubeGrid.OnBlockRemoved += OnBlockRemoved;

            // Place armor blocks forward and backward of the block
            AddArmorBlock(new Vector3I(0, 0, 1)); // Forward
            AddArmorBlock(new Vector3I(0, 0, -1)); // Backward
        }

        private void AddArmorBlock(Vector3I direction)
        {
            var grid = block.CubeGrid;
            var position = block.Position + direction;

            var blockBuilder = new MyObjectBuilder_CubeBlock
            {
                SubtypeName = "LargeBlockSmallGenerator", // Adjust subtype name for desired block type
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
                MyAPIGateway.Utilities.ShowNotification($"Failed to add armor block at {position}", 1000);
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification($"Armor block added at {position}", 1000);
                addedBlockEntityIds.Add(newBlock.FatBlock.EntityId); // Track the ID of the added block
            }
        }

        private void OnBlockRemoved(IMySlimBlock block)
        {
            if (block.FatBlock != null && addedBlockEntityIds.Contains(block.FatBlock.EntityId))
            {
                // Notification when a part of the assembly is removed
                MyAPIGateway.Utilities.ShowNotification($"Part of the assembly has been removed!", 5000, MyFontEnum.Red);
            }
        }

        public override void Close()
        {
            // Unregister the event when the component is closed
            if (block != null && block.CubeGrid != null)
            {
                block.CubeGrid.OnBlockRemoved -= OnBlockRemoved;
            }

            base.Close();
        }
    }
}
