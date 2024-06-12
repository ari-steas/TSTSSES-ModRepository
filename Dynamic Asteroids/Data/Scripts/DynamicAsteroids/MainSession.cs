using System;
using System.Collections.Generic;
using DynamicAsteroids.AsteroidEntities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SC.SUGMA;
using VRage.Game.Components;
using VRage.Input;
using VRageMath;
using ProtoBuf;
using Sandbox.Game.Entities;

namespace DynamicAsteroids
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MainSession : MySessionComponentBase
    {
        public static MainSession I;
        public Random Rand;
        private int seed;
        public AsteroidSpawner _spawner = new AsteroidSpawner();
        private int _saveStateTimer;
        private int _networkMessageTimer;


        public override void LoadData()
        {
            I = this;
            Log.Init();

            try
            {
                Log.Info("Loading data in MainSession");
                if (MyAPIGateway.Session.IsServer)
                {
                    seed = (int)DateTime.UtcNow.Ticks; // Example seed based on current time
                    AsteroidSettings.Seed = seed;
                    Rand = new Random(seed);
                    _spawner.Init(seed);
                    if (AsteroidSettings.EnablePersistence) // Add this line
                    {
                        _spawner.LoadAsteroidState(); // Load asteroid states
                    }
                }

                MyAPIGateway.Multiplayer.RegisterMessageHandler(32000, OnMessageReceived);
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
                Log.Info("Unloading data in MainSession");
                if (MyAPIGateway.Session.IsServer)
                {
                    if (AsteroidSettings.EnablePersistence) // Add this line
                    {
                        _spawner.SaveAsteroidState(); // Save asteroid states
                    }
                    _spawner.Close();
                }

                MyAPIGateway.Multiplayer.UnregisterMessageHandler(32000, OnMessageReceived);
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
                if (MyAPIGateway.Session.IsServer)
                {
                    _spawner.UpdateTick();

                    // Save asteroid states periodically
                    if (_saveStateTimer > 0)
                    {
                        _saveStateTimer--;
                    }
                    else
                    {
                        _spawner.SaveAsteroidState();
                        _saveStateTimer = AsteroidSettings.SaveStateInterval; // Use setting
                    }

                    // Batch and delay network messages
                    if (_networkMessageTimer > 0)
                    {
                        _networkMessageTimer--;
                    }
                    else
                    {
                        _spawner.SendNetworkMessages();
                        _networkMessageTimer = AsteroidSettings.NetworkMessageInterval; // Use setting
                    }
                }

                if (MyAPIGateway.Session?.Player?.Character != null && _spawner._asteroids != null)
                {
                    Vector3D characterPosition = MyAPIGateway.Session.Player.Character.PositionComp.GetPosition();
                    AsteroidEntity nearestAsteroid = FindNearestAsteroid(characterPosition);

                    if (nearestAsteroid != null)
                    {
                        Vector3D angularVelocity = nearestAsteroid.Physics.AngularVelocity;
                        string rotationString = $"({angularVelocity.X:F2}, {angularVelocity.Y:F2}, {angularVelocity.Z:F2})";

                        string message = $"Nearest Asteroid: {nearestAsteroid.EntityId} ({nearestAsteroid.Type})\nRotation: {rotationString}";
                        if (AsteroidSettings.EnableLogging)
                            MyAPIGateway.Utilities.ShowNotification(message, 1000 / 60);
                    }
                }

                if (AsteroidSettings.EnableMiddleMouseAsteroidSpawn && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.MiddleButton))
                {
                    if (MyAPIGateway.Session != null)
                    {
                        var position = MyAPIGateway.Session.Player?.GetPosition() ?? Vector3D.Zero;
                        var velocity = MyAPIGateway.Session.Player?.Character?.Physics?.LinearVelocity ?? Vector3D.Zero;
                        AsteroidType type = DetermineAsteroidType(); // Determine the type of asteroid
                        AsteroidEntity.CreateAsteroid(position, Rand.Next(50), velocity, type);
                        Log.Info($"Asteroid created at {position} with velocity {velocity}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(MainSession));
            }
        }

        private void OnMessageReceived(byte[] message)
        {
            try
            {
                var asteroidMessages = MyAPIGateway.Utilities.SerializeFromBinary<List<AsteroidNetworkMessage>>(message);

                foreach (var asteroidMessage in asteroidMessages)
                {
                    Log.Info($"Client: Received message to create/remove asteroid at {asteroidMessage.Position} with velocity {asteroidMessage.InitialVelocity} of type {asteroidMessage.Type}");

                    if (asteroidMessage.IsRemoval)
                    {
                        // Find and remove the asteroid with the given EntityId
                        var asteroid = MyEntities.GetEntityById(asteroidMessage.EntityId) as AsteroidEntity;
                        if (asteroid != null)
                        {
                            asteroid.Close();
                        }
                    }
                    else if (asteroidMessage.IsInitialCreation)
                    {
                        var asteroid = AsteroidEntity.CreateAsteroid(asteroidMessage.Position, asteroidMessage.Size, asteroidMessage.InitialVelocity, asteroidMessage.Type);
                        asteroid.Physics.AngularVelocity = asteroidMessage.AngularVelocity;
                        MyEntities.Add(asteroid);
                    }
                    else
                    {
                        if (asteroidMessage.IsSubChunk)
                        {
                            // Create the sub-chunk asteroid on the client
                            var subChunk = AsteroidEntity.CreateAsteroid(asteroidMessage.Position, asteroidMessage.Size, asteroidMessage.InitialVelocity, asteroidMessage.Type);
                            subChunk.Physics.AngularVelocity = asteroidMessage.AngularVelocity;
                        }
                        else
                        {
                            // Create the regular asteroid on the client
                            var asteroid = AsteroidEntity.CreateAsteroid(asteroidMessage.Position, asteroidMessage.Size, asteroidMessage.InitialVelocity, asteroidMessage.Type);
                            asteroid.Physics.AngularVelocity = asteroidMessage.AngularVelocity;
                            MyEntities.Add(asteroid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(MainSession));
            }
        }

        private AsteroidEntity FindNearestAsteroid(Vector3D characterPosition)
        {
            if (_spawner._asteroids == null) return null;

            AsteroidEntity nearestAsteroid = null;
            double minDistance = double.MaxValue;

            foreach (var asteroid in _spawner._asteroids)
            {
                double distance = Vector3D.DistanceSquared(characterPosition, asteroid.PositionComp.GetPosition());
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestAsteroid = asteroid;
                }
            }

            return nearestAsteroid;
        }

        // This function determines the type of asteroid to spawn
        private AsteroidType DetermineAsteroidType()
        {
            int randValue = Rand.Next(0, 2); // Adjust as needed for more types
            return (AsteroidType)randValue;
        }

    }
}
