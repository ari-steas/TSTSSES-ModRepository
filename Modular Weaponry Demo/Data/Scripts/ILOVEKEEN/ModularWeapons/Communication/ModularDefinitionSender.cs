using Sandbox.ModAPI;
using ILOVEKEEN.Scripts.ModularWeaponry;
using VRage.Game.Components;
using VRage.Utils;
using static Scripts.ILOVEKEEN.ModularWeaponry.Communication.DefinitionDefs;
using System;
using VRageMath;
using CoreParts.Data.Scripts.ILOVEKEEN.ModularWeaponry.Communication;
using CoreSystems.Api;

namespace Scripts.ILOVEKEEN.ModularWeaponry.Communication
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
            MyLog.Default.WriteLine("ModularWeaponryDefinition: Init new ModularWeaponryDefinition");
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, InputHandler);

            // Init
            storedDef = ModularDefinition.GetBaseDefinitions();
            Storage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);

            ModularDefinition.ModularAPI = new ModularDefinitionAPI();
            ModularDefinition.ModularAPI.LoadData();

            ModularDefinition.WcAPI = new WcApi();
            ModularDefinition.WcAPI.Load();

            MyLog.Default.WriteLine($"ModularWeaponryDefinition: Packaged definitions & going to sleep.");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(InboundMessageId, InputHandler);
            Array.Clear(Storage, 0, Storage.Length);
            Storage = null;
            ModularDefinition.ModularAPI.UnloadData();
            ModularDefinition.WcAPI.Unload();
        }

        private void InputHandler(object o)
        {
            var message = o as byte[];

            if (o is bool && (bool)o)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, Storage);
                MyLog.Default.WriteLine("ModularWeaponryDefinition: Sent definitions & returning to sleep.");
            }
            else
            {
                try
                {
                    FunctionCall call;
                    call = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);

                    if (call == null)
                    {
                        MyLog.Default.WriteLine($"ModularWeaponryDefinition: Invalid FunctionCall!");
                        return;
                    }

                    PhysicalDefinition defToCall = null;
                    foreach (var definition in storedDef.PhysicalDefs)
                        if (call.DefinitionName == definition.Name)
                            defToCall = definition;

                    if (defToCall == null)
                    {
                        //MyLog.Default.WriteLine($"ModularWeaponryDefinition: Function call [{call.DefinitionName}] not addressed to this.");
                        return;
                    }

                    // TODO: Remove
                    //object[] Values = call.Values.Values();

                    switch (call.ActionId)
                    {
                        case FunctionCall.ActionType.OnShoot:
                            SendOnShoot(call.DefinitionName, call.PhysicalWeaponId, call.Values.ulongValues[0], defToCall.OnShoot(call.PhysicalWeaponId, call.Values.longValues[0], call.Values.intValues[0], call.Values.ulongValues[0], call.Values.longValues[1], call.Values.vectorValues[0]));
                            break;
                        case FunctionCall.ActionType.OnPartAdd:
                            // TODO: OnPartUpdate? With ConnectedParts?
                            defToCall.OnPartAdd(call.PhysicalWeaponId, call.Values.longValues[0], call.Values.boolValues[0]);
                            break;
                        case FunctionCall.ActionType.OnPartRemove:
                            defToCall.OnPartRemove(call.PhysicalWeaponId, call.Values.longValues[0], call.Values.boolValues[0]);
                            break;
                        case FunctionCall.ActionType.OnPartDestroy:
                            defToCall.OnPartDestroy(call.PhysicalWeaponId, call.Values.longValues[0], call.Values.boolValues[0]);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine($"ModularWeaponryDefinition: Exception in InputHandler: {ex}");
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
            //MyLog.Default.WriteLine($"ModularWeaponryDefinition: Sending function call [id {call.ActionId}].");
        }
    }
}
