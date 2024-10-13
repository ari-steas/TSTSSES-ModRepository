using Sandbox.ModAPI;
using VRageMath;
using System;
using RealGasGiants;
using VRage.Game;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.Library.Utils;
using VRage;
using VRage.ModAPI;

namespace RingMoonletSpawner
{
    public class RingMoonletSpawner
    {
        private Random _rand;

        // Hardcoded planet and ring properties based on your clarification
        private const double PlanetRadiusKm = 60268; // Planet's radius in kilometers
        private const double RingInnerScale = 1.2;   // Start the ring at 20% beyond the planet's radius
        private const double RingOuterScale = 2.5;   // Outer ring scale, based on your preference

        // Calculate the inner and outer ring distances from the planet's surface
        private const double RingInnerRadiusKm = PlanetRadiusKm * RingInnerScale; // 72,321.6 km
        private const double RingOuterRadiusKm = PlanetRadiusKm * RingOuterScale; // 150,670 km

        public RingMoonletSpawner(int seed)
        {
            _rand = new Random(seed);
        }

        public void PopulateMoonlets(int moonletCount, Vector3D planetCenter)
        {
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Spawning moonlets along Saturn's ring. Planet center: {planetCenter}");
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Ring Inner Radius: {RingInnerRadiusKm:N2} km, Outer Radius: {RingOuterRadiusKm:N2} km");

            int spawnedMoonlets = 0;

            for (int i = 0; i < moonletCount; i++)
            {
                Vector3D moonletPosition = GetRandomPositionInRingPlane(planetCenter);
                SpawnMoonlet(moonletPosition);
                spawnedMoonlets++;
            }

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Spawned {spawnedMoonlets} moonlets out of {moonletCount} requested.");
        }

        // Generate a random position in the ring plane, respecting inner and outer radii
        private Vector3D GetRandomPositionInRingPlane(Vector3D planetCenter)
        {
            // Generate a random angle to distribute the moonlets in a circular manner
            double angle = _rand.NextDouble() * Math.PI * 2;

            // Random distance between inner and outer ring bounds, now in kilometers
            double distanceFromCenterKm = RingInnerRadiusKm + (RingOuterRadiusKm - RingInnerRadiusKm) * _rand.NextDouble();

            // Convert distance to meters for position calculation (because we need positions in meters)
            double distanceFromCenterMeters = distanceFromCenterKm * 1000;

            // Generate the position along the XZ-plane (ring plane, assuming flat ring aligned to XY)
            Vector3D positionInPlane = new Vector3D(
                distanceFromCenterMeters * Math.Cos(angle),
                0,
                distanceFromCenterMeters * Math.Sin(angle)
            );

            // Final moonlet position relative to the planet's center
            Vector3D finalPosition = planetCenter + positionInPlane;

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Generated random position at {distanceFromCenterKm:N2} km from the gas giant.");
            return finalPosition;
        }

        private void SpawnMoonlet(Vector3D position)
        {
            MyPlanet moonlet = MyAPIGateway.Session.VoxelMaps.SpawnPlanet("Moonlet", 1000, _rand.Next(), position) as MyPlanet;

            if (moonlet != null)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Moonlet spawned successfully at {position}");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Failed to spawn moonlet at {position}. Check position and spawn parameters.");
            }
        }

        public static void HandleChatCommand(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/populatemoonlets"))
            {
                sendToOthers = false;
                string[] tokens = messageText.Split(' ');

                if (tokens.Length != 2)
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Usage: /populatemoonlets <moonletCount>");
                    return;
                }

                try
                {
                    int moonletCount = int.Parse(tokens[1]);
                    Vector3D planetCenter = MyAPIGateway.Session.Player.GetPosition(); // Assuming we know how to find the planet center
                    var spawner = new RingMoonletSpawner(MyRandom.Instance.Next());
                    spawner.PopulateMoonlets(moonletCount, planetCenter);
                }
                catch (Exception ex)
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Invalid command parameters: {ex.Message}");
                }
            }
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class RingMoonletSpawnerSession : MySessionComponentBase
    {
        public override void LoadData()
        {
            base.LoadData();
            MyAPIGateway.Utilities.MessageEntered += RingMoonletSpawner.HandleChatCommand;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyAPIGateway.Utilities.MessageEntered -= RingMoonletSpawner.HandleChatCommand;
        }
    }
}
