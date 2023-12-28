using Modular_Weaponry.Data.Scripts.WeaponScripts.DebugDraw;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static Scripts.ILOVEKEEN.ModularWeaponry.Communication.DefinitionDefs;

namespace ILOVEKEEN.Scripts.ModularWeaponry
{

    partial class ModularDefinition
    {
        // You can declare functions in here, and they are shared between all other ModularDefinition files.
        private Dictionary<int, List<MyEntity[]>> Example_ValidArms = new Dictionary<int, List<MyEntity[]>>();
        private List<MyEntity> Example_ScanArm(MyEntity blockEntity, MyEntity prevScan, MyEntity StopAt)
        {
            DebugDrawManager.Instance.AddGridPoint(((IMyCubeBlock)blockEntity).Position, ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);

            List<MyEntity> connectedBlocks = new List<MyEntity>(ModularAPI.GetConnectedBlocks(blockEntity, false));

            // Check if connected to base AND connected to multiple blocks, saves performance
            if (connectedBlocks.Count > 1)
            {
                foreach (var connectedBlock in connectedBlocks)
                {
                    if (connectedBlock != prevScan && connectedBlock != StopAt)
                    {
                        connectedBlocks.AddList(Example_ScanArm(connectedBlock, blockEntity, StopAt));
                        break;
                    }
                }
            }
            else
                return new List<MyEntity>();

            return connectedBlocks;
        }

        // This is the important bit.
        PhysicalDefinition ModularDefinitionExample => new PhysicalDefinition
        {
            Name = "ModularDefinitionExample",

            OnPartAdd = (int PhysicalWeaponId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                // Scan for 'arms' connected on both ends to the basepart.
                if (!IsBaseBlock)
                {
                    MyEntity basePart = ModularAPI.GetBasePart(PhysicalWeaponId);
                    List<MyEntity> scannedArm = Example_ScanArm(BlockEntity, null, basePart);
                    if (scannedArm.Count == 0)
                    {
                        MyAPIGateway.Utilities.ShowNotification("Arms: " + Example_ValidArms[PhysicalWeaponId].Count);
                        return;
                    }

                    Example_ValidArms[PhysicalWeaponId].Add(scannedArm.ToArray());
                }
                else
                    Example_ValidArms.Add(PhysicalWeaponId, new List<MyEntity[]>());

                MyAPIGateway.Utilities.ShowNotification("Arms: " + Example_ValidArms[PhysicalWeaponId].Count);
            },

            OnPartRemove = (int PhysicalWeaponId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                // Remove if the connection is broken.
                if (!IsBaseBlock)
                {
                    MyEntity[] armToRemove = null;
                    foreach (var arm in Example_ValidArms[PhysicalWeaponId])
                    {
                        if (arm.Contains(BlockEntity))
                        {
                            armToRemove = arm;
                            break;
                        }
                    }
                    if (armToRemove != null)
                        Example_ValidArms[PhysicalWeaponId].Remove(armToRemove);
                }
                else
                    Example_ValidArms.Remove(PhysicalWeaponId);
            },

            OnPartDestroy = (int PhysicalWeaponId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                MyLog.Default.WriteLine($"ModularDefinitionEx: OnPartDestroy {IsBaseBlock}");
            },

            OnShoot = (int PhysicalWeaponId, long FirerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition) => {
                return new MyTuple<bool, Vector3D, Vector3D, float>(false, projectilePosition, ModularAPI.OffsetProjectileVelocity(1, projectileId, FirerEntityId), 0);
            },

            AllowedBlocks = new string[]
            {
                "Caster_FocusLens",
                "Caster_Accelerator_0",
                "Caster_Accelerator_90",
            },

            AllowedConnections = new Dictionary<string, Vector3I[]>
            {
                {
                    "Caster_FocusLens", new Vector3I[]
                    {
                        new Vector3I(1, 0, 2),
                        new Vector3I(-1, 0, 2),
                        new Vector3I(0, 1, 2),
                        new Vector3I(0, -1, 2),
                    }
                },
                {
                    "Caster_Accelerator_0", new Vector3I[]
                    {
                        Vector3I.Forward,
                        Vector3I.Backward,
                    }
                },
                {
                    "Caster_Accelerator_90", new Vector3I[]
                    {
                        Vector3I.Forward,
                        Vector3I.Right,
                    }
                },
            },

            BaseBlock = "Caster_FocusLens",
        };
    }
}
