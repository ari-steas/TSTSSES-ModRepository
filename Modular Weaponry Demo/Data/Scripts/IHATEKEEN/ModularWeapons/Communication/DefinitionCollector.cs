using static Scripts.IHATEKEEN.ModularWeapons.Communication.DefinitionDefs;

namespace IHATEKEEN.Scripts.ModularWeapons
{
    partial class ModularDefinition
    {
        internal DefinitionContainer Container = new DefinitionContainer();
        internal void LoadDefinitions(params PhysicalDefinition[] defs)
        {
            Container.PhysicalDefs = defs;
        }

        /// <summary>
        /// Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static DefinitionContainer GetBaseDefinitions()
        {
            return new ModularDefinition().Container;
        }
    }
}
