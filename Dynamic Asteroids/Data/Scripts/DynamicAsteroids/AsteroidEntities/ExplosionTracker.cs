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

namespace DynamicAsteroids
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ExplosionTracker : MySessionComponentBase
    {
        private static ExplosionTracker _instance;
        public static ExplosionTracker Instance => _instance;

        // Make activeExplosions accessible from outside the class
        public List<MyExplosionInfo> ActiveExplosions => activeExplosions;

        private List<MyExplosionInfo> activeExplosions = new List<MyExplosionInfo>();

        public override void BeforeStart()
        {
            base.BeforeStart();
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1000, DamageHandler);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Iterate over active explosions and handle relevant actions
            foreach (var explosion in activeExplosions)
            {
                HandleExplosion(explosion);
            }

            // Cleanup completed explosions
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
            _instance = this;
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

        private void HandleExplosion(MyExplosionInfo explosion)
        {
            string notificationText = $"Explosion at {explosion.ExplosionSphere.Center}, Radius: {explosion.ExplosionSphere.Radius}, Damage: {explosion.Damage}";
            MyAPIGateway.Utilities.ShowNotification(notificationText, 1000, MyFontEnum.Red);

            // Find the nearest asteroid to the explosion
            var nearestAsteroid = FindNearestAsteroid(explosion.ExplosionSphere.Center);
            if (nearestAsteroid != null)
            {
                string nearestAsteroidText = $"Nearest Asteroid to Explosion: ID {nearestAsteroid.EntityId}, Position: {nearestAsteroid.PositionComp.GetPosition()}";
                MyAPIGateway.Utilities.ShowNotification(nearestAsteroidText, 2000, MyFontEnum.Green);

                IMyGps gps = MyAPIGateway.Session.GPS.Create("Nearest Asteroid", nearestAsteroidText, nearestAsteroid.PositionComp.GetPosition(), true, true);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
            }
        }

        private List<AsteroidEntity> FindAsteroidsInRange(BoundingSphereD explosionSphere)
        {
            var asteroidsInRange = new List<AsteroidEntity>();

            // Iterate through all asteroids to find those within the explosion radius
            foreach (var entity in MyEntities.GetEntities().OfType<AsteroidEntity>())
            {
                double distanceSquared = Vector3D.DistanceSquared(explosionSphere.Center, entity.PositionComp.GetPosition());
                if (distanceSquared <= explosionSphere.Radius * explosionSphere.Radius)
                {
                    asteroidsInRange.Add(entity);
                }
            }

            return asteroidsInRange;
        }

        private AsteroidEntity FindNearestAsteroid(Vector3D explosionPosition)
        {
            double minDistance = double.MaxValue;
            AsteroidEntity nearestAsteroid = null;

            // Iterate through all asteroids to find the nearest one
            foreach (var entity in MyEntities.GetEntities().OfType<AsteroidEntity>())
            {
                double distance = Vector3D.DistanceSquared(explosionPosition, entity.PositionComp.GetPosition());
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestAsteroid = entity;
                }
            }

            return nearestAsteroid;
        }

        private void DamageHandler(object target, ref MyDamageInformation info)
        {
            // Only handle explosion damage
            if (info.Type != MyStringHash.GetOrCompute("Explosion"))
            {
                return;
            }

            // Only proceed if the target is an AsteroidEntity
            var asteroid = target as AsteroidEntity;
            if (asteroid != null)
            {
                // Check if the asteroid is actually within any active explosion radius
                foreach (var explosion in activeExplosions)
                {
                    double distanceSquared = Vector3D.DistanceSquared(asteroid.PositionComp.GetPosition(), explosion.ExplosionSphere.Center);
                    if (distanceSquared <= explosion.ExplosionSphere.Radius * explosion.ExplosionSphere.Radius)
                    {
                        // Allow the damage to be applied
                        return;
                    }
                }

                // If no explosion affects this asteroid, set damage to zero
                info.Amount = 0;
            }
        }

        public List<MyExplosionInfo> GetExplosionsNear(Vector3D position, double effectiveRadius)
        {
            return activeExplosions.Where(explosion =>
                Vector3D.DistanceSquared(position, explosion.ExplosionSphere.Center) <= effectiveRadius * effectiveRadius
            ).ToList();
        }
    }
}