using System;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
using VRage.Utils;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace DynamicAsteroids.AsteroidEntities
{
    public class AsteroidEntity : MyEntity, IMyDestroyableObject
    {
        private const double VelocityVariablility = 4;

        private static readonly string[] AvailableModels = {
            @"Models\Components\Sphere.mwm"
        };

        public static void CreateAsteroid(Vector3D position, float size, Vector3D initialVelocity)
        {
            new AsteroidEntity().Init(position, size, initialVelocity);
        }

        public float Size = 3;
        public string ModelString = "";
        private float _integrity = 1;

        public void SplitAsteroid()
        {
            int splits = MainSession.I.Rand.Next(2, 5);

            if (splits > Size)
                splits = (int) Math.Ceiling(Size);

            float newSize = Size / splits;
            MyAPIGateway.Utilities.ShowNotification($"NS: {newSize}");

            if (newSize <= 1)
            {
                MyPhysicalItemDefinition item =
                    MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Ore),
                        "Stone"));
                var newObject = MyObjectBuilderSerializer.CreateNewObject(item.Id.TypeId, item.Id.SubtypeId.ToString()) as MyObjectBuilder_PhysicalObject;
                for (int i = 0; i < splits; i++)
                {

                    //MyFloatingObjects.Spawn(item, PositionComp.GetPosition() + RandVector*Size*4, Vector3D.Forward, Vector3D.Up);

                    //MyObjectBuilder_PhysicalObject newObject = MyObjectBuilderSerializerKeen.CreateNewObject(item.Id.TypeId, item.Id.SubtypeName) as MyObjectBuilder_PhysicalObject;
                    MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(1000, newObject), PositionComp.GetPosition() + RandVector*Size, Vector3D.Forward, Vector3D.Up, Physics);
                }
                Close();
                return;
            }

            for (int i = 0; i < splits; i++)
            {
                CreateAsteroid(this.PositionComp.GetPosition() + RandVector*Size, newSize, this.Physics.LinearVelocity);
            }
            Close();
        }

        public void OnDestroy()
        {
            SplitAsteroid();
        }

        public bool DoDamage(float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0,
            long realHitEntityId = 0, bool shouldDetonateAmmo = true, MyStringHash? extraInfo = null)
        {
            _integrity -= damage;
            if (Integrity < 0)
                OnDestroy();
            return true;
        }

        public float Integrity => _integrity;

        public bool UseDamageSystem { get; } = true;







        private void Init(Vector3D position, float size, Vector3D initialVelocity)
        {
            ModelString = AvailableModels[MainSession.I.Rand.Next(0, AvailableModels.Length - 1)];
            Size = size;

            Init(null, ModelString, null, Size);

            if (string.IsNullOrEmpty(ModelString))
                Flags &= ~EntityFlags.Visible;

            Save = false;
            NeedsWorldMatrix = true;

            //Flags |= EntityFlags.Visible;
            //Flags |= EntityFlags.Near;
            //Flags |= EntityFlags.Sync;
            //Flags |= EntityFlags.NeedsDraw;
            PositionComp.LocalAABB = new BoundingBox(-Vector3.Half*Size, Vector3.Half*Size);

            WorldMatrix = MatrixD.CreateWorld(position, Vector3D.Forward, Vector3D.Up);

            MyEntities.Add(this);

            CreatePhysics();
            Physics.LinearVelocity = initialVelocity + RandVector * VelocityVariablility;
        }

        private void CreatePhysics()
        {
            PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForPhysics(
                this,
                WorldMatrix,
                Vector3.Zero,
                linearDamping: 0,
                angularDamping: 0,
                collisionLayer: CollisionLayers.DefaultCollisionLayer,
                rigidBodyFlags: RigidBodyFlag.RBF_UNLOCKED_SPEEDS,
                isPhantom: false,
                mass: new ModAPIMass(PositionComp.LocalAABB.Volume(), 10000, Vector3.Zero, new Matrix(48531.0f, -1320.0f, 0.0f, -1320.0f, 256608.0f, 0.0f, 0.0f, 0.0f, 211333.0f))
            );

            //settings.DetectorColliderCallback += HitCallback;
            //settings.Entity.Flags |= EntityFlags.IsGamePrunningStructureObject;
            MyAPIGateway.Physics.CreateBoxPhysics(settings, PositionComp.LocalAABB.HalfExtents, 0);

            Physics.Enabled = true;
            Physics.Activate();
        }

        private Vector3D RandVector => new Vector3D(MainSession.I.Rand.NextDouble(), MainSession.I.Rand.NextDouble(),
            MainSession.I.Rand.NextDouble());
    }
}
