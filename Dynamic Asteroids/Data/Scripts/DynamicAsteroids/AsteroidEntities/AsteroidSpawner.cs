using DynamicAsteroids.AsteroidEntities;
using DynamicAsteroids;
using Sandbox.ModAPI;
using SC.SUGMA;
using System.Collections.Generic;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using System.Linq;
using Sandbox.Game.Entities;

public class AsteroidSpawner
{
    public List<AsteroidEntity> _asteroids;
    private bool _canSpawnAsteroids = false;
    private DateTime _worldLoadTime;
    private Random rand;
    public void Init(int seed)
    {
        if (!MyAPIGateway.Session.IsServer)
            return;

        Log.Info("Initializing AsteroidSpawner");
        _asteroids = new List<AsteroidEntity>(AsteroidSettings.MaxAsteroidCount);
        _worldLoadTime = DateTime.UtcNow;
        rand = new Random(seed);
        AsteroidSettings.Seed = seed;
    }

    public void SaveAsteroidState()
    {
        if (!MyAPIGateway.Session.IsServer)
            return;

        var asteroidStates = _asteroids.Select(asteroid => new AsteroidState
        {
            Position = asteroid.PositionComp.GetPosition(),
            Size = asteroid.Size,
            Type = asteroid.Type
        }).ToList();

        var stateBytes = MyAPIGateway.Utilities.SerializeToBinary(asteroidStates);
        using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInLocalStorage("asteroid_states.dat", typeof(AsteroidSpawner)))
        {
            writer.Write(stateBytes, 0, stateBytes.Length);
        }
    }

    public void LoadAsteroidState()
    {
        if (!MyAPIGateway.Session.IsServer)
            return;

        if (MyAPIGateway.Utilities.FileExistsInLocalStorage("asteroid_states.dat", typeof(AsteroidSpawner)))
        {
            byte[] stateBytes;
            using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage("asteroid_states.dat", typeof(AsteroidSpawner)))
            {
                stateBytes = reader.ReadBytes((int)reader.BaseStream.Length);
            }

            var asteroidStates = MyAPIGateway.Utilities.SerializeFromBinary<List<AsteroidState>>(stateBytes);

            foreach (var state in asteroidStates)
            {
                var asteroid = AsteroidEntity.CreateAsteroid(state.Position, state.Size, Vector3D.Zero, state.Type);
                _asteroids.Add(asteroid);
                MyEntities.Add(asteroid); // Ensure the asteroid is added to the game world
            }
        }
    }

    public void Close()
    {
        if (!MyAPIGateway.Session.IsServer)
            return;

        SaveAsteroidState();
        Log.Info("Closing AsteroidSpawner");
        _asteroids?.Clear();
    }

    public void UpdateTick()
    {
        if (!MyAPIGateway.Session.IsServer)
            return;

        // Check if 10 seconds have passed since the world loaded
        if (!_canSpawnAsteroids)
        {
            if ((DateTime.UtcNow - _worldLoadTime).TotalSeconds < 10)
            {
                return;
            }
            _canSpawnAsteroids = true;
        }

        try
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (var player in players)
            {
                Vector3D playerPosition = player.GetPosition();

                foreach (var asteroid in _asteroids.ToArray())
                {
                    double distanceSquared = Vector3D.DistanceSquared(asteroid.PositionComp.GetPosition(), playerPosition);

                    // Remove asteroids that are outside the spherical spawn radius
                    if (distanceSquared > AsteroidSettings.AsteroidSpawnRadius * AsteroidSettings.AsteroidSpawnRadius)
                    {
                        Log.Info($"Removing asteroid at {asteroid.PositionComp.GetPosition()} due to distance from player");
                        _asteroids.Remove(asteroid);

                        var removalMessage = new AsteroidNetworkMessage(asteroid.PositionComp.GetPosition(), asteroid.Size, Vector3D.Zero, Vector3D.Zero, asteroid.Type, false, asteroid.EntityId, true, false);
                        var removalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(removalMessage);
                        MyAPIGateway.Multiplayer.SendMessageToOthers(32000, removalMessageBytes);

                        asteroid.Close();
                        continue;
                    }
                }

                int asteroidsSpawned = 0;
                int spawnAttempts = 0;
                int maxAttempts = 50; // Limit the number of attempts to find valid positions

                while (_asteroids.Count < AsteroidSettings.MaxAsteroidCount && asteroidsSpawned < 10)
                {
                    if (spawnAttempts >= maxAttempts)
                    {
                        Log.Info("Reached maximum spawn attempts, breaking out of loop to prevent freeze");
                        break;
                    }

                    Vector3D newPosition;
                    do
                    {
                        newPosition = playerPosition + RandVector() * AsteroidSettings.AsteroidSpawnRadius;
                        spawnAttempts++;
                    } while (Vector3D.DistanceSquared(newPosition, playerPosition) < AsteroidSettings.MinDistanceFromPlayer * AsteroidSettings.MinDistanceFromPlayer && spawnAttempts < maxAttempts);

                    if (spawnAttempts >= maxAttempts)
                        break;

                    Vector3D newVelocity;
                    if (!AsteroidSettings.CanSpawnAsteroidAtPoint(newPosition, out newVelocity))
                        continue;

                    if (IsNearVanillaAsteroid(newPosition))
                    {
                        Log.Info("Skipped spawning asteroid due to proximity to vanilla asteroid.");
                        continue;
                    }

                    AsteroidType type = AsteroidSettings.GetAsteroidType(newPosition);
                    float size = AsteroidSettings.GetAsteroidSize(newPosition);

                    Log.Info($"Spawning asteroid at {newPosition} with velocity {newVelocity} of type {type}");
                    var asteroid = AsteroidEntity.CreateAsteroid(newPosition, size, newVelocity, type);
                    _asteroids.Add(asteroid);

                    var message = new AsteroidNetworkMessage(newPosition, size, newVelocity, Vector3D.Zero, type, false, asteroid.EntityId, false, true);
                    var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                    MyAPIGateway.Multiplayer.SendMessageToOthers(32000, messageBytes);
                }

                MyAPIGateway.Utilities.ShowNotification($"Active Asteroids: {_asteroids.Count}", 1000 / 60);
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
            if (Vector3D.DistanceSquared(position, voxelMap.GetPosition()) < AsteroidSettings.MinDistanceFromVanillaAsteroids * AsteroidSettings.MinDistanceFromVanillaAsteroids)
            {
                Log.Info($"Position {position} is near vanilla asteroid {voxelMap.StorageName}");
                return true;
            }
        }

        return false;
    }

    private Vector3D RandVector()
    {
        var theta = rand.NextDouble() * 2.0 * Math.PI;
        var phi = Math.Acos(2.0 * rand.NextDouble() - 1.0);
        var sinPhi = Math.Sin(phi);
        return Math.Pow(rand.NextDouble(), 1 / 3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
    }

}

