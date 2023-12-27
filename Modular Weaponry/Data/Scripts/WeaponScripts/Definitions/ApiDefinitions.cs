using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions
{
    internal class ApiDefinitions
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal ApiDefinitions()
        {
            ModApiMethods = new Dictionary<string, Delegate>()
            {
                ["GetAllParts"] = new Func<long[]>(GetAllParts),
                ["GetAllWeapons"] = new Func<int[]>(GetAllWeapons),
                ["GetMemberParts"] = new Func<int, long[]>(GetMemberParts),
                ["GetConnectedBlocks"] = new Func<long, long[]>(GetConnectedBlocks),
                ["GetBasePart"] = new Func<int, long>(GetBasePart),
            };
        }

        private long[] GetAllParts()
        {
            List<long> parts = new List<long>();
            foreach (var block in WeaponPartGetter.Instance.AllWeaponParts.Keys)
                if (block.FatBlock != null)
                    parts.Add(block.FatBlock.EntityId);
            return parts.ToArray();
        }

        private int[] GetAllWeapons()
        {
            return WeaponPartGetter.Instance.AllPhysicalWeapons.Keys.ToArray();
        }

        private long[] GetMemberParts(int weaponId)
        {
            PhysicalWeapon wep;
            if (!WeaponPartGetter.Instance.AllPhysicalWeapons.TryGetValue(weaponId, out wep))
                return new long[0];

            List<long> parts = new List<long>();
            foreach (var part in wep.componentParts)
                if (part.block.FatBlock != null)
                    parts.Add(part.block.FatBlock.EntityId);
            return parts.ToArray();
        }

        private long[] GetConnectedBlocks(long blockId)
        {
            IMyEntity entity;
            if (!MyAPIGateway.Entities.TryGetEntityById(blockId, out entity) || !(entity is IMyCubeBlock))
                return new long[0];

            WeaponPart wep;
            if (!WeaponPartGetter.Instance.AllWeaponParts.TryGetValue(((IMyCubeBlock)entity).SlimBlock, out wep) || wep.connectedParts == null)
                return new long[0];

            List<long> parts = new List<long>();
            foreach (var part in wep.connectedParts)
                if (part.block.FatBlock != null)
                    parts.Add(part.block.FatBlock.EntityId);
            return parts.ToArray();
        }

        private long GetBasePart(int weaponId)
        {
            PhysicalWeapon wep;
            if (!WeaponPartGetter.Instance.AllPhysicalWeapons.TryGetValue(weaponId, out wep))
                return -1;

            return wep.basePart.block.FatBlock.EntityId;
        }
    }
}
