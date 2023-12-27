using CoreSystems.Api;
using Modular_Weaponry.Data.Scripts.WeaponScripts.DebugDraw;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GUI;
using Sandbox.ModAPI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions.DefinitionDefs;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    public class ModularDefinition
    {

        public string[] AllowedBlocks = null;

        public Dictionary<string, Vector3I[]> AllowedConnections = null;

        public string BaseBlockSubtype = null;
        public string Name = null;


        public static ModularDefinition Load(PhysicalDefinition definition)
        {
            ModularDefinition def = new ModularDefinition()
            {
                AllowedBlocks = definition.AllowedBlocks,
                AllowedConnections = definition.AllowedConnections,
                BaseBlockSubtype = definition.BaseBlock,
                Name = definition.Name,
            };

            if (def.AllowedBlocks == null || def.AllowedConnections == null || def.BaseBlockSubtype == null || def.Name == null)
            {
                MyLog.Default.WriteLine("Modular Weaponry: !!Failed!! to create new ModularDefinition for " + definition.Name);
                return null;
            }

            MyLog.Default.WriteLine("Modular Weaponry: Created new ModularDefinition for " + definition.Name);
            return def;
        }

        //public VRage.MyTuple<bool, Vector3D, Vector3D, float> ChangeProjectileData(long firerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition)
        //{
        //    Vector3D velocityOffset = -WeaponPartGetter.Instance.wAPI.GetProjectileState(projectileId).Item2 * 0.5;
        //    MyAPIGateway.Utilities.ShowNotification("Projectile " + Math.Round(velocityOffset.Length(), 2));
        //    return new VRage.MyTuple<bool, Vector3D, Vector3D, float>(false, projectilePosition, velocityOffset, 0);
        //}

        public bool DoesBlockConnect(IMySlimBlock block, IMySlimBlock adajent, bool lineCheck = true)
        {
            // Check if adajent block connects first, but don't make an infinite loop
            if (lineCheck)
                if (!DoesBlockConnect(adajent, block, false))
                    return false;
            
            // Get local offset for below
            Matrix localOrientation;
            block.Orientation.GetMatrix(out localOrientation);

            if (AllowedConnections.ContainsKey(block.BlockDefinition.Id.SubtypeName))
            {
                foreach (Vector3I allowedPos in AllowedConnections[block.BlockDefinition.Id.SubtypeName])
                {
                    Vector3I offsetAllowedPos = (Vector3I)Vector3D.Rotate((Vector3D)allowedPos, localOrientation) + block.Position;

                    if (offsetAllowedPos.IsInsideInclusiveEnd(adajent.Min, adajent.Max))
                    {
                        if (WeaponPartManager.Instance.DebugMode)
                            DebugDrawManager.Instance.AddGridPoint(offsetAllowedPos, block.CubeGrid, Color.Green, 3);
                        return true;
                    }
                    if (WeaponPartManager.Instance.DebugMode)
                        DebugDrawManager.Instance.AddGridPoint(offsetAllowedPos, block.CubeGrid, Color.Red, 3);
                }
                return false;
            }

            // Return true by default.
            return true;
        }

        public bool IsTypeAllowed(string type)
        {
            foreach (string id in AllowedBlocks)
                if (type == id)
                    return true;
            return false;
        }

        public bool IsBlockAllowed(IMySlimBlock block)
        {
            return IsTypeAllowed(block.BlockDefinition.Id.SubtypeName);
        }
    }
}
