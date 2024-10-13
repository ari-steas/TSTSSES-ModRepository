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
        private const float MinimumRingInfluenceForSpawn = 0.1f; // Minimum influence required to spawn
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
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "No nearby gas giant with a significant ring influence found.");
                return;
            }

            // Fetch the ring configuration (position and size) from the API
            var ringInfo = _realGasGiantsApi.GetGasGiantConfig_RingInfo_Size(targetGasGiant);
            if (!ringInfo.Item1)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Could not retrieve ring information for the gas giant.");
                return;
            }

            Vector3D ringCenter = ringInfo.Item2;   // Center of the ring
            float ringInnerRadius = ringInfo.Item3; // Inner radius of the ring
            float ringOuterRadius = ringInfo.Item4; // Outer radius of the ring

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Gas giant ring info: Center at {ringCenter}, Inner Radius: {ringInnerRadius}, Outer Radius: {ringOuterRadius}");

            int spawnedMoonlets = 0;
            int maxAttempts = moonletCount * 5; // Limit attempts to prevent infinite loops
            int attempts = 0;

            while (spawnedMoonlets < moonletCount && attempts < maxAttempts)
            {
                // Distribute moonlets around the ring plane within the ring's inner and outer bounds
                Vector3D moonletPosition = GetRandomPositionInRingPlane(ringCenter, ringInnerRadius, ringOuterRadius);
                float ringInfluence = _realGasGiantsApi.GetRingInfluenceAtPosition(targetGasGiant, moonletPosition);
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Attempt {attempts + 1}: Ring influence at position {moonletPosition}: {ringInfluence}");

                attempts++;

                if (ringInfluence >= MinimumRingInfluenceForSpawn)
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Sufficient ring influence ({ringInfluence}) for spawning moonlet at position {moonletPosition}.");
                    SpawnMoonlet(moonletPosition);
                    spawnedMoonlets++;
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Ring influence too low ({ringInfluence}), skipping position {moonletPosition}.");
                }
            }

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Spawned {spawnedMoonlets} moonlets out of {moonletCount} requested after {attempts} attempts.");
        }

        // New method: Correctly generate a position within the gas giant's ring plane, based on the ring's inner and outer radius
        private Vector3D GetRandomPositionInRingPlane(Vector3D ringCenter, float innerRadius, float outerRadius)
        {
            // Random angle around the ring plane
            double angle = _rand.NextDouble() * Math.PI * 2;

            // Random distance between the inner and outer ring radius
            double distanceFromCenter = innerRadius + (outerRadius - innerRadius) * _rand.NextDouble();

            // Random position within the plane of the ring (Y is kept close to 0 to stay in the ring plane)
            Vector3D position = ringCenter + new Vector3D(distanceFromCenter * Math.Cos(angle), 0, distanceFromCenter * Math.Sin(angle));
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Generated random position {position} along the ring.");
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
