using DynamicAsteroids.AsteroidEntities;
using System;
using VRageMath;

namespace DynamicAsteroids
{
    internal static class AsteroidSettings
    {
        public static int MaxAsteroidCount = 1000;
        public static int AsteroidSpawnRadius = 10000;
        public static int AsteroidVelocityBase = 0;
        public static double VelocityVariability = 10; // New setting for velocity variability
        public static double AngularVelocityVariability = 0.1; // New setting for angular velocity variability

        // Weights for asteroid type frequencies
        public static double IceWeight = 0.45; // 45%
        public static double StoneWeight = 0.45; // 45%
        public static double IronWeight = 0.01;
        public static double NickelWeight = 0.01;
        public static double CobaltWeight = 0.01;
        public static double MagnesiumWeight = 0.01;
        public static double SiliconWeight = 0.01;
        public static double SilverWeight = 0.01;
        public static double GoldWeight = 0.01;
        public static double PlatinumWeight = 0.01;
        public static double UraniniteWeight = 0.01;

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

        public static AsteroidType GetRandomAsteroidType(Random rand)
        {
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
            randomValue -= PlatinumWeight;
            return AsteroidType.Uraninite;
        }
    }

    internal class SpawnableArea
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

            // squared is more performant
            if (pointDistanceSq > Radius * Radius || pointDistanceSq < InnerRadius * InnerRadius)
                return false;

            // Calculate HeightFromCenter
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
