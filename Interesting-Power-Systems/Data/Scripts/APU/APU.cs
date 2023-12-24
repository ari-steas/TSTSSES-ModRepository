using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using VRageMath;
using System;

namespace I46APUBattery
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "APULarge")]
    public class APU : MyGameLogicComponent
    {
        IMyBatteryBlock Battery;
        float Time;
        float TimeLockout;
        bool InLockout;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init (objectBuilder);
			this.NeedsUpdate = VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
            Battery = (Entity as IMyBatteryBlock);
			Battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Recharge;
			Time = 29f;
        }

        public override void UpdateBeforeSimulation()
        {
            if (Battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Discharge || Battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Auto)
            { Time += 1f / 60f; }

            if (Time >= 30f)
            { 
                Time = 0;
                InLockout = true;
            }
            if (InLockout == true)
            {
                TimeLockout += 1f / 60f;
                Battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Recharge;
                if (TimeLockout >= 10f)
                { InLockout = false; } 
            }

            



        }

    }
}