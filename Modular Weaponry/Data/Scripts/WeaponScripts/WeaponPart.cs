using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    /// <summary>
    /// Attached to every part in a WeaponDefinition.
    /// </summary>
    public class WeaponPart
    {
        public IMySlimBlock block;
        public PhysicalWeapon memberWeapon = null;
        public List<WeaponPart> connectedParts = new List<WeaponPart>();
        public ModularDefinition WeaponDefinition;

        public WeaponPart(IMySlimBlock block, ModularDefinition WeaponDefinition)
        {
            this.block = block;
            this.WeaponDefinition = WeaponDefinition;

            //MyAPIGateway.Utilities.ShowNotification("Placed valid WeaponPart");

            if (WeaponPartManager.Instance.AllWeaponParts.ContainsKey(block))
                return;

            WeaponPartManager.Instance.AllWeaponParts.Add(block, this);

            if (WeaponDefinition.BaseBlockSubtype == block.BlockDefinition.Id.SubtypeName)
            {
                memberWeapon = new PhysicalWeapon(WeaponPartManager.Instance.CreatedPhysicalWeapons, this, WeaponDefinition);
            }
            else
                WeaponPartManager.Instance.QueuedConnectionChecks.Add(this);
        }

        public void CheckForExistingWeapon()
        {
            // You can't have two baseblocks per weapon
            //if (WeaponDefinition.BaseBlockSubtype != block.BlockDefinition.Id.SubtypeName)
            //    memberWeapon = null;

            List<WeaponPart> validNeighbors = GetValidNeighborParts();

            // Search for neighboring PhysicalWeapons
            foreach (var nBlockPart in validNeighbors)
            {
                if (nBlockPart.memberWeapon == null)
                    continue;
                nBlockPart.memberWeapon.AddPart(this, memberWeapon != nBlockPart.memberWeapon);
                break;
            }

            if (memberWeapon == null)
            {
                //MyAPIGateway.Utilities.ShowNotification("Null memberWeapon " + validNeighbors.Count);
                if (WeaponDefinition.BaseBlockSubtype == block.BlockDefinition.Id.SubtypeName)
                    MyVisualScriptLogicProvider.SendChatMessage($"CRITICAL ERROR BaseBlock Null memberWeapon", "MW");
                return;
            }

            // Connect non-member blocks & populate connectedParts
            foreach (var nBlockPart in validNeighbors)
            {
                connectedParts.Add(nBlockPart);

                if (nBlockPart.memberWeapon == null)
                {
                    WeaponPartManager.Instance.QueuedConnectionChecks.Add(nBlockPart);
                    //MyAPIGateway.Utilities.ShowNotification("Forced a weapon join");
                }
                //else if (nBlockPart.memberWeapon != memberWeapon)
                //    MyAPIGateway.Utilities.ShowNotification("Invalid memberWeapon");
                else if (!nBlockPart.connectedParts.Contains(this))
                    nBlockPart.connectedParts.Add(this);
            }

            if (WeaponPartManager.Instance.DebugMode)
                MyAPIGateway.Utilities.ShowNotification("Connected: " + connectedParts.Count + " | Failed: " + (GetValidNeighbors().Count - connectedParts.Count));
        }

        /// <summary>
        /// Returns attached (as per WeaponPart) neighbor blocks.
        /// </summary>
        /// <returns></returns>
        public List<IMySlimBlock> GetValidNeighbors(bool MustShareWeapon = false)
        {
            List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
            block.GetNeighbours(neighbors);

            List<IMySlimBlock> validNeighbors = new List<IMySlimBlock>();
            foreach (var nBlock in neighbors)
            {
                if (WeaponDefinition.DoesBlockConnect(block, nBlock, true))
                    validNeighbors.Add(nBlock);
            }

            if (MustShareWeapon)
                validNeighbors.RemoveAll(nBlock =>
                {
                    WeaponPart part;
                    if (!WeaponPartManager.Instance.AllWeaponParts.TryGetValue(nBlock, out part))
                        return true;
                    return part.memberWeapon != this.memberWeapon;
                });

            return validNeighbors;
        }

        /// <summary>
        /// Returns attached (as per WeaponPart) neighbor blocks's parts.
        /// </summary>
        /// <returns></returns>
        private List<WeaponPart> GetValidNeighborParts()
        {
            List<WeaponPart> validNeighbors = new List<WeaponPart>();
            foreach (var nBlock in GetValidNeighbors())
            {
                WeaponPart nBlockPart;
                if (WeaponPartManager.Instance.AllWeaponParts.TryGetValue(nBlock, out nBlockPart))
                {
                    validNeighbors.Add(nBlockPart);
                }
            }

            return validNeighbors;
        }
    }
}