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
        private List<WeaponPart> componentParts = new List<WeaponPart>();

        public int numReactors = 0;
        private Color color;

        public void Update()
        {
            foreach (var part in componentParts)
                //DebugDrawManager.Instance.DrawGridPoint0(part.block.Position, part.block.CubeGrid, color);
                foreach (var conPart in part.connectedParts)
                    DebugDrawManager.Instance.AddLine(DebugDrawManager.GridToGlobal(part.block.Position, part.block.CubeGrid), DebugDrawManager.GridToGlobal(conPart.block.Position, part.block.CubeGrid), color, 1/60);
            MyAPIGateway.Utilities.ShowNotification("PW Parts: " + componentParts.Count, 1000 / 60);
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
            //MyAPIGateway.Utilities.ShowNotification("Reactors: " + numReactors);

            MyAPIGateway.Utilities.ShowNotification("Weapon parts: " + componentParts.Count);
        }

        public void Remove(WeaponPart part, bool removeFromList = true)
        {
            if (removeFromList)
            {
                if (!componentParts.Contains(part))
                    return;
                componentParts.Remove(part);
            }

            MyAPIGateway.Utilities.ShowNotification("Weapon parts: " + componentParts.Count);
            MyAPIGateway.Utilities.ShowNotification("Subpart parts: " + part.connectedParts.Count);

            // Clear self if basepart was removed
            if (part == basePart)
            {
                foreach (var cPart in componentParts)
                    Remove(cPart, false);
                componentParts.Clear();
            }
            // Split apart if necessary. Recalculates every connection - suboptimal but neccessary, I believe.
            else if (part.connectedParts.Count > 1)
            {
                foreach (var cPart in componentParts)
                {
                    part.connectedParts = null;
                    part.memberWeapon = null;
                }
                componentParts.Clear();
                MyAPIGateway.Utilities.ShowNotification("Recreating connections...");
                RecursiveWeaponChecker(basePart);
                MyAPIGateway.Utilities.ShowNotification("Total weapon parts: " + componentParts.Count);

                return;
            }

            part.connectedParts = null;
            part.memberWeapon = null;

            if (removeFromList && componentParts.Count == 0)
                Close();
        }

        public void Close()
        {
            componentParts = null;
            basePart = null;
            WeaponPartGetter.Instance.AllPhysicalWeapons.Remove(this);
        }

        private void RecursiveWeaponChecker(WeaponPart currentBlock)
        {
            // TODO split between threads/ticks

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
