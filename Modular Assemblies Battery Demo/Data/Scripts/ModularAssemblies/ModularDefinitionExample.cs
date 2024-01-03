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
using SpaceEngineers.Game.Entities.Blocks;
using Sandbox.Game.Entities;

namespace Scripts.ModularAssemblies.Communication
{

    partial class ModularDefinition
    {
        // All variables and functions outside of a PhysicalDefinition are shared between ModularDefiniton files within the same mod.
        // It's good practice to make these variables and functions easily identifiable.
        private Dictionary<int, int> Basic_ChargerCount = new Dictionary<int, int>();

        // Updates power output for a given PhysicalAssemblyId.
        private void Basic_UpdateOutput(int PhysicalAssemblyId)
        {
            // Get the basePart.
            IMyReactor basePart = (IMyReactor) ModularAPI.GetBasePart(PhysicalAssemblyId);
            
            // Re-enable reactor.
            if (basePart.PowerOutputMultiplier == 0)
                basePart.Enabled = true;

            // Reactor power output multipliers are funny. Increases the power output of the base reactor by 10 * the number of boosters.
            float desiredPower = 15 + Basic_ChargerCount[PhysicalAssemblyId] * 10;
            basePart.PowerOutputMultiplier = (desiredPower * basePart.PowerOutputMultiplier) / basePart.MaxOutput;

            // Disable reactor so that fuel isn't used unnecessarily.
            if (basePart.PowerOutputMultiplier == 0)
                basePart.Enabled = false;
        }

        // This is the important bit.
        PhysicalDefinition ModularDefinition_BasicDemo => new PhysicalDefinition
        {
            // Unique name of the definition.
            Name = "Basic Reactor Demo",

            // Triggers whenever a new part is added to an assembly.
            OnPartAdd = (int PhysicalAssemblyId, MyEntity NewBlockEntity, bool IsBaseBlock) =>
            {
                if (IsBaseBlock)
                    Basic_ChargerCount.Add(PhysicalAssemblyId, 0);
                else
                    Basic_ChargerCount[PhysicalAssemblyId]++;

                Basic_UpdateOutput(PhysicalAssemblyId);

                if (ModularAPI.IsDebug())
                    MyAPIGateway.Utilities.ShowNotification("Count: " + Basic_ChargerCount[PhysicalAssemblyId] + " | OutputMultiplier: " + ((IMyReactor)ModularAPI.GetBasePart(PhysicalAssemblyId)).PowerOutputMultiplier);
            },

            // Triggers whenever a part is removed from an assembly.
            OnPartRemove = (int PhysicalAssemblyId, MyEntity BlockEntity, bool IsBaseBlock) =>
            {
                // Remove if the connection is broken.
                if (!IsBaseBlock)
                {
                    Basic_ChargerCount[PhysicalAssemblyId]--;
                    Basic_UpdateOutput(PhysicalAssemblyId);
                }
                else
                {
                    Basic_ChargerCount.Remove(PhysicalAssemblyId);
                }
            },

            // Triggers whenever a part is destroyed, simultaneously with OnPartRemove
            OnPartDestroy =
            {
                // You can remove this function, and any others if need be.
            },

            // The most important block in an assembly. Connection checking starts here.
            BaseBlock = "LargeBlockSmallGenerator",

            // All SubtypeIds that can be part of this assembly.
            AllowedBlocks = new string[]
            {
                "Reactor_Booster",
                "LargeBlockSmallGenerator",
            },

            // Allowed connection directions & whitelists, measured in blocks.
            // If an allowed SubtypeId is not included here, connections are allowed on all sides.
            // If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            AllowedConnections = new Dictionary<string, Dictionary<Vector3I, string[]>>
            {
                {
                    // Note - Offsets line up with BuildInfo block orientation, and are measured from the center of the block.
                    "Reactor_Booster", new Dictionary<Vector3I, string[]>
                    {
                        { Vector3I.Backward, new string[0] }, // Only connect to AllowedBlocks backwards
                    }
                },
            },
        };
    }
}
