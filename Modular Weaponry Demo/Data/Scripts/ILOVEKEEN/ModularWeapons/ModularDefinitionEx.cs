using Modular_Weaponry.Data.Scripts.WeaponScripts.DebugDraw;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<MyEntity> Example_BufferArm = new List<MyEntity>();
        private int StopHits = 0;
        private bool Example_ScanArm(MyEntity blockEntity, MyEntity prevScan, MyEntity StopAt)
        {
            if (ModularAPI.IsDebug())
                DebugDrawManager.AddGridPoint(((IMyCubeBlock)blockEntity).Position, ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);
            Example_BufferArm.Add(blockEntity);

            MyEntity[] connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, false);

            foreach (var connectedBlock in connectedBlocks)
            {
                if (connectedBlock == StopAt)
                    StopHits++;

                if (connectedBlock != prevScan && connectedBlock != StopAt)
                {
                    Example_ScanArm(connectedBlock, blockEntity, StopAt);
                }
            }

            return StopHits == 2;
        }

        // This is the important bit.
        PhysicalDefinition ModularDefinitionExample => new PhysicalDefinition
        {
            Name = "ModularDefinitionExample",

            OnPartAdd = (int PhysicalWeaponId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                // Scan for 'arms' connected on both ends to the basepart.
                if (IsBaseBlock)
                {
                    Example_ValidArms.Add(PhysicalWeaponId, new List<MyEntity[]>());
                }
                else
                {
                    MyEntity basePart = ModularAPI.GetBasePart(PhysicalWeaponId);
                    if (!Example_ScanArm(BlockEntity, null, basePart))
                    {
                        //MyAPIGateway.Utilities.ShowNotification("Fail | StopHits: " + StopHits);
                        Example_BufferArm.Clear();
                        StopHits = 0;
                        return;
                    }

                    Example_ValidArms[PhysicalWeaponId].Add(Example_BufferArm.ToArray());
                    Example_BufferArm.Clear();
                    StopHits = 0;
                }

                if (ModularAPI.IsDebug())
                    MyAPIGateway.Utilities.ShowNotification("Pass: Arms: " + Example_ValidArms[PhysicalWeaponId].Count + " (Size " + Example_ValidArms[PhysicalWeaponId][Example_ValidArms[PhysicalWeaponId].Count - 1].Length + ")");
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

                    if (ModularAPI.IsDebug())
                        MyAPIGateway.Utilities.ShowNotification("Remove: Arms: " + Example_ValidArms[PhysicalWeaponId].Count);
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
