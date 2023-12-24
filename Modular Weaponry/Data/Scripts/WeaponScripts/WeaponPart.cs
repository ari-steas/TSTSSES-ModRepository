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
    public class WeaponPart
    {
        public IMySlimBlock block;
        public PhysicalWeapon memberWeapon = null;
        public List<WeaponPart> connectedParts = new List<WeaponPart>();

        public WeaponPart(IMySlimBlock block)
        {
            this.block = block;

            //MyAPIGateway.Utilities.ShowNotification("Placed valid WeaponPart");

            if (WeaponPartGetter.Instance.AllWeaponParts.ContainsKey(block))
                return;

            WeaponPartGetter.Instance.AllWeaponParts.Add(block, this);

            if (WeaponDefiniton.BaseBlock == block.BlockDefinition.Id.SubtypeName)
            {
                memberWeapon = new PhysicalWeapon(this);
            }
            else
                WeaponPartGetter.Instance.QueuedConnectionChecks.Add(this);
        }

        public void CheckForExistingWeapon()
        {
            // You can't have two baseblocks per weapon
            if (WeaponDefiniton.BaseBlock != block.BlockDefinition.Id.SubtypeName)
                memberWeapon = null;

            List<WeaponPart> validNeighbors = GetValidNeighborParts();

            // Search for neighboring PhysicalWeapons
            foreach (var nBlockPart in validNeighbors)
            {
                if (nBlockPart.memberWeapon == null)
                    continue;
                nBlockPart.memberWeapon.AddPart(this);
                break;
            }

            if (memberWeapon == null)
            {
                //MyAPIGateway.Utilities.ShowNotification("Null memberWeapon");
                if (WeaponDefiniton.BaseBlock == block.BlockDefinition.Id.SubtypeName)
                    MyVisualScriptLogicProvider.SendChatMessage($"CRITICAL ERROR BaseBlock Null memberWeapon", "MW");
                return;
            }

            // Connect non-member blocks & populate connectedParts
            foreach (var nBlockPart in validNeighbors)
            {
                connectedParts.Add(nBlockPart);

                if (nBlockPart.memberWeapon == null)
                {
                    WeaponPartGetter.Instance.QueuedConnectionChecks.Add(nBlockPart);
                    MyAPIGateway.Utilities.ShowNotification("Forced a weapon join");
                }
                else if (nBlockPart.memberWeapon != memberWeapon)
                    MyAPIGateway.Utilities.ShowNotification("Invalid memberWeapon");
                else if (!nBlockPart.connectedParts.Contains(this))
                    nBlockPart.connectedParts.Add(this);
            }

            MyAPIGateway.Utilities.ShowNotification("Connected: " + connectedParts.Count + " | Failed: " + (GetValidNeighbors().Count - connectedParts.Count));
        }

        /// <summary>
        /// Returns attached (as per WeaponPart) neighbor blocks.
        /// </summary>
        /// <returns></returns>
        public List<IMySlimBlock> GetValidNeighbors()
        {
            List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
            block.GetNeighbours(neighbors);
            List<IMySlimBlock> validNeighbors = new List<IMySlimBlock>();
            foreach (var nBlock in neighbors)
            {
                if (WeaponDefiniton.DoesBlockConnect(block, nBlock, true))
                    validNeighbors.Add(nBlock);
            }
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
                if (WeaponPartGetter.Instance.AllWeaponParts.TryGetValue(nBlock, out nBlockPart))
                {
                    validNeighbors.Add(nBlockPart);
                }
            }

            return validNeighbors;
        }
    }
}