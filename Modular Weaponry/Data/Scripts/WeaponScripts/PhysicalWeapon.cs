using Modular_Weaponry.Data.Scripts.WeaponScripts.DebugDraw;
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
using VRageMath;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    public class PhysicalWeapon
    {
        public WeaponPart basePart;
        private readonly List<WeaponPart> componentParts = new List<WeaponPart>();

        public int numReactors = 0;
        private Color color;

        public void Update()
        {
            foreach (var part in componentParts)
                DebugDrawManager.Instance.DrawGridPoint0(part.block.Position, part.block.CubeGrid, color);
        }

        public PhysicalWeapon(WeaponPart basePart)
        {
            this.basePart = basePart;
            componentParts.Add(basePart);
            WeaponPartGetter.Instance.AllPhysicalWeapons.Add(this);
            RecursiveWeaponChecker(basePart);
            Random r = new Random();
            color = new Color(r.Next(255), r.Next(255), r.Next(255));
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
            if (componentParts.Count == 0)
                WeaponPartGetter.Instance.AllPhysicalWeapons.Remove(this);
        }

        private void RecursiveWeaponChecker(WeaponPart currentBlock)
        {
            List<IMySlimBlock> slimNeighbors = new List<IMySlimBlock>();
            currentBlock.block.GetNeighbours(slimNeighbors);
        
            foreach (IMySlimBlock neighbor in slimNeighbors)
            {
                if (WeaponDefiniton.IsBlockAllowed(neighbor) && WeaponDefiniton.DoesBlockConnect(currentBlock.block, neighbor))
                {
                    WeaponPart neighborPart;
                    
                    if (WeaponPartGetter.Instance.AllWeaponParts.TryGetValue(neighbor, out neighborPart))
                    {
                        // Avoid double-including blocks
                        if (componentParts.Contains(neighborPart))
                        {
                            MyLog.Default.WriteLineAndConsole("Skip part " + neighbor.BlockDefinition.Id.SubtypeName + " @ " + neighbor.Position);
                            continue;
                        }
        
                        MyLog.Default.WriteLineAndConsole("Add part " + neighbor.BlockDefinition.Id.SubtypeName + " @ " + neighbor.Position);
        
                        componentParts.Add(neighborPart);
                        RecursiveWeaponChecker(neighborPart);
                    }
                }
            }
        }
    }
}
