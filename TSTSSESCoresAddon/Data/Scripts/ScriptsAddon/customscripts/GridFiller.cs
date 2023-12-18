using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;
using IMyEntity = VRage.Game.ModAPI.Ingame.IMyEntity;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMySlimBlock = VRage.Game.ModAPI.IMySlimBlock;

namespace CustomNamespace
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "TSTSSES_FrigateCore")]
    public class SimpleGridFiller : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private const string FrigateReactorSubtype = "FrigateCore_Reactor";
        private const string FrigateCargoSubtype = "FrigateCore_Cargo";
        private const int MaxDistance = 1;
        private const int MaxFrigateReactors = 1;
        private const int MaxFrigateCargos = 1;
        private const double NotificationRadius = 50.0; // Radius for player notification

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;

            // Periodic check to ensure the assembly is intact
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            // Check if all required blocks are present and adjacent
            if (!IsAssemblyIntact())
            {
                NotifyPlayersInRange("Part of the Frigate assembly is missing or not adjacent to the FrigateCore!", block.GetPosition(), NotificationRadius, MyFontEnum.Red);
            }
        }

        public void NotifyPlayersInRange(string text, Vector3D position, double radius, string font)
        {
            var bound = new BoundingSphereD(position, radius);
            List<VRage.ModAPI.IMyEntity> nearEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref bound);

            foreach (var entity in nearEntities)
            {
                var character = entity as VRage.Game.ModAPI.IMyCharacter;
                if (character != null && character.IsPlayer && bound.Contains(character.GetPosition()) != ContainmentType.Disjoint)
                {
                    var notification = MyAPIGateway.Utilities.CreateNotification(text, 1500, font);
                    notification.Show();
                }
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
                //MyAPIGateway.Utilities.ShowNotification($"Too many FrigateReactor blocks detected! Maximum allowed: {MaxFrigateReactors}", 5000, MyFontEnum.Red);
                return false;
            }

            if (cargoCount > MaxFrigateCargos)
            {
               // MyAPIGateway.Utilities.ShowNotification($"Too many FrigateCargo blocks detected! Maximum allowed: {MaxFrigateCargos}", 5000, MyFontEnum.Red);
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
