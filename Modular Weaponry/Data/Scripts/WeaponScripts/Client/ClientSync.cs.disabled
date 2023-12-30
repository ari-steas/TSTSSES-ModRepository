using Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static Modular_Weaponry.Data.Scripts.WeaponScripts.Client.ClientSyncDefinitions;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Client
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ClientSync : MySessionComponentBase
    {
        const ushort ProjectileSyncId = 8770;
        const ushort WeaponSyncId = 8769;
        private ClientSync Instance;
        private List<IMyCubeBlock> trackedWeapons = new List<IMyCubeBlock>();

        public override void LoadData()
        {
            // This should only run on clients.
            if (MyAPIGateway.Multiplayer.IsServer)
                return;

            MyLog.Default.WriteLineAndConsole("Modular Weaponry: ClientSync loading...");

            Instance = this;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ProjectileSyncId, ProjectileMessageHandler);

            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
        }

        protected override void UnloadData()
        {
            // This should only run on clients.
            if (MyAPIGateway.Multiplayer.IsServer)
                return;

            MyLog.Default.WriteLineAndConsole("Modular Weaponry: ClientSync closing...");
            Instance = null;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ProjectileSyncId, ProjectileMessageHandler);

            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
        }

        public static void ServerSyncProjectile(long firerId, MyTuple<bool, Vector3D, Vector3D, float> projectileData)
        {
            // This should only run on server.
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            ProjectileContainer container = new ProjectileContainer(firerId, projectileData, DateTime.Now.Ticks);
            byte[] serializedData = MyAPIGateway.Utilities.SerializeToBinary(container);

            MyAPIGateway.Multiplayer.SendMessageToOthers(ProjectileSyncId, serializedData);
            MyLog.Default.WriteLineAndConsole("Syncing projectile " + firerId + " (speed " + projectileData.Item3 + ")");
        }

        private void ProjectileMessageHandler(ushort handlerId, byte[] package, ulong senderId, bool fromServer)
        {
            if (MyAPIGateway.Session.IsServer && fromServer)
                return;
            try
            {
                ProjectileContainer container = MyAPIGateway.Utilities.SerializeFromBinary<ProjectileContainer>(package);

                if (container == null)
                {
                    MyLog.Default.WriteLineAndConsole($"Modular Weaponry: Invalid message from \nHID: {handlerId}\nSID: {senderId}\nFromServer: {fromServer}");
                    return;
                }
                ClientSyncProjectile(container.FirerId, container.ProjectileData, container.Time);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Modular Weaponry: Exception in ClientSync.MessageHandler: {ex}\n{ex.StackTrace}");
            }
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            if (entity is IMyCubeGrid)
            {
                ((IMyCubeGrid)entity).OnBlockAdded += OnBlockAdd;
            }
        }

        private void OnBlockAdd(IMySlimBlock block)
        {
            if (block.FatBlock != null)
                if (block.BlockDefinition.Id.SubtypeId.String == "Caster_FocusLens")
                    ClientSyncWeapon(block.FatBlock);
        }

        private void ClientSyncProjectile(long firerId, MyTuple<bool, Vector3D, Vector3D, float> projectileData, long fireTime)
        {
            double delta = (DateTime.Now.Ticks - fireTime)/(double) TimeSpan.TicksPerSecond;

            Vector3D newPosition = projectileData.Item2 + projectileData.Item3 * delta;
            projectileData.Item2 = newPosition;

            //WeaponPartManager.Instance.wAPI.SetProjectileState(projectileId, projectileData);
            //MyLog.Default.WriteLineAndConsole($"UpdateProj Id: {projectileId} AdditiveSpd: {projectileData.Item3.Length()} ActualSpd: {WeaponPartManager.Instance.wAPI.GetProjectileState(projectileId).Item2.Length()} Delta: {delta}");
        }

        private void ClientSyncWeapon(IMyCubeBlock weapon)
        {
            MyEntity entity = (MyEntity) weapon;

            trackedWeapons.Add(weapon);

            MyLog.Default.WriteLineAndConsole("Modular Weaponry: SYNC");
            WeaponPartManager.Instance.wAPI.AddProjectileCallback(entity, 0, ProjectileCallback);
        }

        private void ProjectileCallback(long firerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition, bool projectileExists)
        {
            MyLog.Default.WriteLineAndConsole($"Modular Weaponry: FIRE {projectileId} Speed: {WeaponPartManager.Instance.wAPI.GetProjectileState(projectileId).Item2.Length()}");
        }
    }
}
