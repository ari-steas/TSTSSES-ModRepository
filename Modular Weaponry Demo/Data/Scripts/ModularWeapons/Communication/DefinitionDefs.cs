using ProtoBuf;

namespace Scripts.ModularWeapons
{
    public class DefinitionDefs
    {

        [ProtoContract]
        public class PhysicalDefinition
        {
            [ProtoMember(1)] public string Name { get; set; }
        }
    }
}
