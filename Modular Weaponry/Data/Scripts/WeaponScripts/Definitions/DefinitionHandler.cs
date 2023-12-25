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
        const int ReadyMessageId = 8771;

        public override void LoadData()
        {
            MyLog.Default.WriteLine("Init DefinitionHandler.cs");
            Instance = this;
            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, MessageHandler);
            MyAPIGateway.Utilities.SendModMessage(ReadyMessageId, true);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Instance = null;
        }

        public void MessageHandler(object o)
        {
            try
            {
                var message = o as byte[];
                if (message == null) return;

                PhysicalDefinition baseDefArray = null;
                try
                {
                    baseDefArray = MyAPIGateway.Utilities.SerializeFromBinary<PhysicalDefinition>(message);
                }
                catch (Exception e)
                {
                }

                if (baseDefArray != null)
                {
                    MyLog.Default.WriteLine($"Recieved {baseDefArray.Name}");
                }
                else
                {
                    MyLog.Default.WriteLine($"baseDefArray null!");
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLine($"Exception in Handler: {ex}"); }
        }
    }
}
