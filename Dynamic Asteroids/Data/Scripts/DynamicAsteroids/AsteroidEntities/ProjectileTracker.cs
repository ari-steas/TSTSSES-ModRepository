using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Utils;
using System;
using DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities;
using VRage.ModAPI;
using DynamicAsteroids.Data.Scripts.DynamicAsteroids;
using System.Collections.Generic;
using System.Linq;

namespace DynamicAsteroids
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ProjectileTracker : MySessionComponentBase
    {
        private static ProjectileTracker _instance;
        public static ProjectileTracker Instance => _instance;

        private IMyProjectiles _projectileSystem;
        private List<AsteroidHitDetector> _asteroidHitDetectors = new List<AsteroidHitDetector>();

        public override void LoadData()
        {
            base.LoadData();
            _instance = this;

            try
            {
                // Access the projectile system
                _projectileSystem = MyAPIGateway.Session as IMyProjectiles;

                if (_projectileSystem != null)
                {
                    Log.Info("Projectile system accessed successfully.");
                    MyAPIGateway.Utilities.ShowNotification("Projectile system accessed successfully.", 4000, MyFontEnum.Green);
                }
                else
                {
                    Log.Exception(new Exception("IMyProjectiles could not be accessed."), typeof(ProjectileTracker), "Failed to access IMyProjectiles.");
                    MyAPIGateway.Utilities.ShowNotification("Error: IMyProjectiles could not be accessed.", 4000, MyFontEnum.Red);
                    return;
                }

                // Create a hit detector for each asteroid entity
                foreach (var entity in MyEntities.GetEntities().OfType<AsteroidEntity>())
                {
                    AddAsteroidHitDetector(entity);
                }

                MyAPIGateway.Utilities.ShowNotification("Asteroid Hit Detectors added to the projectile system.", 4000, MyFontEnum.Green);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(ProjectileTracker), "Error during LoadData.");
                MyAPIGateway.Utilities.ShowNotification("Error during LoadData. See log for details.", 4000, MyFontEnum.Red);
            }
        }

        protected override void UnloadData()
        {
            base.UnloadData();

            try
            {
                foreach (var detector in _asteroidHitDetectors)
                {
                    _projectileSystem.RemoveHitDetector(detector);
                    Log.Info("Asteroid Hit Detector removed from the projectile system.");
                }

                MyAPIGateway.Utilities.ShowNotification("Asteroid Hit Detectors removed from the projectile system.", 4000, MyFontEnum.Blue);
                _asteroidHitDetectors.Clear();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(ProjectileTracker), "Error during UnloadData.");
                MyAPIGateway.Utilities.ShowNotification("Error during UnloadData. See log for details.", 4000, MyFontEnum.Red);
            }

            _instance = null;
        }

        private void AddAsteroidHitDetector(AsteroidEntity asteroid)
        {
            try
            {
                var detector = new AsteroidHitDetector(asteroid);
                _projectileSystem.AddHitDetector(detector);
                _asteroidHitDetectors.Add(detector);

                Log.Info($"Added Hit Detector for asteroid ID: {asteroid.EntityId}");
                MyAPIGateway.Utilities.ShowNotification($"Hit Detector added for asteroid ID: {asteroid.EntityId}", 2000, MyFontEnum.White);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(ProjectileTracker), "Error adding Hit Detector for asteroid.");
                MyAPIGateway.Utilities.ShowNotification("Error adding Hit Detector for asteroid. See log for details.", 4000, MyFontEnum.Red);
            }
        }

        private class AsteroidHitDetector : IMyProjectileDetector
        {
            public bool IsDetectorEnabled => true;

            public BoundingBoxD DetectorAABB { get; private set; }

            public IMyEntity HitEntity { get; private set; }

            private readonly AsteroidEntity _asteroid;

            public AsteroidHitDetector(AsteroidEntity asteroid)
            {
                _asteroid = asteroid;
                UpdateAABB();
            }

            public void UpdateAABB()
            {
                // Set AABB based on the asteroid's position and radius to closely match its spherical volume.
                Vector3D asteroidPosition = _asteroid.PositionComp.GetPosition();
                float asteroidRadius = _asteroid.PositionComp.LocalVolume.Radius;

                DetectorAABB = new BoundingBoxD(asteroidPosition - new Vector3D(asteroidRadius), asteroidPosition + new Vector3D(asteroidRadius));
                Log.Info($"Asteroid Detector AABB updated for asteroid ID: {_asteroid.EntityId}, AABB Min: {DetectorAABB.Min}, Max: {DetectorAABB.Max}");

                // Show notification about AABB update for each asteroid
                MyAPIGateway.Utilities.ShowNotification($"Updated AABB for asteroid ID: {_asteroid.EntityId}", 2000, MyFontEnum.White);
            }

            public bool GetDetectorIntersectionWithLine(ref LineD line, out Vector3D? hit)
            {
                hit = null;

                try
                {
                    if (_asteroid.PositionComp == null)
                    {
                        return false;
                    }

                    // Create a bounding box around the asteroid that is closer in size to its spherical volume
                    Vector3D asteroidPosition = _asteroid.PositionComp.GetPosition();
                    float asteroidRadius = _asteroid.PositionComp.LocalVolume.Radius;
                    var asteroidAABB = new BoundingBoxD(asteroidPosition - new Vector3D(asteroidRadius), asteroidPosition + new Vector3D(asteroidRadius));

                    // Create a ray from the line to use for intersection detection
                    RayD ray = new RayD(line.From, line.Direction);

                    // Check if the ray intersects the bounding box of the asteroid
                    double? intersectionDistance = asteroidAABB.Intersects(ray);
                    if (intersectionDistance.HasValue && intersectionDistance.Value <= line.Length)
                    {
                        // Calculate the intersection point
                        Vector3D hitPosition = line.From + line.Direction * intersectionDistance.Value;
                        hit = hitPosition;
                        HitEntity = _asteroid;

                        // Log the hit for debugging purposes
                        Log.Info($"Projectile hit detected on asteroid ID: {_asteroid.EntityId} at position: {hitPosition}");
                        MyAPIGateway.Utilities.ShowNotification($"Hit detected on asteroid ID: {_asteroid.EntityId} at {hitPosition}", 2000, MyFontEnum.Blue);

                        // Apply damage to the asteroid
                        HandleProjectileHitAsteroid(_asteroid, hitPosition);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, typeof(AsteroidHitDetector), "Error in GetDetectorIntersectionWithLine.");
                    MyAPIGateway.Utilities.ShowNotification("Error during projectile detection. See log for details.", 4000, MyFontEnum.Red);
                }

                return false;
            }

            private void HandleProjectileHitAsteroid(AsteroidEntity asteroid, Vector3D hitPosition)
            {
                try
                {
                    // Assume a fixed damage value (this could be improved if we can access projectile info)
                    float damage = 100f;

                    asteroid.ReduceIntegrity(damage);
                    MyAPIGateway.Utilities.ShowNotification($"Asteroid hit by projectile. Damage applied: {damage}", 2000, MyFontEnum.Red);
                    Log.Info($"Damage of {damage} applied to asteroid ID: {asteroid.EntityId} at position {hitPosition}.");
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, typeof(AsteroidHitDetector), "Error while applying damage to asteroid.");
                    MyAPIGateway.Utilities.ShowNotification("Error while applying damage to asteroid. See log for details.", 4000, MyFontEnum.Red);
                }
            }
        }
    }
}
