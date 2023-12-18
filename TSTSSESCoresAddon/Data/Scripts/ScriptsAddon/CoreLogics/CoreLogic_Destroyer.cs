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
using System.Text;

namespace TSTSSESCoresAddon.Data.Scripts.ScriptsAddon.CoreLogics
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "TSTSSES_DestroyerCore")]
    public class CoreLogic_Destroyer : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private const string DestroyerReactorSubtype = "DestroyerCore_Reactor";
        private const string DestroyerCargoSubtype = "DestroyerCore_Cargo";
        private const int MaxDistance = 2;
        private const int MaxDestroyerReactors = 1;
        private const int MaxDestroyerCargos = 1;
        private const double NotificationRadius = 50.0; // Radius for player notification

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            block = (IMyCubeBlock)Entity;

            // Periodic check to ensure the assembly is intact
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            string errorMessages = IsAssemblyIntact();
            if (!string.IsNullOrEmpty(errorMessages))
            {
                IMyTerminalBlock terminalBlock = block as IMyTerminalBlock; // Cast to IMyTerminalBlock
                string notificationText = $"[Grid: {block.CubeGrid.DisplayName}] '{terminalBlock.CustomName}' status:\n{errorMessages}";
                NotifyPlayersInRange(notificationText, block.GetPosition(), NotificationRadius, MyFontEnum.Red);
            }
        }


        public void NotifyPlayersInRange(string text, Vector3D position, double radius, string font)
        {
            var bound = new BoundingSphereD(position, radius);
            List<VRage.ModAPI.IMyEntity> nearEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref bound);

            foreach (var entity in nearEntities)
            {
                var character = entity as IMyCharacter;
                if (character != null && character.IsPlayer && bound.Contains(character.GetPosition()) != ContainmentType.Disjoint)
                {
                    var notification = MyAPIGateway.Utilities.CreateNotification(text, 1600, font);
                    notification.Show();
                }
            }
        }



        private string IsAssemblyIntact()
        {
            var grid = block.CubeGrid;
            var corePosition = block.Position;

            // List all blocks of the specific subtypes on the grid
            var allReactorBlocks = new List<IMySlimBlock>();
            var allCargoBlocks = new List<IMySlimBlock>();

            grid.GetBlocks(allReactorBlocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeId == DestroyerReactorSubtype);
            grid.GetBlocks(allCargoBlocks, b => b.FatBlock != null && b.FatBlock.BlockDefinition.SubtypeId == DestroyerCargoSubtype);

            // Filter the blocks to only those that are adjacent to the core
            var adjacentReactorBlocks = allReactorBlocks.Where(b => Vector3I.DistanceManhattan(b.Position, corePosition) <= MaxDistance).ToList();
            var adjacentCargoBlocks = allCargoBlocks.Where(b => Vector3I.DistanceManhattan(b.Position, corePosition) <= MaxDistance).ToList();

            StringBuilder errorMessage = new StringBuilder();

            // Check if the number of adjacent blocks is within the allowed range
            int reactorCount = adjacentReactorBlocks.Count;
            int cargoCount = adjacentCargoBlocks.Count;

            if (reactorCount > MaxDestroyerReactors)
            {
                errorMessage.AppendLine($"Exceeds maximum {DestroyerReactorSubtype} count (Max: {MaxDestroyerReactors}).");
            }
            else if (reactorCount < MaxDestroyerReactors)
            {
                errorMessage.AppendLine($"{DestroyerReactorSubtype} required.");
            }

            if (cargoCount > MaxDestroyerCargos)
            {
                errorMessage.AppendLine($"Exceeds maximum {DestroyerCargoSubtype} count (Max: {MaxDestroyerCargos}).");
            }
            else if (cargoCount < MaxDestroyerCargos)
            {
                errorMessage.AppendLine($"{DestroyerCargoSubtype} required.");
            }

            // Backup check for non-adjacent reactors or cargos
            if (allReactorBlocks.Count > reactorCount)
            {
                errorMessage.AppendLine($"Detected non-adjacent {DestroyerReactorSubtype} blocks.");
            }

            if (allCargoBlocks.Count > cargoCount)
            {
                errorMessage.AppendLine($"Detected non-adjacent {DestroyerCargoSubtype} blocks.");
            }

            return errorMessage.ToString().Trim();
        }



        public override void Close()
        {
            base.Close();
            // Additional cleanup if needed
        }
    }
}
