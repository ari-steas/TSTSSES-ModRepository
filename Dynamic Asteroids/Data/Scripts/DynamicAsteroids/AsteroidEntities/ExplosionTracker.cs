using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Utils;
using Sandbox.Game;
using System.Linq;
using DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities;
using VRage.Game.ModAPI;
using System;
using DynamicAsteroids.Data.Scripts.DynamicAsteroids;

namespace DynamicAsteroids
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ExplosionTracker : MySessionComponentBase
    {
        private static ExplosionTracker _instance;
        public static ExplosionTracker Instance => _instance;

        // Make activeExplosions accessible from outside the class
        private List<MyExplosionInfo> activeExplosions = new List<MyExplosionInfo>();

        public override void BeforeStart()
        {
            base.BeforeStart();
            _instance = this;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Iterate through the list and handle explosions (simplified)
            foreach (var explosion in activeExplosions)
            {
                try
                {
                    HandleNearestAsteroidExplosion(explosion);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, typeof(ExplosionTracker), "ExplosionTracker.UpdateAfterSimulation Exception");
                }
            }

            // Clear the explosions list after processing all of them
            activeExplosions.Clear();
        }

        public void RegisterExplosion(Vector3D position, double radius, float damage)
        {
            MyExplosionInfo explosionInfo = new MyExplosionInfo
            {
                ExplosionSphere = new BoundingSphereD(position, radius),
                Damage = damage
            };
            activeExplosions.Add(explosionInfo);
        }

        public override void LoadData()
        {
            base.LoadData();
            MyExplosions.OnExplosion += OnExplosion;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyExplosions.OnExplosion -= OnExplosion;
            _instance = null;
        }

        private void OnExplosion(ref MyExplosionInfo explosionInfo)
        {
            // Register explosion for tracking with proper damage
            RegisterExplosion(explosionInfo.ExplosionSphere.Center, explosionInfo.ExplosionSphere.Radius, explosionInfo.Damage);
        }

        private void HandleNearestAsteroidExplosion(MyExplosionInfo explosion)
        {
            try
            {
                // Find the nearest asteroid to the explosion
                var nearestAsteroid = FindNearestAsteroid(explosion.ExplosionSphere.Center);

                if (nearestAsteroid != null)
                {
                    // Notify that an asteroid has been found
                    MyAPIGateway.Utilities.ShowNotification($"Found nearest asteroid ID: {nearestAsteroid.EntityId}", 2000, MyFontEnum.Green);

                    // Calculate the distance between the explosion center and the asteroid surface
                    double distanceSquared = Vector3D.DistanceSquared(nearestAsteroid.PositionComp.GetPosition(), explosion.ExplosionSphere.Center);
                    double distance = Math.Sqrt(distanceSquared);

                    // Get the approximate radius of the asteroid
                    float asteroidRadius = nearestAsteroid.PositionComp.LocalVolume.Radius;

                    // Adjust the distance to account for the asteroid's radius
                    double effectiveDistance = distance - asteroidRadius;
                    effectiveDistance = Math.Max(0, effectiveDistance); // Ensure effectiveDistance is not negative

                    // Calculate the impact factor using a quadratic fall-off model
                    double impactFactor = 1.0 - Math.Pow(effectiveDistance / explosion.ExplosionSphere.Radius, 2);

                    // Clamp the impact factor between 0 and 1 manually
                    if (impactFactor < 0.0) impactFactor = 0.0;
                    if (impactFactor > 1.0) impactFactor = 1.0;

                    if (impactFactor > 0)
                    {
                        // Apply damage scaled by the impact factor based on distance
                        float damageToApply = (float)(explosion.Damage * impactFactor);
                        damageToApply = Math.Min(damageToApply, explosion.Damage); // Ensure damage doesn't exceed the original damage

                        // Reduce asteroid integrity
                        nearestAsteroid.ReduceIntegrity(damageToApply);
                        nearestAsteroid._integrity = Math.Max(0, nearestAsteroid._integrity); // Clamp integrity to 0 to prevent negative values

                        // Notify about the damage applied
                        string notificationText = $"Damaged Asteroid ID: {nearestAsteroid.EntityId}, Damage: {damageToApply}, New Integrity: {nearestAsteroid._integrity}";
                        MyAPIGateway.Utilities.ShowNotification(notificationText, 2000, MyFontEnum.Red);
                    }
                    else
                    {
                        // Notify that the impact factor was too low
                        MyAPIGateway.Utilities.ShowNotification($"Impact factor for asteroid ID: {nearestAsteroid.EntityId} is too low, no damage applied", 2000, MyFontEnum.Red);
                    }
                }
                else
                {
                    // Notify that no asteroid was found
                    MyAPIGateway.Utilities.ShowNotification("No asteroid found near the explosion.", 2000, MyFontEnum.Red);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(ExplosionTracker), "ExplosionTracker.HandleNearestAsteroidExplosion Exception");
            }
        }

        private AsteroidEntity FindNearestAsteroid(Vector3D explosionPosition)
        {
            double minDistance = double.MaxValue;
            AsteroidEntity nearestAsteroid = null;

            // Iterate through all asteroids to find the nearest one
            foreach (var entity in MyEntities.GetEntities().OfType<AsteroidEntity>())
            {
                try
                {
                    double distance = Vector3D.DistanceSquared(explosionPosition, entity.PositionComp.GetPosition());
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestAsteroid = entity;
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, typeof(ExplosionTracker), "ExplosionTracker.FindNearestAsteroid - Iteration Exception");
                }
            }

            // Logging for debugging
            if (nearestAsteroid != null)
            {
                Log.Info($"Nearest asteroid found with ID: {nearestAsteroid.EntityId} at distance squared: {minDistance}");
            }
            else
            {
                Log.Info("No nearest asteroid found within the given explosion range.");
            }

            return nearestAsteroid;
        }
    }
}
