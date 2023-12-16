using System;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scanner.Data.Scripts.DeepSpaceScanner;
using VRage.Utils;

namespace DeepSpaceScanner
{
    public partial class GridLogic
    {
        int _frame;
        double _lastUpdated;
        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (_frame++ % ModConfig.SignatureUpdateFrequency != 0) return;
            _frame = 1;
            
            var ctrl = MyAPIGateway.Session.ControlledObject;
            if (!(ctrl is MyShipController)) return;
            var l = (ctrl as MyShipController).CubeGrid.GameLogic.GetAs<GridLogic>();
            if (l == null) return;
            UpdateSignature();
        }

        uint BlocksSignature {
            get
            {
                var blocksSignature = _grid.BlocksCount * _gridSize * ModConfig.SignatureBlocksMultiplier;
                var pcuSignature = _grid.BlocksPCU * _gridSize * ModConfig.SignaturePcuMultiplier;
                var pb = blocksSignature + pcuSignature;
                return (uint) pb;
            }
        }

        void UpdateSignature()
        {
            MyAPIGateway.Parallel.Start(() => CalculateSignature());
        }

        public uint CalculateSignature()
        {
            try
            {
                if ((MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds - _lastUpdated) / 16 < ModConfig.SignatureUpdateFrequency) return _signature;
                var powerSignature = _producers.Keys.Sum(x => x.CurrentOutput) * ModConfig.SignaturePowerMultiplier;
                var thrustSignature = _thrusters.Keys.Sum(x => x.CurrentThrust) / 1000 * ModConfig.SignatureThermalMultiplier;
                
                var pt = powerSignature + thrustSignature;
                var pb = BlocksSignature;
                _signature = (uint) (pb + pt);
                _lastUpdated = MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return _signature;
        }
    }
}