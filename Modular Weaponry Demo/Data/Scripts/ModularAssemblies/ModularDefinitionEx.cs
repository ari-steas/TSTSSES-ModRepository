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
using VRage.ModAPI;
using Sandbox.Game;

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
        private bool Example_ScanArm(MyEntity blockEntity, MyEntity prevScan, string StopAt)
        {
            if (ModularAPI.IsDebug())
                DebugDrawManager.AddGridPoint(((IMyCubeBlock)blockEntity).Position, ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);
            Example_BufferArm.Add(blockEntity);

            MyEntity[] connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, false);
            
            foreach (var connectedBlock in connectedBlocks)
            {
                string connectedSubtype = ((IMyCubeBlock)connectedBlock).BlockDefinition.SubtypeName;
                if (connectedSubtype == StopAt)
                    StopHits++;
            
                if (connectedBlock != prevScan && connectedSubtype != StopAt)
                {
                    Example_ScanArm(connectedBlock, blockEntity, StopAt);
                }
            }
            
            return StopHits == 2;
        }
        // TODO: MyVisualScriptLogicProvider.AddQuestlogDetail for debug mode

        private void UpdatePower(int PhysicalAssemblyId)
        {
            IMyReactor basePart = (IMyReactor) ModularAPI.GetBasePart(PhysicalAssemblyId);

            float desiredPower = Example_ValidArms[PhysicalAssemblyId].Count * GetNumBlocksInArm(PhysicalAssemblyId);

            basePart.PowerOutputMultiplier = desiredPower / (basePart.MaxOutput / basePart.PowerOutputMultiplier);
            MyAPIGateway.Utilities.ShowMessage("MW", "" + basePart.PowerOutputMultiplier);
        }

        // This is the important bit.
        PhysicalDefinition ModularDefinitionExample => new PhysicalDefinition
        {
            Name = "ModularDefinitionExample",

            OnPartAdd = (int PhysicalAssemblyId, MyEntity NewBlockEntity, bool IsBaseBlock) =>
            {
                if (!Example_ValidArms.ContainsKey(PhysicalAssemblyId))
                    Example_ValidArms.Add(PhysicalAssemblyId, new List<MyEntity[]>());

                // Scan for 'arms' connected on both ends to the feeder block.
                switch (((IMyCubeBlock)NewBlockEntity).BlockDefinition.SubtypeName)
                {
                    case "Caster_Accelerator_0":
                    case "Caster_Accelerator_90":
                        MyEntity basePart = ModularAPI.GetBasePart(PhysicalAssemblyId);
                        if (Example_ScanArm(NewBlockEntity, null, "Caster_Feeder"))
                            Example_ValidArms[PhysicalAssemblyId].Add(Example_BufferArm.ToArray());
                    
                        Example_BufferArm.Clear();
                        StopHits = 0;
                        break;
                }

                UpdatePower(PhysicalAssemblyId);

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

                        UpdatePower(PhysicalAssemblyId);
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

            BaseBlock = "Caster_Controller",

            AllowedBlocks = new string[]
            {
                "Caster_FocusLens",
                "Caster_Accelerator_0",
                "Caster_Accelerator_90",
                "Caster_CentralPipe_0",
                "Caster_CentralPipe_90",
                "Caster_CentralPipe_T",
                "Caster_Feeder",
                "Caster_Controller",
            },

            // Allowed connection directions & whitelists, measured in blocks.
            // If an allowed SubtypeId is not included here, connections are allowed on all sides.
            // If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            AllowedConnections = new Dictionary<string, Dictionary<Vector3I, string[]>>
            {
                {
                    // Note - Offsets line up with BuildInfo block orientation.
                    // Note - Offsets are measured from the center of the block; in this case, the Caster_FocusLens is a 3x3 that has connections on the back in a plus shape.
                    "Caster_FocusLens", new Dictionary<Vector3I, string[]>
                    {
                        { new Vector3I(1, 0, 2), new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                        }},
                        { new Vector3I(-1, 0, 2), new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                        }},
                        { new Vector3I(0, 1, 2), new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                        }},
                        { new Vector3I(0, -1, 2), new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                        }},
                    }
                },
                {
                    "Caster_Accelerator_0", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Forward, new string[] {
                            "Caster_Accelerator_0",
                            "Caster_Accelerator_90",
                            "Caster_Feeder",
                        }},
                        { Vector3I.Backward, new string[] {
                            "Caster_Accelerator_0",
                            "Caster_Accelerator_90",
                            "Caster_Feeder",
                        }},
                    }
                },
                {
                    "Caster_Accelerator_90", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Forward, new string[] {
                            "Caster_Accelerator_0",
                            "Caster_Accelerator_90",
                            "Caster_Feeder",
                        }},
                        { Vector3I.Right, new string[] {
                            "Caster_Accelerator_0",
                            "Caster_Accelerator_90",
                            "Caster_Feeder",
                        }},
                    }
                },
                {
                    "Caster_CentralPipe_0", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Forward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                        { Vector3I.Backward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                    }
                },
                {
                    "Caster_CentralPipe_90", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Forward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                        { Vector3I.Right, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                    }
                },
                {
                    "Caster_CentralPipe_T", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Forward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                        { Vector3I.Right, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                        { Vector3I.Backward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                    }
                },
                {
                    "Caster_Feeder", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Forward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},
                        { Vector3I.Backward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                            "Caster_Controller",
                            "Caster_FocusLens",
                        }},

                        { Vector3I.Up, new string[] {
                            "Caster_Accelerator_0",
                            "Caster_Accelerator_90",
                            "Caster_Feeder",
                        }},
                        { Vector3I.Down, new string[] {
                            "Caster_Accelerator_0",
                            "Caster_Accelerator_90",
                            "Caster_Feeder",
                        }},
                    }
                },
                {
                    "Caster_Controller", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Backward, new string[] {
                            "Caster_CentralPipe_0",
                            "Caster_CentralPipe_90",
                            "Caster_CentralPipe_T",
                            "Caster_Feeder",
                        }},
                    }
                },
            },
        };
    }
}
