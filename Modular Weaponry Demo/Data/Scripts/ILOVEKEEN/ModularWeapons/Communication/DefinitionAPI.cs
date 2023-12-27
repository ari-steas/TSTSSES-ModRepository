using CoreSystems.Api;
using ILOVEKEEN.Scripts.ModularWeaponry;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace CoreParts.Data.Scripts.ILOVEKEEN.ModularWeaponry.Communication
{
    public class ModularDefinitionAPI
    {
        /// <summary>
        /// Plug this into a WcAPI.SetProjectileState call. Returns the acceleration offset needed to set a projectile to a given relative speed.
        /// </summary>
        /// <param name="desiredSpeed"></param>
        /// <param name="projectileId"></param>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public Vector3D OffsetProjectileVelocity(float desiredSpeed, ulong projectileId, long blockId)
        {
            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockId);
            if (entity is IMyCubeBlock)
                return OffsetProjectileVelocity(desiredSpeed, projectileId, ((IMyCubeBlock)entity).CubeGrid);
            return Vector3D.Zero;
        }

        /// <summary>
        /// Plug this into a WcAPI.SetProjectileState call. Returns the acceleration offset needed to set a projectile to a given relative speed.
        /// </summary>
        /// <param name="desiredSpeed"></param>
        /// <param name="projectileId"></param>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public Vector3D OffsetProjectileVelocity(float desiredSpeed, ulong projectileId, IMyCubeGrid grid)
        {
            Vector3D currentProjectileVelocity = ModularDefinition.WcAPI.GetProjectileState(projectileId).Item2;
            Vector3D baseProjectileVelocity = currentProjectileVelocity - grid.LinearVelocity;

            baseProjectileVelocity = baseProjectileVelocity.Normalized() * desiredSpeed;

            return baseProjectileVelocity + grid.LinearVelocity - currentProjectileVelocity;
        }

        /// <summary>
        /// Plug this into a WcAPI.SetProjectileState call. Returns the acceleration offset needed to multiply a projectile's relative speed.
        /// </summary>
        /// <param name="desiredSpeed"></param>
        /// <param name="projectileId"></param>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public Vector3D MultiplyProjectileVelocity(float multiplier, ulong projectileId, long blockId)
        {
            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockId);
            if (entity is IMyCubeBlock)
                return OffsetProjectileVelocity(multiplier, projectileId, ((IMyCubeBlock)entity).CubeGrid);
            return Vector3D.Zero;
        }

        /// <summary>
        /// Plug this into a WcAPI.SetProjectileState call. Returns the acceleration offset needed to multiply a projectile's relative speed.
        /// </summary>
        /// <param name="desiredSpeed"></param>
        /// <param name="projectileId"></param>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public Vector3D MultiplyProjectileVelocity(float multiplier, ulong projectileId, IMyCubeGrid grid)
        {
            Vector3D currentProjectileVelocity = ModularDefinition.WcAPI.GetProjectileState(projectileId).Item2;
            Vector3D baseProjectileVelocity = currentProjectileVelocity - grid.LinearVelocity;

            baseProjectileVelocity *= multiplier;

            return baseProjectileVelocity + grid.LinearVelocity - currentProjectileVelocity;
        }

        /// <summary>
        /// Returns the IMyCubeGrid of a given IMyCubeBlock's EntityId.
        /// </summary>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public IMyCubeGrid GetGridFromBlockId(long blockId)
        {
            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockId);
            if (entity is IMyCubeBlock)
                return ((IMyCubeBlock)entity).CubeGrid;
            return null;
        }


        #region API calls

        private Func<MyEntity[]> _getAllParts;
        private Func<int[]> _getAllWeapons;
        private Func<int, MyEntity[]> _getMemberParts;
        private Func<MyEntity, bool, MyEntity[]> _getConnectedBlocks;
        private Func<int, MyEntity> _getBasePart;

        /// <summary>
        /// Gets all WeaponParts in the world. Returns an array of all WeaponParts.
        /// </summary>
        public MyEntity[] GetAllParts()
        {
            return _getAllParts?.Invoke();
        }

        /// <summary>
        /// Gets all PhysicalWeapon ids in the world. Returns an empty list on fail.
        /// <para>
        /// Arg1 is weapon id
        /// </para>
        /// </summary>
        public int[] GetAllWeapons()
        {
            return _getAllWeapons?.Invoke();
        }

        /// <summary>
        /// Gets all member parts of a weapon. Returns an empty list on fail.
        /// <para>
        /// Arg1 is EntityId
        /// </para>
        /// </summary>
        public MyEntity[] GetMemberParts(int weaponId)
        {
            return _getMemberParts?.Invoke(weaponId);
        }

        /// <summary>
        /// Gets all connected parts to a block. Returns an empty list on fail.
        /// <para>
        /// <paramref name="useCached"/>: Set this to 'false' if used in OnPartAdd.
        /// </para>
        /// </summary>
        public MyEntity[] GetConnectedBlocks(MyEntity partBlockId, bool useCached = true)
        {
            return _getConnectedBlocks?.Invoke(partBlockId, useCached);
        }

        /// <summary>
        /// Gets the base part of a PhysicalWeapon. Returns null if weapon does not exist.
        /// </summary>
        public MyEntity GetBasePart(int weaponId)
        {
            return _getBasePart?.Invoke(weaponId);
        }






        public bool IsReady = false;
        private bool _isRegistered = false;
        private bool _apiInit = false;
        private long ApiChannel = 8774;

        public void ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            _apiInit = (delegates != null);
            AssignMethod(delegates, "GetAllParts", ref _getAllParts);
            AssignMethod(delegates, "GetAllWeapons", ref _getAllWeapons);
            AssignMethod(delegates, "GetMemberParts", ref _getMemberParts);
            AssignMethod(delegates, "GetConnectedBlocks", ref _getConnectedBlocks);
            AssignMethod(delegates, "GetBasePart", ref _getBasePart);

            if (_apiInit)
                MyLog.Default.WriteLine("ModularDefinitions: ModularDefinitionsAPI loaded!");
            else
                MyLog.Default.WriteLine("ModularDefinitions: ModularDefinitionsAPI cleared.");
        }

        public void LoadData()
        {
            if (_isRegistered)
                throw new Exception($"{GetType().Name}.Load() should not be called multiple times!");

            _isRegistered = true;
            MyAPIGateway.Utilities.RegisterMessageHandler(ApiChannel, HandleMessage);
            MyAPIGateway.Utilities.SendModMessage(ApiChannel, "ApiEndpointRequest");
            MyLog.Default.WriteLine("ModularDefinitions: ModularDefinitionsAPI inited.");
        }

        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ApiChannel, HandleMessage);

            ApiAssign(null);

            _isRegistered = false;
            _apiInit = false;
            IsReady = false;
            MyLog.Default.WriteLine("ModularDefinitions: ModularDefinitionsAPI unloaded.");
        }

        private void HandleMessage(object obj)
        {
            if (_apiInit || obj is string) // the sent "ApiEndpointRequest" will also be received here, explicitly ignoring that
            {
                MyLog.Default.WriteLine($"ModularDefinitions: ModularDefinitionsAPI ignored message {obj as string}!");
                return;
            }

            var dict = obj as IReadOnlyDictionary<string, Delegate>;

            if (dict == null)
            {
                MyLog.Default.WriteLine("ModularDefinitions: ModularDefinitionsAPI ERR: Recieved null dictionary!");
                return;
            }

            ApiAssign(dict);
            IsReady = true;
        }

        private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field)
            where T : class
        {
            if (delegates == null)
            {
                field = null;
                return;
            }

            Delegate del;
            if (!delegates.TryGetValue(name, out del))
                throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");

            field = del as T;

            if (field == null)
                throw new Exception(
                    $"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
        }

        #endregion
    }
}
