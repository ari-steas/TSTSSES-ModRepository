using static Scripts.ModularWeapons.DefinitionDefs;

namespace Scripts.ModularWeapons
{
    partial class ModularDefinition
    {
        internal DefinitionContainer Container = new DefinitionContainer();
        internal void LoadDefinitions(params PhysicalDefinition[] defs)
        {
            Container.physicalDefs = defs;
        }

        /// <summary>
        /// Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static DefinitionContainer GetBaseDefinitions()
        {
            return new ModularDefinition().Container;
        }

        internal class DefinitionContainer
        {
            public PhysicalDefinition[] physicalDefs;
        }
    }
}
