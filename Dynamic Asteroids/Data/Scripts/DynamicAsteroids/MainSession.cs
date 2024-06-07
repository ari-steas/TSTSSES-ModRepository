using System;
using System.Collections.Generic;
using DynamicAsteroids.AsteroidEntities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SC.SUGMA;
using VRage.Game.Components;
using VRage.Input;
using VRageMath;

namespace DynamicAsteroids
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MainSession : MySessionComponentBase
    {
        public static MainSession I;

        public Random Rand = new Random();

        private AsteroidSpawner _spawner = new AsteroidSpawner();

        #region Base Methods

        public override void LoadData()
        {
            I = this;
            Log.Init();

            try
            {
                _spawner.Init();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(MainSession));
            }
        }

        protected override void UnloadData()
        {
            try
            {
                _spawner.Close();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(MainSession));
            }

            Log.Close();
            I = null;
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                _spawner.UpdateTick();
                if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.MiddleButton))
                    AsteroidEntity.CreateAsteroid(MyAPIGateway.Session.Player?.GetPosition() ?? Vector3D.Zero, Rand.Next(50), MyAPIGateway.Session.Player?.Character?.Physics?.LinearVelocity ?? Vector3D.Zero);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(MainSession));
            }
        }

        #endregion
    }
}
