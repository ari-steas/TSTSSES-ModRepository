using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions
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
