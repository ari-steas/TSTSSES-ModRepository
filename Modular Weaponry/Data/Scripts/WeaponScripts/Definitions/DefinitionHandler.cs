using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using static Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions.DefinitionDefs;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    internal class DefinitionHandler : MySessionComponentBase
    {
        public static DefinitionHandler Instance;
        const int DefinitionMessageId = 8772;
        const int InboundMessageId = 8773;
        const int OutboundMessageId = 8771;

        public List<ModularDefinition> ModularDefinitions = new List<ModularDefinition>();

        public override void LoadData()
        {
            MyLog.Default.WriteLine("ModularWeapons: Init DefinitionHandler.cs");
            Instance = this;
            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, ActionMessageHandler);
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, true);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.UnregisterMessageHandler(InboundMessageId, ActionMessageHandler);
            Instance = null;
        }

        public void DefMessageHandler(object o)
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
            catch (Exception ex) { MyLog.Default.WriteLine($"ModularWeapons: Exception in DefinitionMessageHandler: {ex}"); }
        }

        public void ActionMessageHandler(object o)
        {
            try
            {
                var message = o as byte[];
                if (message == null) return;

                FunctionCall functionCall = null;
                try
                {
                    functionCall = MyAPIGateway.Utilities.SerializeFromBinary<FunctionCall>(message);
                }
                catch (Exception e) { }

                if (functionCall != null)
                {
                    //MyLog.Default.WriteLine($"ModularWeapons: Recieved action of type {functionCall.ActionId}.");

                    PhysicalWeapon wep = WeaponPartManager.Instance.AllPhysicalWeapons[functionCall.PhysicalWeaponId];
                    if (wep == null)
                    {
                        MyLog.Default.WriteLine($"ModularWeapons: Invalid PhysicalWeapon!");
                        return;
                    }

                    // TODO: Remove
                    //object[] Values = functionCall.Values.Values();

                    switch (functionCall.ActionId)
                    {
                        case FunctionCall.ActionType.OnShoot:
                            wep.UpdateProjectile(functionCall.Values.ulongValues[0], functionCall.Values.projectileValues[0]);
                            break;
                    }
                }
                else
                {
                    MyLog.Default.WriteLine($"ModularWeapons: functionCall null!");
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLine($"ModularWeapons: Exception in ActionMessageHandler: {ex}"); }
        }


        public void SendOnShoot(string DefinitionName, int PhysicalWeaponId, long FirerEntityId, int firerPartId, ulong projectileId, long targetEntityId, Vector3D projectilePosition)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                FirerEntityId,
                firerPartId,
                projectileId,
                targetEntityId,
                projectilePosition
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnShoot,
                DefinitionName = DefinitionName,
                PhysicalWeaponId = PhysicalWeaponId,
                Values = Values,
            });
        }

        public void SendOnPartAdd(string DefinitionName, int PhysicalWeaponId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartAdd,
                DefinitionName = DefinitionName,
                PhysicalWeaponId = PhysicalWeaponId,
                Values = Values,
            });
        }

        public void SendOnPartRemove(string DefinitionName, int PhysicalWeaponId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartRemove,
                DefinitionName = DefinitionName,
                PhysicalWeaponId = PhysicalWeaponId,
                Values = Values,
            });
        }

        public void SendOnPartDestroy(string DefinitionName, int PhysicalWeaponId, long BlockEntityId, bool IsBaseBlock)
        {
            SerializedObjectArray Values = new SerializedObjectArray
            (
                BlockEntityId,
                IsBaseBlock
            );

            SendFunc(new FunctionCall()
            {
                ActionId = FunctionCall.ActionType.OnPartDestroy,
                DefinitionName = DefinitionName,
                PhysicalWeaponId = PhysicalWeaponId,
                Values = Values,
            });
        }

        private void SendFunc(FunctionCall call)
        {
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            //MyLog.Default.WriteLine($"ModularWeapons: Sending function call [id {call.ActionId}] to [{call.DefinitionName}].");
        }
    }
}
