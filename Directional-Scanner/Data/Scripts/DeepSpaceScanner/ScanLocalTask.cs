using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scanner.Data.Scripts.DeepSpaceScanner;
using VRage.ModAPI;
using VRage.Voxels;
using VRageMath;

namespace DeepSpaceScanner.Data.Scripts.DeepSpaceScanner
{
    public class ScanLocalTask
    {
        readonly Random _random = new Random();
        readonly double _scanStarted;
        double _lastChecked;
        readonly ScanRequest _request;
        public readonly ScanResponse Response;
        readonly ScanLogic _logic;

        public ScanLocalTask(ScanRequest request, ScanLogic logic)
        {
            _scanStarted = MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds;
            _request = request;
            _logic = logic;
            Response = new ScanResponse
            {
                SenderId = request.SenderId,
                EntityId = request.EntityId,
                Results = new List<ScanResult>()
            };
        }

        public void Start()
        {
            MyAPIGateway.Parallel.Start(StartScan);
        }

        void StartScan()
        {
            var deviation = ModConfig.MaxDistanceDeviation * 2 * _random.NextDouble();
            var multiplier = 1 + ModConfig.MaxDistanceDeviation - deviation;
            var maxDistance = Math.Max(ModConfig.AsteroidScanMaxDistance * _request.Strength / 100 * _logic.GridSize, 1);

            var distance = maxDistance * multiplier;
            var distanceM = distance * 1000;
            var distance2 = Math.Pow(distanceM, 2);

            var position = _logic.Entity.PositionComp.GetPosition();
            var view = _logic.ViewMatrix;
            var perspective = MatrixD.CreatePerspectiveFieldOfView(ModConfig.ScanAngle * Math.PI / 180, 1, 1, distanceM);
            var frustum = new BoundingFrustumD(view * perspective);

            var toCheck = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(toCheck, entity => entity is MyVoxelBase && !(entity is MyPlanet));

            foreach (MyVoxelBase v in toCheck)
            {
                try
                {
                    if (Vector3D.DistanceSquared(v.PositionComp.GetPosition(), position) > distance2) continue;
                    var aabb = v.PositionComp.WorldAABB;

                    ContainmentType containmentType;
                    frustum.Contains(ref aabb, out containmentType);
                    if (containmentType == ContainmentType.Disjoint) continue;
                    if (!_logic.ModuleScanActive) return;
                    
                    var storage = (IMyStorage) v.Storage;
                    var result = new ScanResult
                    {
                        Name = "Asteroid",
                        Size = "Stone",
                        Signature = $"{(uint) (storage.Size.Length() / 10)}",
                        Distance = $"{Vector3D.Distance(position, aabb.Center) / 1000:n2}km",
                        Signal = "Located",
                        Location = aabb.Center
                    };

                    var results = new List<ScanResult>(Response.Results) {result};
                    Response.Results = results;

                    storage.PinAndExecute(() =>
                    {
                        var lodIndex = 2;
                        var max = (storage.Size >> lodIndex) - 1;
                        var data = new MyStorageData();
                        var mats = new HashSet<byte>();

                        // TODO read from center
                        data.Resize(storage.Size);
                        storage.ReadRange(data, MyStorageDataTypeFlags.Material, lodIndex, Vector3I.Zero, max);

                        var index = Vector3I.Zero;
                        for (index.Z = 0; index.Z < max.Z; index.Z++)
                        for (index.Y = 0; index.Y < max.Y; index.Y++)
                        for (index.X = 0; index.X < max.X; index.X++)
                        {
                            if (!_logic.ModuleScanActive) return;
                            var mat = data.Material(ref index);
                            if (mat == MyVoxelConstants.NULL_MATERIAL) continue;
                            if (!mats.Add(mat)) continue;
                            result.Size = String.Join(", ",
                                mats.Select(x => MyDefinitionManager.Static.GetVoxelMaterialDefinition(x)).Where(x => x != null).Select(x => x.MinedOre));
                            // stop scanning at 4 mats including stone
                            if (mats.Count > 3) return;
                        }
                    });
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}