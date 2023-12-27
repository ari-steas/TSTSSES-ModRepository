using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions
{
    internal class ApiDefinitions
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal ApiDefinitions()
        {
            ModApiMethods = new Dictionary<string, Delegate>()
            {
                ["GetAllParts"] = new Func<MyEntity[]>(GetAllParts),
                ["GetAllWeapons"] = new Func<int[]>(GetAllWeapons),
                ["GetMemberParts"] = new Func<int, MyEntity[]>(GetMemberParts),
                ["GetConnectedBlocks"] = new Func<MyEntity, MyEntity[]>(GetConnectedBlocks),
                ["GetBasePart"] = new Func<int, MyEntity>(GetBasePart),
            };
        }

        private MyEntity[] GetAllParts()
        {
            List<MyEntity> parts = new List<MyEntity>();
            foreach (var block in WeaponPartManager.Instance.AllWeaponParts.Keys)
                if (block.FatBlock != null)
                    parts.Add((MyEntity)block.FatBlock);
            return parts.ToArray();
        }

        private int[] GetAllWeapons()
        {
            return WeaponPartManager.Instance.AllPhysicalWeapons.Keys.ToArray();
        }

        private MyEntity[] GetMemberParts(int weaponId)
        {
            PhysicalWeapon wep;
            if (!WeaponPartManager.Instance.AllPhysicalWeapons.TryGetValue(weaponId, out wep))
                return new MyEntity[0];

            List<MyEntity> parts = new List<MyEntity>();
            foreach (var part in wep.componentParts)
                if (part.block.FatBlock != null)
                    parts.Add((MyEntity)part.block.FatBlock);

            MyLog.Default.WriteLine("ModularWeaponry: PASS MEMPARTS");
            return parts.ToArray();
        }

        private MyEntity[] GetConnectedBlocks(MyEntity blockEntity)
        {
            if (!(blockEntity is IMyCubeBlock))
                return new MyEntity[0];

            WeaponPart wep;
            if (!WeaponPartManager.Instance.AllWeaponParts.TryGetValue(((IMyCubeBlock)blockEntity).SlimBlock, out wep) || wep.connectedParts == null)
                return new MyEntity[0];

            List<MyEntity> parts = new List<MyEntity>();
            foreach (var part in wep.connectedParts)
                if (part.block.FatBlock != null)
                    parts.Add((MyEntity)part.block.FatBlock);

            MyLog.Default.WriteLine("ModularWeaponry: PASS CONPARTS");
            return parts.ToArray();
        }

        private MyEntity GetBasePart(int weaponId)
        {
            PhysicalWeapon wep;
            if (!WeaponPartManager.Instance.AllPhysicalWeapons.TryGetValue(weaponId, out wep))
                return null;

            MyLog.Default.WriteLine("ModularWeaponry: PASS BASEPART");
            return (MyEntity) wep.basePart.block.FatBlock;
        }
    }
}
