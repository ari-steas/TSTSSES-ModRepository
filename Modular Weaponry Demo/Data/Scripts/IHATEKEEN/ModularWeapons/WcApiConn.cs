using CoreSystems.Api;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;

namespace CoreParts.Data.Scripts.IHATEKEEN.ModularWeapons
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class WcApiConn : MySessionComponentBase
    {
        public static WcApiConn Instance;
        public WcApi wAPI;

        public override void LoadData()
        {
            Instance = this;
            wAPI = new WcApi();
            wAPI.Load();
        }

        protected override void UnloadData()
        {
            Instance = null;
        }
    }
}
