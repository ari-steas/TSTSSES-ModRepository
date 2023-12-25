
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace IHATEKEEN.Scripts.ModularWeapons
{
    // turns out whoever wrote the CoreSystems definition handler is REALLY SMART. hats off to you
    partial class ModularDefinition
    {
        internal ModularDefinition()
        {
            // it's just like weaponcore, insert definitions here

            LoadDefinitions(ModularDefinitionEx);
        }
    }
}
