using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using static Scripts.IHATEKEEN.ModularWeaponry.Communication.DefinitionDefs;
using static CoreParts.Data.Scripts.IHATEKEEN.ModularWeaponry.WcApiConn;
using CoreParts.Data.Scripts.IHATEKEEN.ModularWeaponry.Communication;
using CoreSystems.Api;

namespace IHATEKEEN.Scripts.ModularWeaponry
{
    partial class ModularDefinition
    {
        internal DefinitionContainer Container = new DefinitionContainer();
        internal static ModularDefinitionAPI ModularAPI = null;
        internal static WcApi WcAPI = null;

        internal void LoadDefinitions(params PhysicalDefinition[] defs)
        {
            Container.PhysicalDefs = defs;
        }

        /// <summary>
        /// Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static DefinitionContainer GetBaseDefinitions()
        {
            return new ModularDefinition().Container;
        }

        internal Vector3D OffsetProjectileVelocity(float desiredSpeed, ulong projectileId, long blockId)
        {
            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockId);
            if (entity is IMyCubeBlock)
                return OffsetProjectileVelocity(desiredSpeed, projectileId, ((IMyCubeBlock)entity).CubeGrid);
            return Vector3D.Zero;
        }

        internal Vector3D OffsetProjectileVelocity(float desiredSpeed, ulong projectileId, IMyCubeGrid grid)
        {
            Vector3D currentProjectileVelocity = Instance.wAPI.GetProjectileState(projectileId).Item2;
            Vector3D baseProjectileVelocity = currentProjectileVelocity - grid.LinearVelocity;

            baseProjectileVelocity = baseProjectileVelocity.Normalized() * desiredSpeed;

            return baseProjectileVelocity + grid.LinearVelocity - currentProjectileVelocity;
        }

        internal Vector3D MultiplyProjectileVelocity(float multiplier, ulong projectileId, long blockId)
        {
            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockId);
            if (entity is IMyCubeBlock)
                return OffsetProjectileVelocity(multiplier, projectileId, ((IMyCubeBlock)entity).CubeGrid);
            return Vector3D.Zero;
        }

        internal Vector3D MultiplyProjectileVelocity(float multiplier, ulong projectileId, IMyCubeGrid grid)
        {
            Vector3D currentProjectileVelocity = Instance.wAPI.GetProjectileState(projectileId).Item2;
            Vector3D baseProjectileVelocity = currentProjectileVelocity - grid.LinearVelocity;

            baseProjectileVelocity *= multiplier;

            return baseProjectileVelocity + grid.LinearVelocity - currentProjectileVelocity;
        }

        internal IMyCubeGrid GetGridFromBlockId(long blockId)
        {
            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockId);
            if (entity is IMyCubeBlock)
                return ((IMyCubeBlock)entity).CubeGrid;
            return null;
        }
    }
}
