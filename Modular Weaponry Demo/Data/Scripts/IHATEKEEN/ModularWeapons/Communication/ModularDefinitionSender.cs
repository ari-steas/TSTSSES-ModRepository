using Sandbox.ModAPI;
using IHATEKEEN.Scripts.ModularWeapons;
using VRage.Game.Components;
using VRage.Utils;
using static Scripts.IHATEKEEN.ModularWeapons.Communication.DefinitionDefs;
using System;

namespace Scripts.IHATEKEEN.ModularWeapons.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    internal class ModularDefinitionSender : MySessionComponentBase
    {
        const int DefinitionMessageId = 8772;
        const int ReadyMessageId = 8771;

        internal DefinitionContainer storedDef;
        internal byte[] Storage;

        public override void LoadData()
        {
            MyLog.Default.WriteLine("ModularWeaponsDefinition: Init new ModularWeaponsDefinition");
            MyAPIGateway.Utilities.RegisterMessageHandler(ReadyMessageId, ReadyHandler);

            // Init
            storedDef = ModularDefinition.GetBaseDefinitions();
            Storage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);
            MyLog.Default.WriteLine($"ModularWeaponsDefinition: Packaged definitions & going to sleep.");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ReadyMessageId, ReadyHandler);
            Array.Clear(Storage, 0, Storage.Length);
            Storage = null;
        }

        private void ReadyHandler(object message)
        {
            if (message is bool && (bool)message)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, Storage);
                MyLog.Default.WriteLine("ModularWeaponsDefinition: Sent definitions & returning to sleep.");
                foreach (var def in storedDef.PhysicalDefs)
                    def.PrintName();
            }
            else
                MyLog.Default.WriteLine("ModularWeaponsDefinition: Ready recieved not a bool!");
        }
    }
}
