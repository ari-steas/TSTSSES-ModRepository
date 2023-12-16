using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scanner.Data.Scripts.DeepSpaceScanner;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace DeepSpaceScanner
{
    public class ScanTask
    {
        readonly Random _random = new Random();
        readonly double _scanStarted;
        double _lastChecked;
        readonly ScanRequest _request;
        public readonly ScanResponse Response;
        readonly ScanLogic _logic;

        public ScanTask(ScanRequest request, ScanLogic logic)
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

        public bool IsComplete => Response.Error != null || _scanStarted + ModConfig.ScanDuration < MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds;

        public void Start()
        {
            MyAPIGateway.Utilities.InvokeOnGameThread(Prepare);
        }

        void Prepare()
        {
            try
            {
                var sink = _logic.Sink;
                if (sink == null) return;
                _logic.RotatePitch(_request.Pitch);
                _logic.RotateYaw(_request.Yaw);
                _logic.ScannerStrength = _request.Strength;
                sink.IsPoweredChanged += IsPoweredChanged;
                sink.Update();
                _logic.ModuleScanActive = sink.IsPoweredByType(ModConfig.E);
                if (_logic.ModuleScanActive)
                {
                    if (_request.ScanAsteroids) return;
                    MyAPIGateway.Parallel.Start(StartScan);
                }
                else IsPoweredChanged();
            }
            catch (Exception e)
            {
                Response.Error = $"{e.Message}";
                Log.Error(e);
                _logic.ModuleScanActive = false;
            }
        }

        void IsPoweredChanged()
        {
            var sink = _logic.Sink;
            if (sink == null) return;
            sink.Update();
            _logic.ModuleScanActive = sink.IsPoweredByType(ModConfig.E);
            if (_logic.ModuleScanActive) return;
            sink.IsPoweredChanged -= IsPoweredChanged;
            Response.Error = "Insufficient power";
        }

        public void Finish()
        {
            var sink = _logic.Sink;
            if (sink == null) return;
            sink.IsPoweredChanged -= IsPoweredChanged;
            _logic.ModuleScanActive = false;
            sink.Update();
        }

        void StartScan()
        {
            try
            {
                var deviation = ModConfig.MaxDistanceDeviation * 2 * _random.NextDouble();
                var multiplier = 1 + ModConfig.MaxDistanceDeviation - deviation;
                var max = _logic.MaxDistance;

                var distance = max * multiplier;
                var distanceM = distance * 1000;
                var distance2 = Math.Pow(distanceM, 2);

                var position = _logic.Entity.PositionComp.GetPosition();
                var view = _logic.ViewMatrix;
                var perspective = MatrixD.CreatePerspectiveFieldOfView(ModConfig.ScanAngle * Math.PI / 180, 1, 1, distanceM);
                var frustum = new BoundingFrustumD(view * perspective);

                var current = _logic.Entity.GetTopMostParent() as IMyCubeGrid;
                var currentGroup = new HashSet<IMyCubeGrid>(MyAPIGateway.GridGroups.GetGroup(current, GridLinkTypeEnum.Physical));
                var toCheck = new HashSet<IMyEntity>();
                var @checked = new HashSet<IMyCubeGrid>();

                MyAPIGateway.Entities.GetEntities(toCheck, entity => entity is IMyCubeGrid && !currentGroup.Contains(entity));

                foreach (MyCubeGrid grid in toCheck)
                {
                    try
                    {
                        if (!_logic.ModuleScanActive) return;

                        if (@checked.Contains(grid)) continue;
                        var group = MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Physical);
                        group.ForEach(x => @checked.Add(x));
                        if (Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), _logic.Entity.PositionComp.GetPosition()) > distance2) continue;

                        var aabb = grid.GetPhysicalGroupAABB();
                        ContainmentType containmentType;
                        frustum.Contains(ref aabb, out containmentType);
                        if (containmentType == ContainmentType.Disjoint) continue;

                        var signature = (uint) group.Sum(x => x.GameLogic.GetAs<GridLogic>()?.CalculateSignature() ?? 0);
                        if (signature < 1) continue;

                        var gridDistanceM = Vector3D.Distance(position, aabb.Center);
                        if (IsObstructedByVoxelBody(aabb, gridDistanceM)) continue;

                        var gridDistance = Math.Max((uint) gridDistanceM / 1000, 1);
                        var powerMultiplier = ModConfig.MaxSignatureMultiplier - (ModConfig.MaxSignatureMultiplier - 1) * gridDistance / distance;
                        var multipliedSignature = powerMultiplier * signature;

                        // grid with 400+m signature should be 100+% scannable at <200km
                        var distanceRatio = (float) 300 / (gridDistance + 100); // 200% at 50km, 100% at 200km, 27% at 1000km, 15% at 2000km
                        var signatureRatio = (float) Math.Pow(multipliedSignature, 0.5) / 20; // 50% at 100m, 100% at 400m, 200% at 1600m
                        var signal = distanceRatio * signatureRatio;

                        // TODO reduce signal for small grid scanner? 
                        // if (_gridSize < 1) signal *= 0.5f;

                        if (signal < 0.15)
                        {
                            if (_random.NextDouble() < 0.65) continue;
                            Response.Results.Add(new ScanResult {Signal = "Weak"});
                        }
                        else if (signal < 0.5)
                        {
                            if (_random.NextDouble() < 0.3) continue;
                            var dist = _random.Next((int) (gridDistance * signal), (int) (gridDistance * (2f - signal)));
                            Response.Results.AddRange(group.Select(x => new ScanResult
                                {
                                    Signal = "Moderate",
                                    NumDistance = dist
                                }
                            ));
                        }
                        else if (signal < 0.75)
                        {
                            if (_random.NextDouble() < 0.05) continue;
                            var dist = _random.Next((int) (gridDistance * signal), (int) (gridDistance * (2f - signal)));
                            Response.Results.AddRange(group.Select(x => new ScanResult
                            {
                                Signal = "Strong",
                                Size = $"{grid.GridSizeEnum}",
                                Name = x.DisplayName,
                                NumDistance = dist
                            }));
                        }
                        else if (signal < 1)
                        {
                            var dist = _random.Next((int) (gridDistance * signal), (int) (gridDistance * (2f - signal)));
                            Response.Results.AddRange(group.Select(x => new ScanResult
                            {
                                Signal = "Very Strong",
                                Size = $"{grid.GridSizeEnum}",
                                Name = x.DisplayName,
                                NumDistance = dist
                            }));
                        }
                        else
                        {
                            Response.Results.AddRange(group.Select(x => new ScanResult
                            {
                                Signal = "Located",
                                Name = x.DisplayName,
                                Size = $"{grid.GridSizeEnum}",
                                Signature = $"{multipliedSignature:##,###}",
                                Distance = $"{gridDistance:##,###}km",
                                NumDistance = (int) gridDistance,
                                Location = grid.PositionComp.GetPosition()
                            }));
                        }

                        if (!ModConfig.NotifyWhenScanned) continue;
                        group.ForEach(x =>
                        {
                            var player = MyAPIGateway.Multiplayer.Players.GetPlayerControllingEntity(x);
                            if (player != null) ModComponent.SendResponse(new Scanned {SenderId = player.SteamUserId});
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                Response.Results.Sort((x, y) => x.NumDistance - y.NumDistance);
            }
            catch (Exception e)
            {
                Response.Error = e.Message;
                Log.Error(e);
            }
        }

        bool IsObstructedByVoxelBody(BoundingBoxD gridAABB, double gridDistance)
        {
            if (gridDistance == null || gridDistance == 0) return true;
            var pos = _logic.WorldMatrix.Translation;
            var lineToGrid = new LineD(pos, gridAABB.Center);

            var voxels = new List<MyLineSegmentOverlapResult<MyVoxelBase>>();
            MyGamePruningStructure.GetVoxelMapsOverlappingRay(ref lineToGrid, voxels);
            if (voxels.Count == 0) return false;

            var gridArea = gridAABB.ProjectedArea(lineToGrid.Direction);
            var hiddenCorners = new HashSet<Vector3>();

            foreach (var x in voxels)
            {
                var p = x.Element as MyPlanet;
                if (p != null)
                {
                    var planetToGrid = gridAABB.Center - p.PositionComp.WorldAABB.Center;
                    // inside
                    if (planetToGrid.Length() <= p.MaximumRadius) return true;

                    var scannerToPlanet = p.PositionComp.WorldAABB.Center - pos;
                    // in front of
                    if (gridDistance < scannerToPlanet.Length()) continue;

                    var planetToGridNorm = planetToGrid;
                    planetToGridNorm.Normalize();
                    var scannerToPlanetNorm = scannerToPlanet;
                    scannerToPlanetNorm.Normalize();

                    // in front of
                    if (planetToGridNorm.Dot(scannerToPlanetNorm) < 0) continue;

                    var plane = new PlaneD(p.PositionComp.WorldAABB.Center, scannerToPlanetNorm);
                    var distance = Vector3D.Distance(plane.Intersection(ref pos, ref lineToGrid.Direction), p.PositionComp.WorldAABB.Center);
                    // behind
                    if (distance <= p.MaximumRadius) return true;
                    continue;
                }

                var bodyArea = x.Element.PositionComp.WorldAABB.ProjectedArea(lineToGrid.Direction);
                if (bodyArea < gridArea) continue;

                var containment = x.Element.PositionComp.WorldAABB.Contains(gridAABB);
                if (containment == ContainmentType.Contains) return true;

                foreach (var c in gridAABB.GetCorners())
                {
                    if (hiddenCorners.Contains(c)) continue;
                    var l = new LineD(pos, c);
                    Vector3D? v;
                    x.Element.GetIntersectionWithLine(ref l, out v);
                    if (!v.HasValue) continue;

                    hiddenCorners.Add(c);
                    if (hiddenCorners.Count == 8) return true;
                }
            }

            return false;
        }
    }
}