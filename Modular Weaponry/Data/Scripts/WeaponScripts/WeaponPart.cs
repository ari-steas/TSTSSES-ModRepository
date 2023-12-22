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

        public WeaponPart(IMySlimBlock block)
        {
            this.block = block;

            //MyAPIGateway.Utilities.ShowNotification("Placed valid WeaponPart");

            if (WeaponPartGetter.AllWeaponParts.ContainsKey(block))
                return;

            WeaponPartGetter.AllWeaponParts.Add(block, this);

            if (WeaponDefiniton.BaseBlock == block.BlockDefinition.Id.SubtypeName)
                memberWeapon = new PhysicalWeapon(this);
            else
                CheckForExistingWeapon();
        }

        private void CheckForExistingWeapon()
        {
            List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
            block.GetNeighbours(neighbors);
            foreach (var nBlock in neighbors)
            {
                WeaponPart nBlockPart;
                if (WeaponPartGetter.AllWeaponParts.TryGetValue(nBlock, out nBlockPart) && nBlockPart.memberWeapon != null)
                {
                    nBlockPart.memberWeapon.AddPart(this);
                    //MyAPIGateway.Utilities.ShowNotification("Added");
                    return;
                }
            }
        }
    }
}