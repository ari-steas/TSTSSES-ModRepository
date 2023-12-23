using CoreSystems.Api;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WeaponPartGetter : MySessionComponentBase
    {
        public static WeaponPartGetter Instance; // the only way to access session comp from other classes and the only accepted static field.
        public Dictionary<IMySlimBlock, WeaponPart> AllWeaponParts = new Dictionary<IMySlimBlock, WeaponPart>();

        private List<IMySlimBlock> queuedBlockAdds = new List<IMySlimBlock>();

        public WcApi wAPI = new WcApi();

        public override void LoadData()
        {
            Instance = this;
            MyAPIGateway.Entities.OnEntityAdd += OnGridAdd;
            wAPI.Load();
        }

        protected override void UnloadData()
        {
            Instance = null; // important for avoiding this object to remain allocated in memory
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Queue gridadds to account for world load/grid pasting
            foreach (var queuedBlock in queuedBlockAdds)
            {
                OnBlockAdd(queuedBlock);
            }
            queuedBlockAdds.Clear();
        }

        private void OnGridAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;

            IMyCubeGrid grid = (IMyCubeGrid) entity;

            // Exclude projected and held grids
            if (grid.Physics == null)
                return;

            grid.OnBlockAdded += OnBlockAdd;
            grid.OnBlockRemoved += OnBlockRemove;

            List<IMySlimBlock> existingBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(existingBlocks);
            foreach (var block in existingBlocks)
                queuedBlockAdds.Add(block);
        }

        private void OnBlockAdd(IMySlimBlock block)
        {
            if (wAPI.IsReady && block.FatBlock != null)
            {
                try
                {
                    if (wAPI.HasCoreWeapon((MyEntity)block.FatBlock))
                    {
                        wAPI.AddProjectileCallback((MyEntity)block.FatBlock, 0, ProjectileCallback);
                    }
                }
                catch
                {
                    MyAPIGateway.Utilities.ShowNotification("it threw an error dumbass");
                }
            }

            if (!WeaponDefiniton.IsBlockAllowed(block))
                return;

            WeaponPart w = new WeaponPart(block);
        }
        private void ProjectileCallback(long firerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition, bool projectileExists)
        {
            if (projectileExists)
                wAPI.SetProjectileState(projectileId, WeaponDefiniton.ChangeProjectileData(firerEntityId, firerPartId, projectileId, targetEntityId, projectilePosition));
        }







        private void OnBlockRemove(IMySlimBlock block)
        {
            WeaponPart part;
            if (AllWeaponParts.TryGetValue(block, out part))
            {
                //MyAPIGateway.Utilities.ShowNotification("Removing");
                part.memberWeapon?.Remove(part);
                AllWeaponParts.Remove(block);
            }
        }
    }
}
