using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;
using static Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions.DefinitionDefs;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    internal class DefinitionHandler : MySessionComponentBase
    {
        public static DefinitionHandler Instance;
        const int DefinitionMessageId = 8772;
        const int OutboundMessageId = 8771;
        const int InboundMessageId = 8773;

        public List<ModularDefinition> ModularDefinitions = new List<ModularDefinition>();

        public override void LoadData()
        {
            MyLog.Default.WriteLine("ModularWeapons: Init DefinitionHandler.cs");
            Instance = this;
            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, MessageHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, MessageHandler);
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, true);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, MessageHandler);
            Instance = null;
        }

        public void MessageHandler(object o)
        {
            try
            {
                var message = o as byte[];
                if (message == null) return;

                DefinitionContainer baseDefArray = null;
                try
                {
                    baseDefArray = MyAPIGateway.Utilities.SerializeFromBinary<DefinitionContainer>(message);
                }
                catch (Exception e)
                {
                }

                if (baseDefArray != null)
                {
                    MyLog.Default.WriteLine($"ModularWeapons: Recieved {baseDefArray.PhysicalDefs.Length} definitions.");
                    foreach (var def in baseDefArray.PhysicalDefs)
                    {
                        ModularDefinition modDef = ModularDefinition.Load(def);
                        if (modDef != null)
                            ModularDefinitions.Add(modDef);
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"ModularWeapons: baseDefArray null!");
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLine($"ModularWeapons: Exception in Handler: {ex}"); }
        }
    }
}
