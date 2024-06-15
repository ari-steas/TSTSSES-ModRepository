using DynamicAsteroids.AsteroidEntities;
using Sandbox.ModAPI;
using SC.SUGMA;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using System.Linq;
using System;
using DynamicAsteroids;
using Sandbox.Game.Entities;

public class AsteroidZone
{
    public Vector3D Center { get; set; }
    public double Radius { get; set; }

    public AsteroidZone(Vector3D center, double radius)
    {
        Center = center;
        Radius = radius;
    }

    public bool IsPointInZone(Vector3D point)
    {
        return Vector3D.DistanceSquared(Center, point) <= Radius * Radius;
    }
}

public class AsteroidSpawner
{
    public List<AsteroidEntity> _asteroids;
    private bool _canSpawnAsteroids = false;
    private DateTime _worldLoadTime;
    private Random rand;
    private List<AsteroidState> _despawnedAsteroids = new List<AsteroidState>();
    private List<AsteroidNetworkMessage> _networkMessages = new List<AsteroidNetworkMessage>();

    private Dictionary<long, AsteroidZone> playerZones = new Dictionary<long, AsteroidZone>();
    private const double ZONE_RADIUS = 10000; // Example radius, adjust as needed

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
        if (!MyAPIGateway.Session.IsServer || !AsteroidSettings.EnablePersistence)
            return;

        var asteroidStates = _asteroids.Select(asteroid => new AsteroidState
        {
            Position = asteroid.PositionComp.GetPosition(),
            Size = asteroid.Size,
            Type = asteroid.Type,
            EntityId = asteroid.EntityId // Save unique ID
        }).ToList();

        asteroidStates.AddRange(_despawnedAsteroids);

        var stateBytes = MyAPIGateway.Utilities.SerializeToBinary(asteroidStates);
        using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInLocalStorage("asteroid_states.dat", typeof(AsteroidSpawner)))
        {
            writer.Write(stateBytes, 0, stateBytes.Length);
        }
    }

    public void LoadAsteroidState()
    {
        if (!MyAPIGateway.Session.IsServer || !AsteroidSettings.EnablePersistence)
            return;

        _asteroids.Clear();

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
                if (_asteroids.Any(a => a.EntityId == state.EntityId))
                {
                    Log.Info($"Skipping duplicate asteroid with ID {state.EntityId}");
                    continue; // Skip duplicates
                }

                var asteroid = AsteroidEntity.CreateAsteroid(state.Position, state.Size, Vector3D.Zero, state.Type);
                asteroid.EntityId = state.EntityId; // Assign the saved ID
                _asteroids.Add(asteroid);
                MyEntities.Add(asteroid);
            }
        }
    }

    private void LoadAsteroidsInRange(Vector3D playerPosition, AsteroidZone zone)
    {
        foreach (var state in _despawnedAsteroids.ToArray())
        {
            if (zone.IsPointInZone(state.Position))
            {
                bool tooClose = _asteroids.Any(a => Vector3D.DistanceSquared(a.PositionComp.GetPosition(), state.Position) < AsteroidSettings.MinDistanceFromPlayer * AsteroidSettings.MinDistanceFromPlayer);
                bool exists = _asteroids.Any(a => a.EntityId == state.EntityId); // Check for existing IDs

                if (tooClose || exists)
                {
                    Log.Info($"Skipping respawn of asteroid at {state.Position} due to proximity to other asteroids or duplicate ID");
                    continue;
                }

                Log.Info($"Respawning asteroid at {state.Position} due to player re-entering range");
                var asteroid = AsteroidEntity.CreateAsteroid(state.Position, state.Size, Vector3D.Zero, state.Type);
                asteroid.EntityId = state.EntityId; // Assign the saved ID
                _asteroids.Add(asteroid);

                var message = new AsteroidNetworkMessage(state.Position, state.Size, Vector3D.Zero, Vector3D.Zero, state.Type, false, asteroid.EntityId, false, true, Quaternion.Identity);
                var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, messageBytes);

                _despawnedAsteroids.Remove(state);
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

    public void AssignZonesToPlayers()
    {
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        foreach (var player in players)
        {
            if (!playerZones.ContainsKey(player.IdentityId))
            {
                Vector3D playerPosition = player.GetPosition();
                AsteroidZone zone = new AsteroidZone(playerPosition, ZONE_RADIUS);
                playerZones.Add(player.IdentityId, zone);
            }
        }
    }

    public void UpdateZones()
    {
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        foreach (var player in players)
        {
            AsteroidZone zone;
            if (playerZones.TryGetValue(player.IdentityId, out zone))
            {
                Vector3D playerPosition = player.GetPosition();
                if (!zone.IsPointInZone(playerPosition))
                {
                    // Player has moved out of the zone, reassign
                    zone.Center = playerPosition;
                }
            }
        }
    }

    private int _spawnIntervalTimer = 0;
    private int _updateIntervalTimer = 0;

    public void UpdateTick()
    {
        if (!MyAPIGateway.Session.IsServer) return;

        AssignZonesToPlayers();
        UpdateZones();

        try
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (var player in players)
            {
                Vector3D playerPosition = player.GetPosition();

                AsteroidZone zone;
                if (playerZones.TryGetValue(player.IdentityId, out zone))
                {
                    if (_updateIntervalTimer > 0)
                    {
                        _updateIntervalTimer--;
                    }
                    else
                    {
                        UpdateAsteroids(playerPosition, zone);
                        _updateIntervalTimer = AsteroidSettings.UpdateInterval;
                    }

                    if (_spawnIntervalTimer > 0)
                    {
                        _spawnIntervalTimer--;
                    }
                    else
                    {
                        SpawnAsteroids(playerPosition, zone);
                        _spawnIntervalTimer = AsteroidSettings.SpawnInterval;
                    }

                    LoadAsteroidsInRange(playerPosition, zone);

                    if (AsteroidSettings.EnableLogging)
                        MyAPIGateway.Utilities.ShowNotification($"Active Asteroids: {_asteroids.Count}", 1000 / 60);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, typeof(AsteroidSpawner));
        }
    }

    private void UpdateAsteroids(Vector3D playerPosition, AsteroidZone zone)
    {
        foreach (var asteroid in _asteroids.ToArray())
        {
            if (!zone.IsPointInZone(asteroid.PositionComp.GetPosition()))
            {
                Log.Info($"Removing asteroid at {asteroid.PositionComp.GetPosition()} due to distance from player");
                RemoveAsteroid(asteroid);
            }
        }
    }

    private void SpawnAsteroids(Vector3D playerPosition, AsteroidZone zone)
    {
        int asteroidsSpawned = 0;
        int spawnAttempts = 0;
        int maxAttempts = 50;

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
                newPosition = zone.Center + RandVector() * zone.Radius;
                spawnAttempts++;
            } while (Vector3D.DistanceSquared(newPosition, zone.Center) < AsteroidSettings.MinDistanceFromPlayer * AsteroidSettings.MinDistanceFromPlayer && spawnAttempts < maxAttempts);

            if (spawnAttempts >= maxAttempts) break;

            Vector3D newVelocity;
            if (!AsteroidSettings.CanSpawnAsteroidAtPoint(newPosition, out newVelocity)) continue;

            if (IsNearVanillaAsteroid(newPosition))
            {
                Log.Info("Skipped spawning asteroid due to proximity to vanilla asteroid.");
                continue;
            }

            AsteroidType type = AsteroidSettings.GetAsteroidType(newPosition);
            float size = AsteroidSettings.GetAsteroidSize(newPosition);
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll((float)rand.NextDouble() * MathHelper.TwoPi, (float)rand.NextDouble() * MathHelper.TwoPi, (float)rand.NextDouble() * MathHelper.TwoPi);

            Log.Info($"Spawning asteroid at {newPosition} with velocity {newVelocity} of type {type}");
            var asteroid = AsteroidEntity.CreateAsteroid(newPosition, size, newVelocity, type, rotation);
            _asteroids.Add(asteroid);
            Log.Info($"Server: Added new asteroid with ID {asteroid.EntityId} to _asteroids list");

            var message = new AsteroidNetworkMessage(new Vector3D(newPosition.X, newPosition.Y, newPosition.Z), size, new Vector3D(newVelocity.X, newVelocity.Y, newVelocity.Z), Vector3D.Zero, type, false, asteroid.EntityId, false, true, rotation);
            _networkMessages.Add(message);
            asteroidsSpawned++;
        }
    }

    public void SendNetworkMessages()
    {
        if (_networkMessages.Count == 0) return;
        try
        {
            Log.Info($"Server: Preparing to send {_networkMessages.Count} network messages");

            foreach (var message in _networkMessages)
            {
                var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                Log.Info($"Server: Serialized message size: {messageBytes.Length} bytes");
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, messageBytes);
                Log.Info($"Server: Sent message for asteroid ID {message.EntityId}");
            }

            _networkMessages.Clear();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, typeof(AsteroidSpawner), "Failed to send network messages");
        }
    }

    private void RemoveAsteroid(AsteroidEntity asteroid)
    {
        if (_asteroids.Any(a => a.EntityId == asteroid.EntityId))
        {
            _despawnedAsteroids.Add(new AsteroidState
            {
                Position = asteroid.PositionComp.GetPosition(),
                Size = asteroid.Size,
                Type = asteroid.Type,
                EntityId = asteroid.EntityId
            });

            var removalMessage = new AsteroidNetworkMessage(asteroid.PositionComp.GetPosition(), asteroid.Size, Vector3D.Zero, Vector3D.Zero, asteroid.Type, false, asteroid.EntityId, true, false, Quaternion.Identity);
            var removalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(removalMessage);
            MyAPIGateway.Multiplayer.SendMessageToOthers(32000, removalMessageBytes);

            _asteroids.Remove(asteroid);
            MyEntities.Remove(asteroid);
            asteroid.Close();
            Log.Info($"Server: Removed asteroid with ID {asteroid.EntityId} from _asteroids list and MyEntities");
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
