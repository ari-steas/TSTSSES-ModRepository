using ProtoBuf;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Scripts.IHATEKEEN.ModularWeapons.Communication
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
            public Action PrintName { get; set; }

            [ProtoMember(2)] public string[] AllowedBlocks { get; set; }
            [ProtoMember(3)] public Dictionary<string, Vector3I[]> AllowedConnections { get; set; }
            [ProtoMember(4)] public string BaseBlock { get; set; }
        }
    }
}
