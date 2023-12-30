using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Client
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false)]
    internal class ClientGameLogic : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        // wouldn't it be funny if I killed myself
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            block = (IMyCubeBlock)Entity;

            MyLog.Default.WriteLineAndConsole("Modular Weaponry: AddBlock IsWc: " + WeaponPartManager.Instance.wAPI.HasCoreWeapon((MyEntity) block));
        }
    }
}
