using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SC.SUGMA;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using ProtoBuf;

namespace DynamicAsteroids.AsteroidEntities
{
    internal class AsteroidSpawner
    {
        public List<AsteroidEntity> _asteroids;
        private const double MinDistanceFromVanillaAsteroids = 1000; // 1 km

        public void Init()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.Info("Initializing AsteroidSpawner");
            _asteroids = new List<AsteroidEntity>(AsteroidSettings.MaxAsteroidCount);
        }

        public void Close()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.Info("Closing AsteroidSpawner");
            _asteroids?.Clear();
        }

        public void UpdateTick()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            try
            {
                //Log.Info("UpdateTick called in AsteroidSpawner");

                // Get all players on the server
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);

                foreach (var player in players)
                {
                    Vector3D playerPosition = player.GetPosition();
                    //Log.Info($"Player {player.DisplayName} position: {playerPosition}");

                    if (!AsteroidSettings.PlayerCanSeeRings(playerPosition))
                    {
                        //Log.Info("Player cannot see rings");
                        continue;
                    }

                    foreach (var asteroid in _asteroids.ToArray())
                    {
                        if (Vector3D.DistanceSquared(asteroid.PositionComp.GetPosition(), playerPosition) >
                            AsteroidSettings.AsteroidSpawnRadius * AsteroidSettings.AsteroidSpawnRadius * 1.1)
                        {
                            Log.Info($"Removing asteroid at {asteroid.PositionComp.GetPosition()} due to distance from player");
                            _asteroids.Remove(asteroid);

                            // Send a network message to clients for removal
                            var removalMessage = new AsteroidNetworkMessage(asteroid.PositionComp.GetPosition(), asteroid.Size, Vector3D.Zero, Vector3D.Zero, asteroid.Type, false, asteroid.EntityId, true);
                            var removalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(removalMessage);
                            MyAPIGateway.Multiplayer.SendMessageToOthers(1337, removalMessageBytes);

                            asteroid.Close();
                            continue;
                        }
                    }

                    int asteroidsSpawned = 0;
                    while (_asteroids.Count < AsteroidSettings.MaxAsteroidCount && asteroidsSpawned < 10)
                    {
                        Vector3D newPosition = playerPosition + RandVector() * AsteroidSettings.AsteroidSpawnRadius;
                        Vector3D newVelocity;
                        if (!AsteroidSettings.CanSpawnAsteroidAtPoint(newPosition, out newVelocity))
                            continue;

                        if (IsNearVanillaAsteroid(newPosition))
                        {
                            Log.Info("Skipped spawning asteroid due to proximity to vanilla asteroid.");
                            continue;
                        }

                        // Determine asteroid type to spawn
                        AsteroidType type = AsteroidSettings.GetRandomAsteroidType(MainSession.I.Rand);

                        Log.Info($"Spawning asteroid at {newPosition} with velocity {newVelocity} of type {type}");
                        var asteroid = AsteroidEntity.CreateAsteroid(newPosition, RandAsteroidSize, newVelocity, type);
                        _asteroids.Add(asteroid);
                        asteroidsSpawned++;

                        // Send a network message to clients
                        var message = new AsteroidNetworkMessage(newPosition, RandAsteroidSize, newVelocity, Vector3D.Zero, type, false, asteroid.EntityId, false);
                        var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                        MyAPIGateway.Multiplayer.SendMessageToOthers(1337, messageBytes);
                    }

                    // Show a notification with the number of active asteroids
                    MyAPIGateway.Utilities.ShowNotification($"Active Asteroids: {_asteroids.Count}", 1000 / 60);

                    // Log the number of active asteroids for debugging purposes
                    //Log.Info($"Active Asteroids: {_asteroids.Count}");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidSpawner));
            }
        }

        private bool IsNearVanillaAsteroid(Vector3D position)
        {
            List<IMyVoxelBase> voxelMaps = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(voxelMaps, v => v is IMyVoxelMap && !v.StorageName.StartsWith("mod_"));

            foreach (var voxelMap in voxelMaps)
            {
                if (Vector3D.DistanceSquared(position, voxelMap.GetPosition()) < MinDistanceFromVanillaAsteroids * MinDistanceFromVanillaAsteroids)
                {
                    Log.Info($"Position {position} is near vanilla asteroid {voxelMap.StorageName}");
                    return true;
                }
            }

            return false;
        }

        private Vector3D RandVector()
        {
            var theta = MainSession.I.Rand.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * MainSession.I.Rand.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(MainSession.I.Rand.NextDouble(), 1 / 3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }

        private float RandAsteroidSize => (float)(MainSession.I.Rand.NextDouble() * MainSession.I.Rand.NextDouble() * MainSession.I.Rand.NextDouble() * 500) + 1.5f;
    }
}
