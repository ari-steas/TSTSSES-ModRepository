using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Utils;
using Sandbox.Game;
using System.Linq;

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
        }

        public List<MyExplosionInfo> GetExplosionsNear(Vector3D position, double effectiveRadius)
        {
            return activeExplosions.Where(explosion =>
                Vector3D.DistanceSquared(position, explosion.ExplosionSphere.Center) <= effectiveRadius * effectiveRadius
            ).ToList();
        }

    }
}
