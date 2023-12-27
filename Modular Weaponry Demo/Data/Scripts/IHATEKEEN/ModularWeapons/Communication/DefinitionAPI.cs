using CoreSystems.Api;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Utils;

namespace CoreParts.Data.Scripts.IHATEKEEN.ModularWeaponry.Communication
{
    public class ModularDefinitionAPI
    {
        private Func<long[]> _getAllParts;
        private Func<int[]> _getAllWeapons;
        private Func<int, long[]> _getMemberParts;
        private Func<long, long[]> _getConnectedBlocks;

        /// <summary>
        /// Gets all parts in the world.
        /// <para>
        /// Arg1 is CubeBlock EntityId
        /// </para>
        /// </summary>
        public long[] GetAllParts()
        {
            return _getAllParts?.Invoke();
        }

        /// <summary>
        /// Gets all weapons in the world.
        /// <para>
        /// Arg1 is weapon id
        /// </para>
        /// </summary>
        public int[] GetAllWeapons()
        {
            return _getAllWeapons?.Invoke();
        }

        /// <summary>
        /// Gets all member parts of a weapon.
        /// <para>
        /// Arg1 is EntityId
        /// </para>
        /// </summary>
        public long[] GetMemberParts(int weaponId)
        {
            return _getMemberParts?.Invoke(weaponId);
        }

        /// <summary>
        /// Gets all connected parts to a block.
        /// </summary>
        public long[] GetConnectedBlocks(long partBlockId)
        {
            return _getConnectedBlocks?.Invoke(partBlockId);
        }

        public bool IsReady = false;
        private bool _isRegistered = false;
        private bool _apiInit = false;
        private long ApiChannel = 8774;

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

        public void ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
        {
            _apiInit = (delegates != null);
            AssignMethod(delegates, "GetAllParts", ref _getAllParts);
            AssignMethod(delegates, "GetAllWeapons", ref _getAllWeapons);
            AssignMethod(delegates, "GetMemberParts", ref _getMemberParts);
            AssignMethod(delegates, "GetConnectedBlocks", ref _getConnectedBlocks);

            if (_apiInit)
                MyLog.Default.WriteLine("ModularDefinitions: ModularDefinitionsAPI loaded!");
            else
                MyLog.Default.WriteLine("ModularDefinitions: ModularDefinitionsAPI cleared.");
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
    }
}
