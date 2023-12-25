using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace Scripts.ModularWeapons
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    internal class ModularDefinitionSender : MySessionComponentBase
    {
        public static ModularDefinitionSender Instance;
        const int DefinitionMessageId = 8772;
        const int ReadyMessageId = 8771;

        internal byte[] Storage;

        public override void LoadData()
        {
            Instance = this;
            MyLog.Default.WriteLine("Init new ModularWeaponsDefinition");
            MyAPIGateway.Utilities.RegisterMessageHandler(ReadyMessageId, ReadyHandler);

            // Init
            Storage = MyAPIGateway.Utilities.SerializeToBinary(ModularDefinition.GetBaseDefinitions());
            MyLog.Default.WriteLine($"ModularWeapons: Packaged definitions & going to sleep.");
        }

        protected override void UnloadData()
        {
            Instance = null;
        }

        private void ReadyHandler(object message)
        {
            if (message is bool && (bool)message)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, Storage);
                MyLog.Default.WriteLine("ModularWeapons: Sent definitions & returning to sleep.");
            }
            else
                MyLog.Default.WriteLine("Ready recieved not a bool!");
        }
    }
}
