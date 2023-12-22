using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class PhysicalWeapon : MySessionComponentBase
    {
        public WeaponPart basePart;
        private List<WeaponPart> componentParts = new List<WeaponPart>();

        public int numReactors = 0;

        public PhysicalWeapon(WeaponPart basePart)
        {
            this.basePart = basePart;
            RecursiveWeaponChecker(basePart);
            componentParts.Add(basePart);

            MyAPIGateway.Utilities.ShowNotification("Weapon parts: " + componentParts.Count);
        }

        public void AddPart(WeaponPart part)
        {
            componentParts.Add(part);
            part.memberWeapon = this;

            // TODO remove. Test for counting number of reactors; i.e. PAC cannon
            numReactors = 0;
            foreach (var cPart in componentParts)
            {
                if (cPart.block.BlockDefinition.Id.SubtypeName == "LargeBlockSmallGenerator")
                {
                    List<IMySlimBlock> blocks2 = new List<IMySlimBlock>();
                    cPart.block.GetNeighbours(blocks2);

                    if (blocks2.Count >= 2)
                        numReactors++;
                }
            }
            WeaponDefiniton.numReactors = numReactors;
            MyAPIGateway.Utilities.ShowNotification("Reactors: " + numReactors);

            MyAPIGateway.Utilities.ShowNotification("Weapon parts: " + componentParts.Count);
        }

        public void Remove(WeaponPart part)
        {
            if (!componentParts.Contains(part))
                return;
            componentParts.Remove(part);
            MyAPIGateway.Utilities.ShowNotification("Weapon parts: " + componentParts.Count);
        }

        private void RecursiveWeaponChecker(WeaponPart currentBlock)
        {
            componentParts.Clear();

            List<IMySlimBlock> slimNeighbors = new List<IMySlimBlock>();
            currentBlock.block.GetNeighbours(slimNeighbors);

            foreach (IMySlimBlock neighbor in slimNeighbors)
            {
                if (WeaponDefiniton.IsBlockAllowed(neighbor))
                {
                    WeaponPart neighborPart;
                    
                    if (WeaponPartGetter.AllWeaponParts.TryGetValue(neighbor, out neighborPart))
                    {
                        // Avoid double-including blocks
                        if (componentParts.Contains(neighborPart))
                            continue;

                        MyLog.Default.WriteLineAndConsole("Add part " + neighbor.BlockDefinition.Id.SubtypeName + " @ " + neighbor.Position);

                        componentParts.Add(neighborPart);
                        RecursiveWeaponChecker(neighborPart);
                    }
                }
            }
        }
    }
}
