using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Scanner.Data.Scripts.DeepSpaceScanner;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace DeepSpaceScanner
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "DeepSpaceScannerL", "DeepSpaceScannerS")]
    public partial class ScanLogic : MyGameLogicComponent
    {
        MyObjectBuilder_EntityBase _ob;

        public MyResourceSinkComponent Sink;
        IMyCameraBlock _block;
        MyEntitySubpart _subpart1;
        MyEntitySubpart _subpart2;
        float _gridSize;
        IMyCubeGrid _prevGrid;

        public float GridSize => _gridSize;

        public MyEntitySubpart Subpart1
        {
            get
            {
                if (_subpart1 != null) return _subpart1;
                Entity.TryGetSubpart("LaserComTurret", out _subpart1);
                return _subpart1;
            }
        }

        public MyEntitySubpart Subpart2
        {
            get
            {
                if (_subpart2 != null) return _subpart2;
                Subpart1?.TryGetSubpart("LaserCom", out _subpart2);
                return _subpart2;
            }
        }


        public MatrixD ViewMatrix => Subpart2.PositionComp.GetViewMatrix();
        public MatrixD WorldMatrix => Subpart2.WorldMatrix;

        public float Pitch { get; private set; }

        public float Yaw { get; private set; }

        float NextPitch { get; set; }

        float NextYaw { get; set; }

        ScanResult SelectedResult { get; set; }

        public List<ScanResult> ScanResults = new List<ScanResult>();

        public float ScannerStrength { get; set; }

        public bool ShowPopup { get; set; }

        bool _moduleScanActive;

        double _scanStarted;

        public bool ScanAsteroids;

        public MyTuple<long, int> TextSurface;

        public bool ModuleScanActive
        {
            get { return _moduleScanActive; }
            set
            {
                _moduleScanActive = value;
                ModComponent.ScanActive = value;
                if (value) _scanStarted = MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds;
                else _scanStarted = 0;
            }
        }

        float Consumption => (float) (ModConfig.PowerLimits[0] + Math.Pow(ScannerStrength, 2) * ModConfig.PowerLimits[1] / 10000 * _gridSize);

        public float MaxDistance => (ModConfig.RangeLimits[0] + (ModConfig.RangeLimits[1] - ModConfig.RangeLimits[0]) * ScannerStrength / 100) * _gridSize;

        public override void Close()
        {
            _prevGrid = null;
            Entity.Components.Remove<MyResourceSinkComponent>();
            _block.AppendingCustomInfo -= AppendingCustomInfo;
            MyAPIGateway.Session.OnSessionReady -= OnSessionReady;
            _block = null;
            _subpart1 = null;
            _subpart2 = null;
            Sink = null;
            _moduleScanActive = false;
        }

        public override void Init(MyObjectBuilder_EntityBase ob)
        {
            base.Init(ob);
            _ob = ob;
            _block = Entity as IMyCameraBlock;
            _block.AppendingCustomInfo += AppendingCustomInfo;
            _gridSize = _block.CubeGrid.GridSize / 2.5f;
            Sink = _block.ResourceSink as MyResourceSinkComponent;
            Sink.SetMaxRequiredInputByType(ModConfig.E, ModConfig.PowerLimits[1] * _gridSize);
            Sink.SetRequiredInputFuncByType(ModConfig.E, GetRequiredPower);
            Sink.Update();

            if (!MyAPIGateway.Utilities.IsDedicated) NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            CreateTerminalControls();
            LoadBlockSettings();
            MyAPIGateway.Session.OnSessionReady += OnSessionReady;
        }

        float GetRequiredPower()
        {
            if (!_block.IsWorking) return 0f;
            if (ModuleScanActive) return Consumption;
            return ModConfig.PowerLimits[0] / 10 * _gridSize;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _ob;
        }

        static void AppendingCustomInfo(IMyTerminalBlock b, StringBuilder s)
        {
            var l = b.GameLogic.GetAs<ScanLogic>();
            if (l == null) return;
            var max = l.ScanAsteroids
                ? Math.Max(ModConfig.AsteroidScanMaxDistance * l.ScannerStrength / 100 * l.GridSize, 1)
                : l.MaxDistance;
            s.AppendLine($"Max distance: {max * (1 - ModConfig.MaxDistanceDeviation):##,###} - {max * (1 + ModConfig.MaxDistanceDeviation):##,###} km");
            s.AppendLine($"Power required: {l.Consumption:n3} MWt");
            s.AppendLine($"Available: {l.Sink.ResourceAvailableByType(ModConfig.E):n3} MWt");
        }

        public static void RefreshCustomInfo(IMyTerminalBlock block)
        {
            block.RefreshCustomInfo();
            var b2 = block as MyCubeBlock;
            if (b2.IDModule == null) return;
            var m = b2.IDModule.ShareMode;
            b2.ChangeOwner(block.OwnerId, m == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.Faction : MyOwnershipShareModeEnum.None);
            b2.ChangeOwner(block.OwnerId, m);
        }

        void OnSessionReady()
        {
            RefreshCustomInfo(_block);
        }
    }
}