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
using static Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions.DefinitionDefs;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
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
            Instance = this;

            if (!MyAPIGateway.Session.IsServer)
                return;

            MyLog.Default.WriteLineAndConsole("Modular Assemblies: DefinitionHandler loading...");

            MyLog.Default.WriteLineAndConsole("ModularAssemblies: Init DefinitionHandler.cs");
            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(InboundMessageId, ActionMessageHandler);
            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, true);
        }

        protected override void UnloadData()
        {
            Instance = null;
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyLog.Default.WriteLineAndConsole("Modular Assemblies: DefinitionHandler closing...");

            base.UnloadData();
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, DefMessageHandler);
            MyAPIGateway.Utilities.UnregisterMessageHandler(InboundMessageId, ActionMessageHandler);
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
                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Recieved {baseDefArray.PhysicalDefs.Length} definitions.");
                    foreach (var def in baseDefArray.PhysicalDefs)
                    {
                        ModularDefinition modDef = ModularDefinition.Load(def);
                        if (modDef != null)
                            ModularDefinitions.Add(modDef);
                    }
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: baseDefArray null!");
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Exception in DefinitionMessageHandler: {ex}"); }
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
                    //MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Recieved action of type {functionCall.ActionId}.");

                    PhysicalAssembly wep = AssemblyPartManager.Instance.AllPhysicalAssemblies[functionCall.PhysicalAssemblyId];
                    if (wep == null)
                    {
                        MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Invalid PhysicalAssembly!");
                        return;
                    }

                    // TODO: Remove
                    //object[] Values = functionCall.Values.Values();

                    switch (functionCall.ActionId)
                    {
                        default:
                            // Fill in here if necessary.
                            break;
                    }
                }
                else
                {
                    MyLog.Default.WriteLineAndConsole($"ModularAssemblies: functionCall null!");
                }
            }
            catch (Exception ex) { MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Exception in ActionMessageHandler: {ex}"); }
        }

        public void SendOnPartAdd(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
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
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        public void SendOnPartRemove(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
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
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        public void SendOnPartDestroy(string DefinitionName, int PhysicalAssemblyId, long BlockEntityId, bool IsBaseBlock)
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
                PhysicalAssemblyId = PhysicalAssemblyId,
                Values = Values,
            });
        }

        private void SendFunc(FunctionCall call)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.Utilities.SendModMessage(OutboundMessageId, MyAPIGateway.Utilities.SerializeToBinary(call));
            //MyLog.Default.WriteLineAndConsole($"ModularAssemblies: Sending function call [id {call.ActionId}] to [{call.DefinitionName}].");
        }
    }
}
