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

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    public static class WeaponDefiniton
    {

        private readonly static string[] AllowedBlocks =
        {
            "MA_AC150",
            "LargeBlockSmallGenerator"
        };

        private readonly static Dictionary<string, Vector3I[]> AllowedConnections = new Dictionary<string, Vector3I[]>
        {
            {
                "LargeBlockSmallGenerator", new Vector3I[] {
                    Vector3I.Backward,
                    Vector3I.Up
            }},
        };

        public readonly static string BaseBlock = "MA_AC150";

        public static int numReactors = 0;

        public static VRage.MyTuple<bool, Vector3D, Vector3D, float> ChangeProjectileData(long firerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition)
        {
            Vector3D velocityOffset = -WeaponPartGetter.Instance.wAPI.GetProjectileState(projectileId).Item2 / (numReactors > 0 ? numReactors : 1);
            MyAPIGateway.Utilities.ShowNotification("Projectile " + Math.Round(velocityOffset.Length(), 2));
            return new VRage.MyTuple<bool, Vector3D, Vector3D, float>(false, projectilePosition, velocityOffset, 0);
        }

        public static bool DoesBlockConnect(IMySlimBlock block, IMySlimBlock adajent, bool lineCheck = true)
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
                        DebugDrawManager.Instance.AddGridPoint(offsetAllowedPos, block.CubeGrid, 3, Color.Green);
                        return true;
                    }

                    DebugDrawManager.Instance.AddGridPoint(offsetAllowedPos, block.CubeGrid, 3, Color.Red);
                }
                return false;
            }

            // Return true by default.
            return true;
        }

        public static bool IsTypeAllowed(string type)
        {
            foreach (string id in AllowedBlocks)
                if (type == id)
                    return true;
            return false;
        }

        public static bool IsBlockAllowed(IMySlimBlock block)
        {
            return IsTypeAllowed(block.BlockDefinition.Id.SubtypeName);
        }
    }
}
