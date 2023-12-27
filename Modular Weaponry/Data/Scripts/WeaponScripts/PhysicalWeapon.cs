using CoreSystems.Api;
using Modular_Weaponry.Data.Scripts.WeaponScripts.DebugDraw;
using Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    /// <summary>
    /// The collection of WeaponParts attached to a modular weapon base.
    /// </summary>
    public class PhysicalWeapon
    {
        public WeaponPart basePart;
        public List<WeaponPart> componentParts = new List<WeaponPart>();
        public ModularDefinition WeaponDefinition;
        public int id = -1;

        public int numReactors = 0;
        private Color color;

        public void Update()
        {
            if (WeaponPartManager.Instance.DebugMode)
            {
                foreach (var part in componentParts)
                {
                    DebugDrawManager.Instance.AddGridPoint(part.block.Position, part.block.CubeGrid, color, 0f);
                    foreach (var conPart in part.connectedParts)
                        DebugDrawManager.Instance.AddLine(DebugDrawManager.GridToGlobal(part.block.Position, part.block.CubeGrid), DebugDrawManager.GridToGlobal(conPart.block.Position, part.block.CubeGrid), color, 0f);
                }
                MyAPIGateway.Utilities.ShowNotification(id + " PW Parts: " + componentParts.Count, 1000 / 60);
            }
        }

        public PhysicalWeapon(int id, WeaponPart basePart, ModularDefinition WeaponDefinition)
        {
            this.basePart = basePart;
            this.WeaponDefinition = WeaponDefinition;
            this.id = id;
            WeaponPartManager.Instance.CreatedPhysicalWeapons++;

            if (WeaponPartManager.Instance.AllPhysicalWeapons.ContainsKey(id))
                throw new Exception("Duplicate weapon ID!");
            WeaponPartManager.Instance.AllPhysicalWeapons.Add(id, this);

            Random r = new Random();
            color = new Color(r.Next(255), r.Next(255), r.Next(255));

            // Register projectile callback
            if (WeaponPartManager.Instance.wAPI.IsReady && basePart.block.FatBlock != null)
            {
                try
                {
                    if (WeaponPartManager.Instance.wAPI.HasCoreWeapon((MyEntity)basePart.block.FatBlock))
                    {
                        WeaponPartManager.Instance.wAPI.AddProjectileCallback((MyEntity)basePart.block.FatBlock, 0, ProjectileCallback);
                    }
                }
                catch
                {
                    MyAPIGateway.Utilities.SendMessage($"Error in registering projectile callback!\nDefinition: {WeaponDefinition.Name}\nBasePart: {basePart.block.BlockDefinition.Id.SubtypeName}");
                }
            }

            AddPart(basePart);
            WeaponPartManager.Instance.QueuedWeaponChecks.Add(basePart, this);
        }

        public void ProjectileCallback(long firerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition, bool projectileExists)
        {
            if (projectileExists)
                DefinitionHandler.Instance.SendOnShoot(WeaponDefinition.Name, id, firerEntityId, firerPartId, projectileId, targetEntityId, projectilePosition);
        }

        public void UpdateProjectile(ulong projectileId, MyTuple<bool, Vector3D, Vector3D, float> projectileData)
        {
            WeaponPartManager.Instance.wAPI.SetProjectileState(projectileId, projectileData);
        }

        public void AddPart(WeaponPart part, bool triggerDefinition = true)
        {
            if (componentParts.Contains(part))
            {
                componentParts.Remove(part);
                triggerDefinition = false;
            }

            componentParts.Add(part);
            part.memberWeapon = this;
            if (triggerDefinition)
                DefinitionHandler.Instance.SendOnPartAdd(WeaponDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);
        }

        /// <summary>
        /// Removes a part without running connection checks. Only use when the PhysicalWeapon will be removed.
        /// </summary>
        /// <param name="part"></param>
        public void RemoveFast(WeaponPart part)
        {
            if (componentParts == null || part == null)
                return;

            if (!componentParts.Contains(part))
                return;
            componentParts.Remove(part);

            part.connectedParts.Clear();
            part.memberWeapon = null;

            DefinitionHandler.Instance.SendOnPartRemove(WeaponDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);

            if (componentParts.Count == 0)
                Close();
        }

        public void Remove(WeaponPart part)
        {
            if (componentParts == null || part == null)
                return;

            if (!componentParts.Contains(part))
                return;
            componentParts.Remove(part);

            DefinitionHandler.Instance.SendOnPartRemove(WeaponDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);
            if (part.block.Integrity == 0)
                DefinitionHandler.Instance.SendOnPartDestroy(WeaponDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);

            //MyAPIGateway.Utilities.ShowNotification("Subpart parts: " + part.connectedParts.Count);

            // Clear self if basepart was removed
            if (part == basePart)
            {
                foreach (var cPart in componentParts.ToList())
                    ResetPart(cPart);
                Close();
                return;
            }
            // Split apart if necessary. Recalculates every connection - suboptimal but neccessary, I believe.
            else if (part.connectedParts.Count > 1)
            {
                foreach (var cPart in componentParts.ToList())
                    ResetPart(cPart);
                componentParts.Clear();

                // Above loop removes all parts's memberWeapons, but the base should always have one.
                basePart.memberWeapon = this;
                componentParts.Add(basePart);

                if (WeaponPartManager.Instance.DebugMode)
                    MyAPIGateway.Utilities.ShowNotification("Recreating connections...");
                WeaponPartManager.Instance.QueuedConnectionChecks.Add(basePart);
                WeaponPartManager.Instance.QueuedWeaponChecks.Add(basePart, this);

                return;
            }

            // Make doubly and triply sure that each part does not remember this one.
            foreach (var cPart in part.connectedParts)
            {
                int idx = cPart.connectedParts.IndexOf(part);
                if (idx >= 0)
                {
                    cPart.connectedParts.RemoveAt(idx);
                }
            }

            part.connectedParts.Clear();
            part.memberWeapon = null;

            if (componentParts.Count == 0)
                Close();
        }

        private void ResetPart(WeaponPart part)
        {
            if (part == null)
                return;
            part.memberWeapon = null;
            part.connectedParts.Clear();
            WeaponPartManager.Instance.QueuedConnectionChecks.Add(part);
        }

        public void Close()
        {
            if (basePart != null && basePart.block != null)
                WeaponPartManager.Instance.wAPI.RemoveProjectileCallback((MyEntity)basePart.block.FatBlock, 0, ProjectileCallback);

            if (componentParts == null)
                return;

            componentParts = null;
            basePart = null;
            WeaponPartManager.Instance.AllPhysicalWeapons.Remove(id);
        }

        public void RecursiveWeaponChecker(WeaponPart currentBlock)
        {
            // Safety check
            if (currentBlock == null || currentBlock.block == null) return;

            // TODO split between threads/ticks
            currentBlock.memberWeapon = this;

            List<IMySlimBlock> slimNeighbors = new List<IMySlimBlock>();
            currentBlock.block.GetNeighbours(slimNeighbors);
        
            foreach (IMySlimBlock neighbor in slimNeighbors)
            {
                // Another safety check
                if (neighbor == null) continue;

                if (WeaponDefinition.IsBlockAllowed(neighbor) && WeaponDefinition.DoesBlockConnect(currentBlock.block, neighbor))
                {
                    WeaponPart neighborPart;
                    
                    if (WeaponPartManager.Instance.AllWeaponParts.TryGetValue(neighbor, out neighborPart))
                    {
                        // Avoid double-including blocks
                        if (componentParts.Contains(neighborPart))
                        {
                            //MyLog.Default.WriteLine("ModularWeapons: Skip part " + neighbor.BlockDefinition.Id.SubtypeName + " @ " + neighbor.Position);
                            continue;
                        }

                        //MyLog.Default.WriteLine("ModularWeapons: Add part " + neighbor.BlockDefinition.Id.SubtypeName + " @ " + neighbor.Position);

                        componentParts.Add(neighborPart);
                        WeaponPartManager.Instance.QueuedConnectionChecks.Add(neighborPart);
                        WeaponPartManager.Instance.QueuedWeaponChecks.Add(neighborPart, this);
                    }
                }
            }
        }
    }
}
