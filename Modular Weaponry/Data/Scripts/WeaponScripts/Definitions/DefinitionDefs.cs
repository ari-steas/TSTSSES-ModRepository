using ProtoBuf;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions
{
    public class DefinitionDefs
    {
        [ProtoContract]
        public class DefinitionContainer
        {
            [ProtoMember(1)] internal PhysicalDefinition[] PhysicalDefs;
        }

        [ProtoContract]
        public class PhysicalDefinition
        {
            [ProtoMember(1)] public string Name { get; set; }
            [ProtoMember(2)] public string[] AllowedBlocks { get; set; }
            [ProtoMember(3)] public Dictionary<string, Vector3I[]> AllowedConnections { get; set; }
            [ProtoMember(4)] public string BaseBlock { get; set; }
        }

        [ProtoContract]
        public class FunctionCall
        {
            public string DefinitionName { get; set; }
            public int PhysicalWeaponId { get; set; }
            public ActionType ActionId { get; set; }
            public object[] values { get; set; }

            public enum ActionType
            {
                OnShoot,
                OnPartPlace,
                OnPartRemove,
            }
        }
    }
}
