using System;
using Sandbox.ModAPI;
using Scanner.Data.Scripts.DeepSpaceScanner;
using VRageMath;

namespace DeepSpaceScanner
{
    public partial class ScanLogic
    {
        const float RotateRenderStep = 0.25f;

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (MyAPIGateway.Utilities.IsDedicated) return;
            if (MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds - _scanStarted > ModConfig.ScanDuration + 1000) ModuleScanActive = false;

            try
            {
                if (Subpart2 == null) return;
                SetParent();
                
                if (NextPitch == Pitch && NextYaw == Yaw) return;
                
                var k2 = -NextYaw > Yaw ? RotateRenderStep : -RotateRenderStep;
                Yaw = k2 > 0 ? Math.Min(-NextYaw, Yaw + k2) : Math.Max(-NextYaw, Yaw + k2);
                var transformYaw = RotateYaw(Yaw);
                var sub1Matrix = transformYaw * _block.PositionComp.LocalMatrixRef;
                Subpart1.Render?.UpdateRenderObjectLocal(sub1Matrix);
                
                var k = NextPitch > Pitch ? RotateRenderStep : -RotateRenderStep;
                Pitch = k > 0 ? Math.Min(NextPitch, Pitch + k) : Math.Max(NextPitch, Pitch + k);
                var transformPitch = RotatePitch(Pitch);
                Subpart2.Render?.UpdateRenderObjectLocal(transformPitch * sub1Matrix);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        public Matrix RotateYaw(float yaw)
        {
            yaw = MathHelper.ToRadians(yaw);
            var rotationMatrixY = Matrix.CreateRotationY(yaw);
            rotationMatrixY.Translation = Subpart1.PositionComp.LocalMatrixRef.Translation;
            Subpart1.PositionComp.SetLocalMatrix(ref rotationMatrixY, Subpart1.Physics);
            return rotationMatrixY;
        }

        public Matrix RotatePitch(float pitch)
        {
            pitch = MathHelper.ToRadians(pitch);
            var rotationMatrixX = Matrix.CreateRotationX(pitch);
            rotationMatrixX.Translation = Subpart2.PositionComp.LocalMatrixRef.Translation;
            Subpart2.PositionComp.SetLocalMatrix(ref rotationMatrixX, Subpart2.Physics);
            return rotationMatrixX;
        }

        void SetParent()
        {
            if (_block.Render.ParentIDs.Length == 0) return;
            if (_prevGrid == _block.CubeGrid) return;

            Subpart1.Render.SetParent(0, _block.Render.ParentIDs[0]);
            Subpart1.NeedsWorldMatrix = false;
            Subpart1.InvalidateOnMove = false;

            Subpart2.Render.SetParent(0, _block.Render.ParentIDs[0]);
            Subpart2.NeedsWorldMatrix = false;
            Subpart2.InvalidateOnMove = false;

            _prevGrid = _block.CubeGrid;
        }
    }
}