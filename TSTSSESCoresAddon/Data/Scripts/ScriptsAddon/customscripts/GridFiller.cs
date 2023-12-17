using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Specials.ShipClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //This takes effect works after the grid is cut/pasted or spec block is deleted
        private static void OnSpecBlockCreated(object specBlock)
        {
            var tBlock = SpecBlockHooks.GetBlockSpecCore(specBlock);
            if (tBlock == null)
                return;
            MyAPIGateway.Utilities.ShowNotification($"SpecBlock {tBlock.DisplayNameText} placed!");

            AddBlock<MyObjectBuilder_Reactor>(tBlock, "LargeBlockSmallGenerator", tBlock.Position + (Vector3I)tBlock.LocalMatrix.Forward);
            AddBlock<MyObjectBuilder_CargoContainer>(tBlock, "LargeBlockSmallContainer", tBlock.Position + (Vector3I)tBlock.LocalMatrix.Backward);
        }

        private static void OnSpecBlockDestroyed(object specBlock)
        {
            var tBlock = SpecBlockHooks.GetBlockSpecCore(specBlock);
            if (tBlock == null)
                return;
            MyAPIGateway.Utilities.ShowNotification($"SpecBlock {tBlock.DisplayNameText} removed!");
        }

        private static void AddBlock<T>(IMyTerminalBlock block, string subtypeName, Vector3I position) where T : MyObjectBuilder_CubeBlock, new()
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
                return;
            }
            MyAPIGateway.Utilities.ShowNotification($"{subtypeName} added at {position}", 1000);
        }
    }
}
