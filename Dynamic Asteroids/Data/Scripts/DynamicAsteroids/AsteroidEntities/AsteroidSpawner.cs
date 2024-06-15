﻿using DynamicAsteroids.AsteroidEntities;
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
    public int AsteroidCount { get; set; }

    public AsteroidZone(Vector3D center, double radius)
    {
        Center = center;
        Radius = radius;
        AsteroidCount = 0;
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
    private const double ZONE_RADIUS = 10000;

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

        // Create a new dictionary to store the updated zones
        Dictionary<long, AsteroidZone> updatedZones = new Dictionary<long, AsteroidZone>();

        foreach (var player in players)
        {
            Vector3D playerPosition = player.GetPosition();

            // Check if the player already has a zone assigned
            AsteroidZone existingZone;
            if (playerZones.TryGetValue(player.IdentityId, out existingZone))
            {
                // If the player's position is still within the existing zone, keep the zone
                if (existingZone.IsPointInZone(playerPosition))
                {
                    updatedZones[player.IdentityId] = existingZone;
                }
                else
                {
                    // If the player's position is outside the existing zone, create a new zone
                    AsteroidZone newZone = new AsteroidZone(playerPosition, ZONE_RADIUS);
                    updatedZones[player.IdentityId] = newZone;
                }
            }
            else
            {
                // If the player doesn't have a zone assigned, create a new zone
                AsteroidZone newZone = new AsteroidZone(playerPosition, ZONE_RADIUS);
                updatedZones[player.IdentityId] = newZone;
            }
        }

        // Update the playerZones dictionary with the updated zones
        playerZones = updatedZones;
    }

    public void MergeZones()
    {
        List<AsteroidZone> mergedZones = new List<AsteroidZone>();

        foreach (var zone in playerZones.Values)
        {
            bool merged = false;

            foreach (var mergedZone in mergedZones)
            {
                double distance = Vector3D.Distance(zone.Center, mergedZone.Center);
                double combinedRadius = zone.Radius + mergedZone.Radius;

                if (distance <= combinedRadius)
                {
                    // Merge the zones by updating the center and radius of the merged zone
                    Vector3D newCenter = (zone.Center + mergedZone.Center) / 2;
                    double newRadius = Math.Max(zone.Radius, mergedZone.Radius) + distance / 2;
                    mergedZone.Center = newCenter;
                    mergedZone.Radius = newRadius;
                    mergedZone.AsteroidCount += zone.AsteroidCount;
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                // If the zone couldn't be merged with any existing merged zones, add it as a new merged zone
                mergedZones.Add(new AsteroidZone(zone.Center, zone.Radius) { AsteroidCount = zone.AsteroidCount });
            }
        }

        // Update the playerZones dictionary with the merged zones
        playerZones.Clear();
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        foreach (var mergedZone in mergedZones)
        {
            foreach (var player in players)
            {
                if (mergedZone.IsPointInZone(player.GetPosition()))
                {
                    playerZones[player.IdentityId] = mergedZone;
                    break;
                }
            }
        }
    }
    public void UpdateZones()
    {
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        // Create a new dictionary to store the updated zones
        Dictionary<long, AsteroidZone> updatedZones = new Dictionary<long, AsteroidZone>();

        foreach (var player in players)
        {
            Vector3D playerPosition = player.GetPosition();

            // Check if the player is within any of the existing zones
            bool playerInZone = false;
            foreach (var zone in playerZones.Values)
            {
                if (zone.IsPointInZone(playerPosition))
                {
                    playerInZone = true;
                    break;
                }
            }

            // If the player is not in any zone, create a new zone for them
            if (!playerInZone)
            {
                AsteroidZone newZone = new AsteroidZone(playerPosition, ZONE_RADIUS);
                updatedZones[player.IdentityId] = newZone;
            }
        }

        // Add any existing zones that still have players in them
        foreach (var kvp in playerZones)
        {
            if (players.Any(p => kvp.Value.IsPointInZone(p.GetPosition())))
            {
                updatedZones[kvp.Key] = kvp.Value;
            }
        }

        // Update the playerZones dictionary with the updated zones
        playerZones = updatedZones;
    }

    private int _spawnIntervalTimer = 0;
    private int _updateIntervalTimer = 0;

    public void UpdateTick()
    {
        if (!MyAPIGateway.Session.IsServer) return;

        AssignZonesToPlayers();
        MergeZones();
        UpdateZones();

        try
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            if (_updateIntervalTimer > 0)
            {
                _updateIntervalTimer--;
            }
            else
            {
                UpdateAsteroids(playerZones.Values.ToList());
                _updateIntervalTimer = AsteroidSettings.UpdateInterval;
            }

            if (_spawnIntervalTimer > 0)
            {
                _spawnIntervalTimer--;
            }
            else
            {
                SpawnAsteroids(playerZones.Values.ToList());
                _spawnIntervalTimer = AsteroidSettings.SpawnInterval;
            }

            foreach (var player in players)
            {
                Vector3D playerPosition = player.GetPosition();
                AsteroidZone zone;
                if (playerZones.TryGetValue(player.IdentityId, out zone))
                {
                    LoadAsteroidsInRange(playerPosition, zone);
                }
            }

            if (AsteroidSettings.EnableLogging)
                MyAPIGateway.Utilities.ShowNotification($"Active Asteroids: {_asteroids.Count}", 1000 / 60);
        }
        catch (Exception ex)
        {
            Log.Exception(ex, typeof(AsteroidSpawner));
        }
    }

    private void UpdateAsteroids(List<AsteroidZone> zones)
    {
        Log.Info($"Updating asteroids. Total asteroids: {_asteroids.Count}, Total zones: {zones.Count}");
        int removedCount = 0;

        foreach (var asteroid in _asteroids.ToArray())
        {
            bool inAnyZone = false;
            AsteroidZone currentZone = null;

            foreach (var zone in zones)
            {
                if (zone.IsPointInZone(asteroid.PositionComp.GetPosition()))
                {
                    inAnyZone = true;
                    currentZone = zone;
                    break;
                }
            }

            if (!inAnyZone)
            {
                Log.Info($"Removing asteroid at {asteroid.PositionComp.GetPosition()} due to distance from all player zones");
                RemoveAsteroid(asteroid);
                removedCount++;
            }
            else if (currentZone != null)
            {
                // Ensure the asteroid is counted in the correct zone
                foreach (var zone in zones)
                {
                    if (zone != currentZone && zone.IsPointInZone(asteroid.PositionComp.GetPosition()))
                    {
                        zone.AsteroidCount--;
                    }
                }
                currentZone.AsteroidCount++;
            }
        }

        Log.Info($"Update complete. Removed asteroids: {removedCount}, Remaining asteroids: {_asteroids.Count}");
        foreach (var zone in zones)
        {
            Log.Info($"Zone center: {zone.Center}, Radius: {zone.Radius}, Asteroid count: {zone.AsteroidCount}");
        }
    }

    private void SpawnAsteroids(List<AsteroidZone> zones)
    {
        const int MAX_ASTEROIDS_PER_ZONE = 1000;
        int totalSpawnAttempts = 0;
        int maxTotalAttempts = 100;

        Log.Info($"Attempting to spawn asteroids. Current total count: {_asteroids.Count}, Zones: {zones.Count}");

        foreach (var zone in zones)
        {
            int asteroidsSpawned = 0;
            int zoneSpawnAttempts = 0;
            int maxZoneAttempts = 50;

            while (zone.AsteroidCount < MAX_ASTEROIDS_PER_ZONE && asteroidsSpawned < 10 && zoneSpawnAttempts < maxZoneAttempts && totalSpawnAttempts < maxTotalAttempts)
            {
                Vector3D newPosition;
                do
                {
                    newPosition = zone.Center + RandVector() * zone.Radius;
                    zoneSpawnAttempts++;
                    totalSpawnAttempts++;
                } while (!IsValidSpawnPosition(newPosition, zones) && zoneSpawnAttempts < maxZoneAttempts && totalSpawnAttempts < maxTotalAttempts);

                if (zoneSpawnAttempts >= maxZoneAttempts || totalSpawnAttempts >= maxTotalAttempts) break;

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

                if (asteroid != null)
                {
                    _asteroids.Add(asteroid);
                    zone.AsteroidCount++;
                    Log.Info($"Server: Added new asteroid with ID {asteroid.EntityId} to _asteroids list");

                    var message = new AsteroidNetworkMessage(newPosition, size, newVelocity, Vector3D.Zero, type, false, asteroid.EntityId, false, true, rotation);
                    _networkMessages.Add(message);
                    asteroidsSpawned++;
                }
                else
                {
                    Log.Info($"Failed to create asteroid at position {newPosition}");
                }
            }

            Log.Info($"Zone spawn complete. Asteroids spawned: {asteroidsSpawned}, Zone spawn attempts: {zoneSpawnAttempts}, Zone asteroid count: {zone.AsteroidCount}");
        }

        Log.Info($"All zones spawn attempt complete. Total spawn attempts: {totalSpawnAttempts}, New total asteroid count: {_asteroids.Count}");
    }

    private bool IsValidSpawnPosition(Vector3D position, List<AsteroidZone> zones)
    {
        foreach (var zone in zones)
        {
            if (zone.IsPointInZone(position) &&
                Vector3D.DistanceSquared(position, zone.Center) >= AsteroidSettings.MinDistanceFromPlayer * AsteroidSettings.MinDistanceFromPlayer)
            {
                return true;
            }
        }
        return false;
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
