using CoreParts.Data.Scripts.IHATEKEEN.ModularWeapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Utils;
using VRageMath;
using static Scripts.IHATEKEEN.ModularWeapons.Communication.DefinitionDefs;

namespace IHATEKEEN.Scripts.ModularWeapons
{
    partial class ModularDefinition
    {
        PhysicalDefinition ModularDefinitionEx => new PhysicalDefinition
        {
            Name = "ModularDefinitionEx",

            OnPartPlace = (int PhysicalWeaponId, long BlockEntityId, bool IsBaseBlock) =>
            {
                MyLog.Default.WriteLine($"ModularDefinitionEx: OnPartPlace {IsBaseBlock}");
            },

            OnPartRemove = (int PhysicalWeaponId, long BlockEntityId, bool IsBaseBlock) =>
            {
                MyLog.Default.WriteLine($"ModularDefinitionEx: OnPartRemove {IsBaseBlock}");
            },

            OnPartDestroy = (int PhysicalWeaponId, long BlockEntityId, bool IsBaseBlock) =>
            {
                MyLog.Default.WriteLine($"ModularDefinitionEx: OnPartDestroy {IsBaseBlock}");
            },

            OnShoot = (int PhysicalWeaponId, long FirerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition) => {
                return new MyTuple<bool, Vector3D, Vector3D, float>(false, projectilePosition, OffsetProjectileVelocity(1, projectileId, FirerEntityId), 0);
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
