using Sandbox.Common.ObjectBuilders;
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
        public List<WeaponPart> connectedParts;

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
                CheckForExistingWeapon();
        }

        private void CheckForExistingWeapon()
        {
            // You can't have two baseblocks per weapon
            if (WeaponDefiniton.BaseBlock == block.BlockDefinition.Id.SubtypeName)
                return;

            List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
            block.GetNeighbours(neighbors);
            foreach (var nBlock in neighbors)
            {
                if (!WeaponDefiniton.DoesBlockConnect(block, nBlock, true))
                    continue;

                WeaponPart nBlockPart;
                if (WeaponPartGetter.Instance.AllWeaponParts.TryGetValue(nBlock, out nBlockPart) && nBlockPart.memberWeapon != null)
                {
                    nBlockPart.memberWeapon.AddPart(this);
                    return;
                }
            }
        }
    }
}