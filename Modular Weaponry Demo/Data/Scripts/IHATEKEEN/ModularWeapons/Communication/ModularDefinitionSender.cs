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
        const int InboundMessageId = 8771;
        const int OutboundMessageId = 8773;

        internal DefinitionContainer storedDef;
        internal byte[] Storage;

        public override void LoadData()
        {
            MyLog.Default.WriteLine("ModularWeaponsDefinition: Init new ModularWeaponsDefinition");
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, InputHandler);

            // Init
            storedDef = ModularDefinition.GetBaseDefinitions();
            Storage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);

            MyLog.Default.WriteLine($"ModularWeaponsDefinition: Packaged definitions & going to sleep.");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(InboundMessageId, InputHandler);
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

                    if (call == null)
                    {
                        MyLog.Default.WriteLine($"ModularWeaponsDefinition: Invalid FunctionCall!");
                        return;
                    }

                    PhysicalDefinition defToCall = null;
                    foreach (var definition in storedDef.PhysicalDefs)
                        if (call.DefinitionName == definition.Name)
                            defToCall = definition;

                    if (defToCall == null)
                    {
                        MyLog.Default.WriteLine($"ModularWeaponsDefinition: Function call [{call.DefinitionName}] not addressed to this.");
                        return;
                    }

                    object[] Values = call.Values.Values();

                    switch (call.ActionId)
                    {
                        case FunctionCall.ActionType.OnShoot:
                            SendOnShoot(call.DefinitionName, call.PhysicalWeaponId, call.Values.ulongValues[0], defToCall.OnShoot(call.PhysicalWeaponId, call.Values.intValues[0], call.Values.ulongValues[0], call.Values.longValues[0], call.Values.vectorValues[0]));
                            break;
                        case FunctionCall.ActionType.OnPartPlace:
                            // TODO: OnPartUpdate? With ConnectedParts?
                            defToCall.OnPartPlace(call.PhysicalWeaponId, (long)Values[0], (bool)Values[1]);
                            break;
                        case FunctionCall.ActionType.OnPartRemove:
                            defToCall.OnPartPlace(call.PhysicalWeaponId, (long)Values[0], (bool)Values[1]);
                            break;
                        case FunctionCall.ActionType.OnPartDestroy:
                            defToCall.OnPartDestroy(call.PhysicalWeaponId, (long)Values[0], (bool)Values[1]);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine($"ModularWeaponsDefinition: Exception in InputHandler: {ex}");
                }
            }
        }


        private void SendOnShoot(string definitionName, int physicalWeaponId, ulong projectileId, VRage.MyTuple<bool, Vector3D, Vector3D, float> returnData)
        {
            SendFunc(new FunctionCall()
            {
                DefinitionName = definitionName,
                PhysicalWeaponId = physicalWeaponId,
                ActionId = FunctionCall.ActionType.OnShoot,
                Values = new SerializedObjectArray(projectileId, returnData),
            });
        }

        private void SendFunc(FunctionCall call)
        {
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            MyLog.Default.WriteLine($"ModularWeaponsDefinition: Sending function call [id {call.ActionId}].");
        }
    }
}
