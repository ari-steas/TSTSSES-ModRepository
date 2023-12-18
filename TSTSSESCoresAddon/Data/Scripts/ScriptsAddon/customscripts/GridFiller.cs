using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using System.Collections.Generic;
using System.Linq;

namespace CustomNamespace
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "TSTSSES_FrigateCore")]
    public class SimpleGridFiller : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private const string FrigateReactorSubtype = "FrigateCore_Reactor"; // Subtype of the reactor
        private const string FrigateCargoSubtype = "FrigateCore_Cargo"; // Subtype of the cargo container
        private const int MaxDistance = 1; // Maximum distance for blocks to be considered adjacent
        private const int MaxFrigateReactors = 1; // Maximum allowed FrigateReactor blocks
        private const int MaxFrigateCargos = 1; // Maximum allowed FrigateCargo blocks

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;

            // Removed automatic block placement functionality

            // Periodic check to ensure the assembly is intact
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            // Check if all required blocks are present and adjacent
            if (!IsAssemblyIntact())
            {
                MyAPIGateway.Utilities.ShowNotification("Part of the Frigate assembly is missing or not adjacent to the FrigateCore!", 5000, MyFontEnum.Red);
            }
        }

        private bool IsAssemblyIntact()
        {
            var grid = block.CubeGrid;
            var reactorPosition = block.Position;

            var reactorBlocks = new List<IMySlimBlock>();
            var cargoBlocks = new List<IMySlimBlock>();

            // Use GetBlocks to get all reactor and cargo blocks
            grid.GetBlocks(reactorBlocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeId == FrigateReactorSubtype);
            grid.GetBlocks(cargoBlocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeId == FrigateCargoSubtype);

            // Check if the required number of FrigateReactor blocks and FrigateCargo blocks are present
            int reactorCount = reactorBlocks.Count(b => Vector3I.DistanceManhattan(b.Position, reactorPosition) <= MaxDistance);
            int cargoCount = cargoBlocks.Count(b => Vector3I.DistanceManhattan(b.Position, reactorPosition) <= MaxDistance);

            if (reactorCount > MaxFrigateReactors)
            {
                MyAPIGateway.Utilities.ShowNotification($"Too many FrigateReactor blocks detected! Maximum allowed: {MaxFrigateReactors}", 5000, MyFontEnum.Red);
                return false;
            }

            if (cargoCount > MaxFrigateCargos)
            {
                MyAPIGateway.Utilities.ShowNotification($"Too many FrigateCargo blocks detected! Maximum allowed: {MaxFrigateCargos}", 5000, MyFontEnum.Red);
                return false;
            }

            return reactorCount >= 1 && cargoCount >= 1;
        }

        public override void Close()
        {
            base.Close();
            // Additional cleanup if needed
        }
    }
}
