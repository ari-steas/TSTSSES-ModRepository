using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Utils;
using System;
using DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities;
using DynamicAsteroids.Data.Scripts.DynamicAsteroids;
using Sandbox.Game;
using VRage.ModAPI;
using VRage.Game.ModAPI;

namespace DynamicAsteroids
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ProjectileTracker : MySessionComponentBase
    {
        private static ProjectileTracker _instance;
        public static ProjectileTracker Instance => _instance;

        public override void LoadData()
        {
            base.LoadData();
            _instance = this;

            try
            {
                // Any initialization that could potentially throw an exception
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(ProjectileTracker), "Error during LoadData.");
            }
        }

        public override void BeforeStart()
        {
            base.BeforeStart();

            try
            {
                // Register to handle damage events
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnEntityDamaged);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(ProjectileTracker), "Error during BeforeStart.");
            }
        }

        private void OnEntityDamaged(object target, ref MyDamageInformation info)
        {
            try
            {
                // Check if the target is an AsteroidEntity
                var asteroid = target as AsteroidEntity;
                if (asteroid != null)
                {
                    // Check if the damage type is one of the relevant projectile or explosive damage types
                    if (
                        info.Type == MyDamageType.Explosion ||
                        info.Type == MyDamageType.Rocket ||
                        info.Type == MyDamageType.Bullet)
                    {
                        try
                        {
                            // Apply the damage to the asteroid's integrity
                            asteroid.ReduceIntegrity(info.Amount);

                            // Log the collision event (optional)
                            MyAPIGateway.Utilities.ShowNotification($"Asteroid hit by projectile. Damage applied: {info.Amount}", 2000, MyFontEnum.Red);
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(ex, typeof(ProjectileTracker), "Error while reducing asteroid integrity.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(ProjectileTracker), "Error in OnEntityDamaged handler.");
            }
        }
    }
}
