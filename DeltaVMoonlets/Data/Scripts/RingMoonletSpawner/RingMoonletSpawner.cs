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
        private RealGasGiantsApi _realGasGiantsApi;
        private Random _rand;
        private bool _apiInit = false;

        public RingMoonletSpawner(int seed)
        {
            _rand = new Random(seed);
            InitializeApi();
        }

        private void InitializeApi()
        {
            if (_apiInit) return;

            _realGasGiantsApi = new RealGasGiantsApi();
            if (_realGasGiantsApi.Load())
            {
                _apiInit = true;
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "RealGasGiants API initialized successfully.");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Failed to initialize RealGasGiants API.");
            }
        }

        public void PopulateMoonlets(int moonletCount)
        {
            if (!_apiInit || !_realGasGiantsApi.IsReady)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "RealGasGiants API is not ready.");
                return;
            }

            Vector3D playerPosition = MyAPIGateway.Session.Player.GetPosition();
            MyPlanet targetGasGiant = FindNearestGasGiant(playerPosition);

            if (targetGasGiant == null)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "No nearby gas giant with significant ring influence found.");
                return;
            }

            // Fetch the ring configuration (position and size) from the API
            var ringInfo = _realGasGiantsApi.GetGasGiantConfig_RingInfo_Size(targetGasGiant);
            if (!ringInfo.Item1)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Could not retrieve ring information for the gas giant.");
                return;
            }

            Vector3D ringCenter = targetGasGiant.PositionComp.GetPosition();   // Use the actual gas giant's position
            double ringInnerRadius = ringInfo.Item3; // Inner radius of the ring (large scale, ensure correct handling in meters)
            double ringOuterRadius = ringInfo.Item4; // Outer radius of the ring (large scale, ensure correct handling in meters)

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Gas giant ring info: Center at {ringCenter}, Inner Radius: {ringInnerRadius / 1000:N0} km, Outer Radius: {ringOuterRadius / 1000:N0} km");

            if (ringInnerRadius <= 0 || ringOuterRadius <= 0)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Error: Invalid inner or outer radius for the gas giant's ring.");
                return;
            }

            int spawnedMoonlets = 0;

            // Distribute moonlets around the ring plane within the ring's inner and outer bounds
            for (int i = 0; i < moonletCount; i++)
            {
                Vector3D moonletPosition = GetRandomPositionInRingPlane(ringCenter, ringInnerRadius, ringOuterRadius);
                SpawnMoonlet(moonletPosition);
                spawnedMoonlets++;
            }

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Spawned {spawnedMoonlets} moonlets out of {moonletCount} requested.");
        }

        // Method to generate a position within the gas giant's ring plane, based on the ring's inner and outer radius
        private Vector3D GetRandomPositionInRingPlane(Vector3D ringCenter, double innerRadius, double outerRadius)
        {
            // Random angle around the ring plane
            double angle = _rand.NextDouble() * Math.PI * 2;

            // Random distance between the inner and outer ring radius (we're working with large distances in meters)
            double distanceFromCenter = innerRadius + (outerRadius - innerRadius) * _rand.NextDouble();

            if (distanceFromCenter < innerRadius || distanceFromCenter > outerRadius)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Error: Generated distance {distanceFromCenter} is out of bounds.");
                return ringCenter; // Return ring center to avoid crashing, but ideally should never happen
            }

            // Calculate the moonlet's position relative to the gas giant's position (ringCenter)
            Vector3D position = ringCenter + new Vector3D(distanceFromCenter * Math.Cos(angle), 0, distanceFromCenter * Math.Sin(angle));

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Generated random position at {distanceFromCenter / 1000:N0} km from the gas giant.");
            return position;
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

        private MyPlanet FindNearestGasGiant(Vector3D position)
        {
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Searching for the nearest gas giant...");
            const double searchRadius = 1e9; // 1 billion km
            MyPlanet nearestGasGiant = null;
            double nearestDistance = double.MaxValue;

            // Get all gas giants within the larger search sphere
            var gasGiants = _realGasGiantsApi.GetAtmoGasGiantsAtPosition(position);
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Found {gasGiants.Count} potential gas giants to evaluate.");

            foreach (var gasGiant in gasGiants)
            {
                var basicInfo = _realGasGiantsApi.GetGasGiantConfig_BasicInfo_Base(gasGiant);
                if (!basicInfo.Item1)
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Skipping a gas giant due to missing basic info.");
                    continue;
                }

                float gasGiantRadius = basicInfo.Item2;
                Vector3D gasGiantCenter = gasGiant.PositionComp.GetPosition();

                // Calculate distance from player to the surface of the gas giant
                double distance = Vector3D.Distance(position, gasGiantCenter) - gasGiantRadius;

                if (distance < nearestDistance && distance <= searchRadius)
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Found closer gas giant at distance: {distance:N0} meters.");
                    nearestDistance = distance;
                    nearestGasGiant = gasGiant;
                }
            }

            if (nearestGasGiant != null)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Nearest gas giant found successfully.");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "No suitable gas giant found within the search radius.");
            }

            return nearestGasGiant;
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
                    var spawner = new RingMoonletSpawner(MyRandom.Instance.Next());
                    spawner.PopulateMoonlets(moonletCount);
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
