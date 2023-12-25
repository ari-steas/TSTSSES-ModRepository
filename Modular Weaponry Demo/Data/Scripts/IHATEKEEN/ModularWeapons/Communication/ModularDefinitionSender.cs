using Sandbox.ModAPI;
using IHATEKEEN.Scripts.ModularWeapons;
using VRage.Game.Components;
using VRage.Utils;
using static Scripts.IHATEKEEN.ModularWeapons.Communication.DefinitionDefs;
using System;
using VRageMath;

namespace Scripts.IHATEKEEN.ModularWeapons.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation)]
    internal class ModularDefinitionSender : MySessionComponentBase
    {
        const int DefinitionMessageId = 8772;
        const int ReadyMessageId = 8771;
        const int OutboundMessageId = 8773;

        internal DefinitionContainer storedDef;
        internal byte[] Storage;

        public override void LoadData()
        {
            MyLog.Default.WriteLine("ModularWeaponsDefinition: Init new ModularWeaponsDefinition");
            MyAPIGateway.Utilities.RegisterMessageHandler(ReadyMessageId, InputHandler);

            // Init
            storedDef = ModularDefinition.GetBaseDefinitions();
            Storage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);

            MyLog.Default.WriteLine($"ModularWeaponsDefinition: Packaged definitions & going to sleep.");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ReadyMessageId, InputHandler);
            Array.Clear(Storage, 0, Storage.Length);
            Storage = null;
        }

        private void InputHandler(object o)
        {
            var message = o as byte[];

            if (o is bool && (bool)o)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, Storage);
                MyLog.Default.WriteLine("ModularWeaponsDefinition: Sent definitions & returning to sleep.");
            }
            else
            {
                try
                {
                    FunctionCall call;
                    call = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);

                    PhysicalDefinition defToCall = null;
                    foreach (var definition in storedDef.PhysicalDefs)
                        if (call.DefinitionName == definition.Name)
                            defToCall = definition;
                    if (defToCall == null)
                    {
                        MyLog.Default.WriteLine($"ModularWeaponsDefinition: Function call not addressed to this.");
                        return;
                    }

                    switch (call.ActionId)
                    {
                        case FunctionCall.ActionType.OnShoot:
                            SendOnShoot(call.DefinitionName, call.PhysicalWeaponId, defToCall.OnShoot(call.PhysicalWeaponId, (int)call.values[1], (ulong)call.values[2], (long)call.values[3], (Vector3D)call.values[4]));
                            break;
                        case FunctionCall.ActionType.OnPartPlace:
                            defToCall.OnPartPlace(call.PhysicalWeaponId, (long)call.values[0]);
                            break;
                        case FunctionCall.ActionType.OnPartRemove:
                            defToCall.OnPartPlace(call.PhysicalWeaponId, (long)call.values[0]);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine($"ModularWeaponsDefinition: Exception in Handler: {ex}");
                }
            }
        }


        // TODO: Invoke SendOnShoot whenever original function is called
        private void SendOnShoot(string definitionName, int physicalWeaponId, VRage.MyTuple<bool, Vector3D, Vector3D, float> returnData)
        {
            SendFunc(new FunctionCall()
            {
                DefinitionName = definitionName,
                PhysicalWeaponId = physicalWeaponId,
                ActionId = FunctionCall.ActionType.OnShoot,
                values = new object[] { returnData },
            });
        }

        private void SendFunc(FunctionCall call)
        {
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            MyLog.Default.WriteLine($"ModularWeaponsDefinition: Sending function call [id {call.ActionId}].");
        }
    }
}
