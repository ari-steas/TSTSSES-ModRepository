using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, Priority = 0)]
    internal class ApiHandler : MySessionComponentBase
    {
        private const long Channel = 8774;
        private Dictionary<string, Delegate> _apiDefinitions = new ApiDefinitions().ModApiMethods;

        /// <summary>
        /// Is the API ready?
        /// </summary>
        public bool IsReady { get; private set; }

        private void HandleMessage(object o)
        {
            if ((o as string) == "ApiEndpointRequest")
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, _apiDefinitions);
                MyLog.Default.WriteLine("ModularWeaponry: ModularDefinitionsAPI start load.");
            }
            else
                MyLog.Default.WriteLine($"ModularWeaponry: ModularDefinitionsAPI ignored message {o as string}.");
        }

        /// <summary>
        /// Registers for API requests and updates any pre-existing clients.
        /// </summary>
        public override void LoadData()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);

            IsReady = true;
            try
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, _apiDefinitions);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"Exception in Api Load: {ex}"); 
            }
            MyLog.Default.WriteLine("ModularWeaponry: ModularDefinitionsAPI inited.");
        }


        /// <summary>
        /// Unloads all API endpoints and detaches events.
        /// </summary>
        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);

            IsReady = false;
            MyAPIGateway.Utilities.SendModMessage(Channel, new Dictionary<string, Delegate>());

            MyLog.Default.WriteLine("ModularWeaponry: ModularDefinitionsAPI unloaded.");
        }
    }
}
