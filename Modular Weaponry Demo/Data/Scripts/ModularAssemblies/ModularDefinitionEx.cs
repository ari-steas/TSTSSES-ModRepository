using Scripts.ModularAssemblies.DebugDraw;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static Scripts.ModularAssemblies.Communication.DefinitionDefs;

namespace Scripts.ModularAssemblies.Communication
{

    partial class ModularDefinition
    {
        // You can declare functions in here, and they are shared between all other ModularDefinition files.
        private Dictionary<int, List<MyEntity[]>> Example_ValidArms = new Dictionary<int, List<MyEntity[]>>();
        private List<MyEntity> Example_BufferArm = new List<MyEntity>();
        private int StopHits = 0;

        private int GetNumBlocksInArm(int PhysicalAssemblyId)
        {
            int total = 0;

            foreach (var arm in Example_ValidArms[PhysicalAssemblyId])
                total += arm.Length;

            return total;
        }
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

        private void UpdatePower(int PhysicalAssemblyId)
        {
            IMyReactor basePart = (IMyReactor) ModularAPI.GetBasePart(PhysicalAssemblyId);

            int desiredPower = Example_ValidArms[PhysicalAssemblyId].Count * GetNumBlocksInArm(PhysicalAssemblyId);

            basePart.PowerOutputMultiplier = basePart.MaxOutput;
        }

        // This is the important bit.
        PhysicalDefinition ModularDefinitionExample => new PhysicalDefinition
        {
            Name = "ModularDefinitionExample",

            OnPartAdd = (int PhysicalAssemblyId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                if (!Example_ValidArms.ContainsKey(PhysicalAssemblyId))
                    Example_ValidArms.Add(PhysicalAssemblyId, new List<MyEntity[]>());

                // Scan for 'arms' connected on both ends to the basepart.
                if (IsBaseBlock)
                {
                    
                }
                else
                {
                    MyEntity basePart = ModularAPI.GetBasePart(PhysicalAssemblyId);
                    if (Example_ScanArm(BlockEntity, null, basePart))
                        Example_ValidArms[PhysicalAssemblyId].Add(Example_BufferArm.ToArray());
                    
                    Example_BufferArm.Clear();
                    StopHits = 0;
                }

                MyEntity basePartEntity = ModularAPI.GetBasePart(PhysicalAssemblyId);
                float velocityMult = Example_ValidArms[PhysicalAssemblyId].Count * GetNumBlocksInArm(PhysicalAssemblyId) / 50f;

                if (ModularAPI.IsDebug())
                    MyAPIGateway.Utilities.ShowNotification("Pass: Arms: " + Example_ValidArms[PhysicalAssemblyId].Count + " (Size " + Example_ValidArms[PhysicalAssemblyId][Example_ValidArms[PhysicalAssemblyId].Count - 1].Length + ")");
            },

            OnPartRemove = (int PhysicalAssemblyId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                // Remove if the connection is broken.
                if (!IsBaseBlock)
                {
                    MyEntity[] armToRemove = null;
                    foreach (var arm in Example_ValidArms[PhysicalAssemblyId])
                    {
                        if (arm.Contains(BlockEntity))
                        {
                            armToRemove = arm;
                            break;
                        }
                    }
                    if (armToRemove != null)
                    {
                        Example_ValidArms[PhysicalAssemblyId].Remove(armToRemove);

                        MyEntity basePartEntity = ModularAPI.GetBasePart(PhysicalAssemblyId);
                        float velocityMult = Example_ValidArms[PhysicalAssemblyId].Count * GetNumBlocksInArm(PhysicalAssemblyId) / 50f;
                    }

                    if (ModularAPI.IsDebug())
                        MyAPIGateway.Utilities.ShowNotification("Remove: Arms: " + Example_ValidArms[PhysicalAssemblyId].Count);
                }
                else
                    Example_ValidArms.Remove(PhysicalAssemblyId);
            },

            OnPartDestroy = (int PhysicalAssemblyId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                MyLog.Default.WriteLineAndConsole($"ModularDefinitionEx: OnPartDestroy {IsBaseBlock}");
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
