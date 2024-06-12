using DynamicAsteroids.AsteroidEntities;
using System;
using VRageMath;

namespace DynamicAsteroids
{
    public static class AsteroidSettings
    {
        public static bool EnableLogging = false;
        public static bool EnablePersistence = false; //barely works, don't touch this
        public static bool EnableMiddleMouseAsteroidSpawn = false;  //debug


        public static int MaxAsteroidCount = 1000;
        public static int AsteroidSpawnRadius = 10000;
        //TODO: make these velocities only affect a % of asteroids with an option
        //note: these are absolutely awful for performance, thousands of moving entities etc.
        public static int AsteroidVelocityBase = 0;
        public static double VelocityVariability = 0;
        public static double AngularVelocityVariability = 0;

        public static double MinDistanceFromVanillaAsteroids = 1000; // 1 km
        public static double MinDistanceFromPlayer = 1; // Minimum distance from the player to spawn new asteroids
        public static int Seed = 69420; // Default seed, can be set dynamically

        public static double IceWeight = 99;
        public static double StoneWeight = 0.5;  // Represents silicate materials
        public static double IronWeight = 0.25;
        public static double NickelWeight = 0.05;
        public static double CobaltWeight = 0.05;
        public static double MagnesiumWeight = 0.05;
        public static double SiliconWeight = 0.05;
        public static double SilverWeight = 0.05;
        public static double GoldWeight = 0.05;
        public static double PlatinumWeight = 0.05;
        public static double UraniniteWeight = 0.05;

        public static float BaseIntegrity = 1f;
        public static float MinAsteroidSize = 50f;
        public static float MaxAsteroidSize = 250f;
        public static float MinSubChunkSize = 5f;

        public static double SubChunkVelocityMin = 1.0;
        public static double SubChunkVelocityMax = 5.0;
        public static double SubChunkAngularVelocityMin = 0.01;
        public static double SubChunkAngularVelocityMax = 0.1;

        public static int[] IceDropRange = { 1000, 10000 };
        public static int[] StoneDropRange = { 1000, 10000 };
        public static int[] IronDropRange = { 500, 2500 };
        public static int[] NickelDropRange = { 500, 2500 };
        public static int[] CobaltDropRange = { 500, 2500 };
        public static int[] MagnesiumDropRange = { 500, 2500 };
        public static int[] SiliconDropRange = { 500, 2500 };
        public static int[] SilverDropRange = { 500, 2500 };
        public static int[] GoldDropRange = { 500, 2500 };
        public static int[] PlatinumDropRange = { 500, 2500 };
        public static int[] UraniniteDropRange = { 500, 2500 };

        public static SpawnableArea[] ValidSpawnLocations =
        {
        new SpawnableArea
        {
            CenterPosition = new Vector3D(148001024.50, 1024.50, 1024.50),
            Normal = new Vector3D(1, 10, 0.5).Normalized(),
            Radius = 60268000 * 2.5,
            InnerRadius = 60268000 * 1.2,
            HeightFromCenter = 1000,
        }
    };

        public static bool CanSpawnAsteroidAtPoint(Vector3D point, out Vector3D velocity)
        {
            foreach (var area in ValidSpawnLocations)
            {
                if (area.ContainsPoint(point))
                {
                    velocity = area.VelocityAtPoint(point);
                    return true;
                }
            }

            velocity = Vector3D.Zero;
            return false;
        }

        public static bool PlayerCanSeeRings(Vector3D point)
        {
            foreach (var area in ValidSpawnLocations)
                if (area.ContainsPoint(point))
                    return true;
            return false;
        }

        public static AsteroidType GetAsteroidType(Vector3D position)
        {
            Random rand = new Random(Seed + position.GetHashCode());

            double totalWeight = IceWeight + StoneWeight + IronWeight + NickelWeight + CobaltWeight + MagnesiumWeight +
                                 SiliconWeight + SilverWeight + GoldWeight + PlatinumWeight + UraniniteWeight;
            double randomValue = rand.NextDouble() * totalWeight;

            if (randomValue < IceWeight) return AsteroidType.Ice;
            randomValue -= IceWeight;
            if (randomValue < StoneWeight) return AsteroidType.Stone;
            randomValue -= StoneWeight;
            if (randomValue < IronWeight) return AsteroidType.Iron;
            randomValue -= IronWeight;
            if (randomValue < NickelWeight) return AsteroidType.Nickel;
            randomValue -= NickelWeight;
            if (randomValue < CobaltWeight) return AsteroidType.Cobalt;
            randomValue -= CobaltWeight;
            if (randomValue < MagnesiumWeight) return AsteroidType.Magnesium;
            randomValue -= MagnesiumWeight;
            if (randomValue < SiliconWeight) return AsteroidType.Silicon;
            randomValue -= SiliconWeight;
            if (randomValue < SilverWeight) return AsteroidType.Silver;
            randomValue -= SilverWeight;
            if (randomValue < GoldWeight) return AsteroidType.Gold;
            randomValue -= GoldWeight;
            if (randomValue < PlatinumWeight) return AsteroidType.Platinum;
            return AsteroidType.Uraninite;
        }

        public static float GetAsteroidSize(Vector3D position)
        {
            Random rand = new Random(Seed + position.GetHashCode());
            return MinAsteroidSize + (float)rand.NextDouble() * (MaxAsteroidSize - MinAsteroidSize);
        }

        public static double GetRandomAngularVelocity(Random rand)
        {
            return AngularVelocityVariability * rand.NextDouble();
        }

        public static double GetRandomSubChunkVelocity(Random rand)
        {
            return SubChunkVelocityMin + rand.NextDouble() * (SubChunkVelocityMax - SubChunkVelocityMin);
        }

        public static double GetRandomSubChunkAngularVelocity(Random rand)
        {
            return SubChunkAngularVelocityMin + rand.NextDouble() * (SubChunkAngularVelocityMax - SubChunkAngularVelocityMin);
        }
    }

    public class SpawnableArea
    {
        public Vector3D CenterPosition;
        public Vector3D Normal;
        public double Radius;
        public double InnerRadius;
        public double HeightFromCenter;

        public bool ContainsPoint(Vector3D point)
        {
            point -= CenterPosition;
            double pointDistanceSq = point.LengthSquared();

            if (pointDistanceSq > Radius * Radius || pointDistanceSq < InnerRadius * InnerRadius)
                return false;

            if (Math.Abs(Vector3D.Dot(point, Normal)) > HeightFromCenter)
                return false;

            return true;
        }

        public Vector3D VelocityAtPoint(Vector3D point)
        {
            return -(point - CenterPosition).Cross(Normal).Normalized() * AsteroidSettings.AsteroidVelocityBase;
        }
    }
}
