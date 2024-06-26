using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace DisablePlayerCollisionDamageMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DisablePlayerCollisionDamage : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            // Register the damage handler
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamageApplied);
        }

        protected override void UnloadData()
        {
            // No need to unregister as there's no unregister method
        }

        private void BeforeDamageApplied(object target, ref MyDamageInformation info)
        {
            // Check if the target is a player character
            if (target is IMyCharacter)
            {
                // Show the damage type in a notification
                //MyAPIGateway.Utilities.ShowNotification($"DamageType: {info.Type}", 2000, MyFontEnum.Green);

                // Check if the damage type is collision
                if (info.Type == MyDamageType.Environment || info.Type == MyDamageType.Fall)
                {
                    // Set damage amount to zero for collision damage
                    info.Amount = 0f;
                }
            }
        }
    }
}