using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Specials.ShipClass;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace TSTSSESCoresAddon.Data.Scripts.ScriptsAddon.customscripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class GridFiller : MySessionComponentBase
    {
        private static bool isServer;
        private static Dictionary<long, HashSet<long>> blockGroups = new Dictionary<long, HashSet<long>>();

        static GridFiller()
        {
            SpecBlockHooks.OnReady += HooksOnOnReady;
        }

        public override void LoadData()
        {
            isServer = MyAPIGateway.Session.IsServer;
        }

        private static void HooksOnOnReady()
        {
            if (isServer)
            {
                SpecBlockHooks.OnSpecBlockCreated += OnSpecBlockCreated;
                SpecBlockHooks.OnSpecBlockDestroyed += OnSpecBlockDestroyed;
            }
        }

        private static void OnSpecBlockCreated(object specBlock)
        {
            var tBlock = SpecBlockHooks.GetBlockSpecCore(specBlock);
            if (tBlock == null)
                return;

            MyAPIGateway.Utilities.ShowNotification($"SpecBlock {tBlock.DisplayNameText} placed!");

            HashSet<long> associatedBlocks = new HashSet<long>();
            associatedBlocks.Add(AddBlock<MyObjectBuilder_Reactor>(tBlock, "LargeBlockSmallGenerator", tBlock.Position + (Vector3I)tBlock.LocalMatrix.Forward));
            associatedBlocks.Add(AddBlock<MyObjectBuilder_CargoContainer>(tBlock, "LargeBlockSmallContainer", tBlock.Position + (Vector3I)tBlock.LocalMatrix.Backward));

            blockGroups.Add(tBlock.EntityId, associatedBlocks);
            tBlock.CubeGrid.OnBlockRemoved += Grid_OnBlockRemoved;
        }

        private static void OnSpecBlockDestroyed(object specBlock)
        {
            var tBlock = SpecBlockHooks.GetBlockSpecCore(specBlock);
            if (tBlock == null)
                return;

            MyAPIGateway.Utilities.ShowNotification($"SpecBlock {tBlock.DisplayNameText} removed!");
            blockGroups.Remove(tBlock.EntityId);

            tBlock.CubeGrid.OnBlockRemoved -= Grid_OnBlockRemoved;
        }

        private static long AddBlock<T>(IMyTerminalBlock block, string subtypeName, Vector3I position) where T : MyObjectBuilder_CubeBlock, new()
        {
            var grid = block.CubeGrid;

            var nextBlockBuilder = new T
            {
                SubtypeName = subtypeName,
                Min = position,
                BlockOrientation = block.Orientation,
                ColorMaskHSV = new SerializableVector3(0, -1, 0),
                Owner = block.OwnerId,
                EntityId = 0,
                ShareMode = MyOwnershipShareModeEnum.None
            };

            IMySlimBlock newBlock = grid.AddBlock(nextBlockBuilder, false);

            if (newBlock == null)
            {
                MyAPIGateway.Utilities.ShowNotification($"Failed to add {subtypeName}", 1000);
                return 0;
            }
            MyAPIGateway.Utilities.ShowNotification($"{subtypeName} added at {position}", 1000);
            return newBlock.FatBlock.EntityId;
        }

        private static void Grid_OnBlockRemoved(IMySlimBlock block)
        {
            long removedBlockId = block.FatBlock?.EntityId ?? 0;
            foreach (var group in blockGroups)
            {
                if (group.Value.Contains(removedBlockId))
                {
                    MyAPIGateway.Utilities.ShowNotification("A part of a SpecBlock assembly has been removed!", 5000);
                    break;
                }
            }
        }
    }
}
