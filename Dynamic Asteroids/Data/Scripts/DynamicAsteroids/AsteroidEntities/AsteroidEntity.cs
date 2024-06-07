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
        private const double VelocityVariability = 10;
        private const double AngularVelocityVariability = 0.1;

        private static readonly string[] AvailableModels = {
            @"Models\Components\Sphere.mwm"
        };

        public static AsteroidEntity CreateAsteroid(Vector3D position, float size, Vector3D initialVelocity)
        {
            var ent = new AsteroidEntity();
            ent.Init(position, size, initialVelocity);
            return ent;
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
                    MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(1000, newObject), PositionComp.GetPosition() + RandVector()*Size, Vector3D.Forward, Vector3D.Up, Physics);
                }
                Close();
                return;
            }

            for (int i = 0; i < splits; i++)
            {
                Vector3D newPos = this.PositionComp.GetPosition() + RandVector() * Size;
                CreateAsteroid(newPos, newSize, this.Physics.GetVelocityAtPoint(newPos));
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

        public bool UseDamageSystem => true;


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
            Physics.LinearVelocity = initialVelocity + RandVector() * VelocityVariability;
            Physics.AngularVelocity = RandVector() * AngularVelocityVariability;
        }

        private void CreatePhysics()
        {
            float mass = 10000 * Size * Size * Size;
            PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForPhysics(
                this,
                WorldMatrix,
                Vector3.Zero,
                linearDamping: 0,
                angularDamping: 0,
                collisionLayer: CollisionLayers.DefaultCollisionLayer,
                //rigidBodyFlags: RigidBodyFlag.RBF_UNLOCKED_SPEEDS,
                isPhantom: false,
                mass: new ModAPIMass(PositionComp.LocalAABB.Volume(), mass, Vector3.Zero, mass * PositionComp.LocalAABB.Height * PositionComp.LocalAABB.Height / 6 * Matrix.Identity)
            );

            //settings.DetectorColliderCallback += HitCallback;
            //settings.Entity.Flags |= EntityFlags.IsGamePrunningStructureObject;
            MyAPIGateway.Physics.CreateBoxPhysics(settings, PositionComp.LocalAABB.HalfExtents, 0);
            
            Physics.Enabled = true;
            Physics.Activate();
        }

        private Vector3D RandVector()
        {
            var theta = MainSession.I.Rand.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * MainSession.I.Rand.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(MainSession.I.Rand.NextDouble(), 1/3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }
    }
}
