using VRageMath;

namespace DynamicAsteroids
{
    internal static class AsteroidSettings
    {
        public static int MaxAsteroidCount = 500;
        public static int AsteroidSpawnRadius = 10000;

        public static SpawnableArea[] ValidSpawnLocations =
        {
            new SpawnableArea
            {
                CenterPosition = new Vector3D(148001024.50, 1024.50, 1024.50),
                Normal = new Vector3D(1, 10, 0.5).Normalized(),
                Radius = 60268000 * 2.5,
                InnerRadius = 60268000 * 1.2,
                HeightFromCenter = 25000,
            }
        };

        public static bool SpawnAsteroidsAtPoint(Vector3D point)
        {
            foreach (var area in ValidSpawnLocations)
                if (area.ContainsPoint(point))
                    return true;
            return false;
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

            if (pointDistanceSq > Radius * Radius || pointDistanceSq < InnerRadius * InnerRadius)
                return false;

            // TODO: Normal and HeightFromCenter

            return true;
        }
    }
}
