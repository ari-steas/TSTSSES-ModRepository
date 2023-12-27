using CoreParts.Data.Scripts.ILOVEKEEN.ModularWeaponry.Communication;
using System.Collections.Generic;
using VRage;
using VRage.Utils;
using VRageMath;
using static Scripts.ILOVEKEEN.ModularWeaponry.Communication.DefinitionDefs;

namespace ILOVEKEEN.Scripts.ModularWeaponry
{
    /* Hey modders!
     * 
     * This is a bit of a mess, so please bear with me. Ping [@aristeas.] on discord if you have any questions, comments, or concers.
     * https://discord.com/invite/kssCqSmbYZ
     * 
     * This mod behaves kind of like Weaponcore. Kind of. Definitions are declared in a similar manner, with an example below.
     * What makes this mod unique (other than the modular stuff) is that you're supposed to run code in it. It will not function well otherwise.
     * 
     * I tried my best to document stuff, most of it will be in DefinitionAPI.cs
     *   - You can also hover over variables in most IDEs for a description.
     * You have access to ModularAPI and WcAPI; ModularAPI handles stuff like GetMemberParts for modular weapons, whereas WcAPI is your bog-standard Weaponcore ModAPI.
     * If you need logic to run in a MySessionComponent, you can init the ModularAPI via LoadData() and UnloadData().
     * 
     * As for file structure, DON'T TOUCH ANYTHING IN Scripts.ILOVEKEEN.ModularWeapons OTHER THAN DEFINITIONS. It is all important.
     * 
     * Good luck, and happy modularizing!
     */

    partial class ModularDefinition
    {
        PhysicalDefinition ModularDefinitionEx => new PhysicalDefinition
        {
            Name = "ModularDefinitionEx",

            OnPartAdd = (int PhysicalWeaponId, long BlockEntityId, bool IsBaseBlock) =>
            {
                MyLog.Default.WriteLine($"ModularDefinitionEx: OnPartAdd {IsBaseBlock}.");
                MyLog.Default.WriteLine($"\nPartCount: {ModularAPI.GetAllParts().Length}\nWeaponCount: {ModularAPI.GetAllWeapons().Length}\nThisPartCount: {ModularAPI.GetMemberParts(PhysicalWeaponId).Length}\nConnectedBlocks: {ModularAPI.GetConnectedBlocks(ModularAPI.GetBasePart(PhysicalWeaponId), true).Length}");
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
