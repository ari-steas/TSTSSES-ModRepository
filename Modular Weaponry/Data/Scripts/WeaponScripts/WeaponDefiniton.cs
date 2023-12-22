using CoreSystems.Api;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    public static class WeaponDefiniton
    {
        public static string[] AllowedBlocks =
        {
            "MA_AC150",
            "LargeBlockSmallGenerator"
        };

        public static string BaseBlock = "MA_AC150";

        public static int numReactors = 0;

        public static VRage.MyTuple<bool, Vector3D, Vector3D, float> ChangeProjectileData(long firerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition)
        {
            Vector3D velocityOffset = -WeaponPartGetter.wAPI.GetProjectileState(projectileId).Item2 / (numReactors > 0 ? numReactors : 1);
            MyAPIGateway.Utilities.ShowNotification("Projectile " + Math.Round(velocityOffset.Length(), 2));

            return new VRage.MyTuple<bool, Vector3D, Vector3D, float>(false, projectilePosition, velocityOffset, 0);
        }

        public static bool DoesBlockConnect(IMySlimBlock block, Vector3I connectionOffset)
        {
            switch (block.BlockDefinition.Id.SubtypeName)
            {
                case "LargeBlockSmallGenerator":
                    return connectionOffset == Vector3I.Backward || connectionOffset == Vector3I.Up;
            }

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
