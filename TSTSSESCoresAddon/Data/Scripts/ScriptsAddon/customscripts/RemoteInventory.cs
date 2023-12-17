using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Aristeas.RemoteInventory.Data.Scripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class InventoryGrabber : MySessionComponentBase
    {
        const double MaxRange = 1000;
        const int TimeBetweenInventory = 60;
        public static readonly string[] ValidBlockSubtypes = new string[]
        {
            "LargeBlockLargeGenerator"
        };


        int lastInventoryTry = 0;

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                // If not currently in GUI screen && Ctrl-R pressed
                if (
                    MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None &&
                    MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftControl) &&
                    MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.R))
                {
                    // Wait between calls, as I suspect there is a large performance implication for this
                    if (lastInventoryTry > TimeBetweenInventory)
                    {
                        GetInventory();
                        lastInventoryTry = 0;
                    }
                }

                lastInventoryTry++;
            }
        }

        public void GetInventory()
        {
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            // Sort entities by distance from player
            Vector3D playerPos = MyAPIGateway.Session.Player.GetPosition();
            IOrderedEnumerable<IMyEntity> sortedEntities = entities.ToList().OrderBy(e => Vector3D.DistanceSquared(e.GetPosition(), playerPos));

            foreach (var entity in sortedEntities)
            {
                // Iterates through all entities, closest first.
                // If succeeds or out of range, return.
                if (Vector3D.Distance(entity.GetPosition(), playerPos) > MaxRange)
                    break;
                
                if (TryOpenEntityContainers(entity))
                    return;
            }

            MyAPIGateway.Utilities.ShowNotification($"No grids in range {MaxRange}m!");
        }

        private bool TryOpenEntityContainers(IMyEntity entity)
        {
            // Get all CubeGrids and blocks with inventory on them
            IMyCubeGrid grid = entity as IMyCubeGrid;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, block => block.FatBlock != null && block.FatBlock.HasInventory);

            MyCubeBlock interactBlock = null;

            // Check blocks for ownership
            foreach (var block in blocks)
            {
                if (ValidBlockSubtypes.Length > 0 && !ValidBlockSubtypes.Contains(block.BlockDefinition.Id.SubtypeName))
                    continue;

                int relations = (int)block.FatBlock.GetUserRelationToOwner(MyAPIGateway.Session.Player.IdentityId);
                // If relations are Owner, Faction, or Neutral
                if (relations == 0 || relations == 1 || relations == 2 || relations == 3)
                {
                    interactBlock = (MyCubeBlock)block.FatBlock;
                    break;
                }
            }

            if (interactBlock == null)
                return false;

            try
            {
                List<IMyUseObject> useObjects = new List<IMyUseObject>();

                interactBlock.UseObjectsComponent.GetInteractiveObjects(useObjects);

                if (useObjects.Count > 0)
                {
                    useObjects[0].Use(UseActionEnum.OpenInventory, MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity);
                    return true;
                }
                else
                {
                    MyAPIGateway.Utilities.ShowNotification("NO USEOBJECTS", 5000);
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("NULL IN INNER METHOD", 5000);
            }

            MyAPIGateway.Utilities.ShowNotification($"{grid.CustomName}: {interactBlock?.DisplayNameText}");
            return false;
        }

        /// <summary>
        /// Checks if player exists and is currently in their suit.
        /// </summary>
        /// <returns></returns>
        private bool IsPlayerValid()
        {
            return MyAPIGateway.Session.Player != null &&
                MyAPIGateway.Session.Player.Controller != null &&
                MyAPIGateway.Session.Player.Controller.ControlledEntity != null &&
                MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity != null &&
                MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity is IMyCharacter;
        }
    }
}
