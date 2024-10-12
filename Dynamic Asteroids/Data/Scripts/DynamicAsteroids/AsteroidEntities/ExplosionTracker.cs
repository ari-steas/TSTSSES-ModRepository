using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Utils;
using Sandbox.Game;

namespace DynamicAsteroids
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ExplosionTracker : MySessionComponentBase
    {
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
                Damage = damage // Correctly set the damage value
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
        }

        private void OnExplosion(ref MyExplosionInfo explosionInfo)
        {
            // Register explosion for tracking with proper damage
            RegisterExplosion(explosionInfo.ExplosionSphere.Center, explosionInfo.ExplosionSphere.Radius, explosionInfo.Damage);
        }

        private void HandleExplosion(MyExplosionInfo explosion)
        {
            // Display explosion details including damage
            string notificationText = $"Explosion at {explosion.ExplosionSphere.Center}, Radius: {explosion.ExplosionSphere.Radius}, Damage: {explosion.Damage}";
            MyAPIGateway.Utilities.ShowNotification(notificationText, 1000, MyFontEnum.Red);
        }
    }
}