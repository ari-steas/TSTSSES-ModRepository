using CoreSystems.Api;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static VRageRender.MyBillboard;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.DebugDraw
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class DebugDrawManager : MySessionComponentBase
    {
        // i'm gonna kiss digi on the 

        public static DebugDrawManager Instance;
        protected static readonly MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");
        protected static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");

        private Dictionary<Vector3D, MyTuple<long, Color>> QueuedPoints = new Dictionary<Vector3D, MyTuple<long, Color>>();
        private Dictionary<Vector3I, MyTuple<long, Color, IMyCubeGrid>> QueuedGridPoints = new Dictionary<Vector3I, MyTuple<long, Color, IMyCubeGrid>>();
        private List<Vector3D> ToRemove = new List<Vector3D>();
        private List<Vector3I> ToGridRemove = new List<Vector3I>();

        private Dictionary<MyTuple<Vector3D, Vector3D>, MyTuple<long, Color>> QueuedLinePoints = new Dictionary<MyTuple<Vector3D, Vector3D>, MyTuple<long, Color>>();
        private List<MyTuple<Vector3D, Vector3D>> ToLineRemove = new List<MyTuple<Vector3D, Vector3D>>();

        public override void LoadData()
        {
            Instance = this;
        }

        protected override void UnloadData()
        {
            Instance = null;
        }

        public void AddPoint(Vector3D globalPos, float duration, Color color)
        {
            if (QueuedPoints.ContainsKey(globalPos))
                QueuedPoints[globalPos] = new MyTuple<long, Color>((long)(DateTime.Now.Ticks + duration * TimeSpan.TicksPerSecond), color);
            else
                QueuedPoints.Add(globalPos, new MyTuple<long, Color>((long)(DateTime.Now.Ticks + duration * TimeSpan.TicksPerSecond), color));
        }

        public void AddGPS(string name, Vector3D position, float duration)
        {
            IMyGps gps = MyAPIGateway.Session.GPS.Create(name, string.Empty, position, showOnHud: true, temporary: true);
            gps.DiscardAt = MyAPIGateway.Session.ElapsedPlayTime.Add(new TimeSpan((long)(duration * TimeSpan.TicksPerSecond)));
            MyAPIGateway.Session.GPS.AddLocalGps(gps);
        }

        public void AddGridGPS(string name, Vector3I gridPosition, IMyCubeGrid grid, float duration)
        {
            AddGPS(name, GridToGlobal(gridPosition, grid), duration);
        }

        public void AddGridPoint(Vector3I blockPos, IMyCubeGrid grid, float duration, Color color)
        {
            if (QueuedGridPoints.ContainsKey(blockPos))
                QueuedGridPoints[blockPos] = new MyTuple<long, Color, IMyCubeGrid>((long)(DateTime.Now.Ticks + duration * TimeSpan.TicksPerSecond), color, grid);
            else
                QueuedGridPoints.Add(blockPos, new MyTuple<long, Color, IMyCubeGrid>((long)(DateTime.Now.Ticks + duration * TimeSpan.TicksPerSecond), color, grid));
        }

        public void AddLine(Vector3D origin, Vector3D destination, Color color, float duration)
        {
            QueuedLinePoints.Add(new MyTuple<Vector3D, Vector3D>(origin, destination), new MyTuple<long, Color>((long)(DateTime.Now.Ticks + duration * TimeSpan.TicksPerSecond), color));
        }

        public override void Draw()
        {
            base.Draw();


            foreach (var kvp in QueuedPoints)
            {
                DrawPoint0(kvp.Key, kvp.Value.Item2);

                if (DateTime.Now.Ticks > kvp.Value.Item1)
                    ToRemove.Add(kvp.Key);
            }

            if (ToRemove.Count > 0) {
                foreach (var key in ToRemove)
                    QueuedPoints.Remove(key);

                ToRemove.Clear();
            }

            foreach (var kvp in QueuedGridPoints)
            {
                DrawGridPoint0(kvp.Key, kvp.Value.Item3, kvp.Value.Item2);

                if (DateTime.Now.Ticks > kvp.Value.Item1)
                    ToGridRemove.Add(kvp.Key);
            }

            if (ToGridRemove.Count > 0)
            {
                foreach (var key in ToGridRemove)
                    QueuedGridPoints.Remove(key);

                ToGridRemove.Clear();
            }

            foreach (var kvp in QueuedLinePoints)
            {
                DrawLine0(kvp.Key.Item1, kvp.Key.Item2, kvp.Value.Item2);

                if (DateTime.Now.Ticks > kvp.Value.Item1)
                    ToLineRemove.Add(kvp.Key);
            }

            if (ToLineRemove.Count > 0)
            {
                foreach (var key in ToLineRemove)
                    QueuedLinePoints.Remove(key);

                ToLineRemove.Clear();
            }
        }

        private void DrawPoint0(Vector3D globalPos, Color color)
        {
            float depthScale = ToAlwaysOnTop(ref globalPos);
            MyTransparentGeometry.AddPointBillboard(MaterialDot, color * OnTopColorMul, globalPos, 1.25f*depthScale, 0, blendType: BlendTypeEnum.PostPP);
        }

        private void DrawGridPoint0(Vector3I blockPos, IMyCubeGrid grid, Color color)
        {
            DrawPoint0(GridToGlobal(blockPos, grid), color);
        }

        private void DrawLine0(Vector3D origin, Vector3D destination, Color color)
        {
            float length = (float)(destination - origin).Length();
            Vector3D direction = (destination - origin) / length;

            MyTransparentGeometry.AddLineBillboard(MaterialSquare, color, origin, direction, length, 0.5f, blendType: BlendTypeEnum.PostPP);

            float depthScale = ToAlwaysOnTop(ref origin);
            direction *= depthScale;

            MyTransparentGeometry.AddLineBillboard(MaterialSquare, color * OnTopColorMul, origin, direction, length, 0.5f * depthScale, blendType: BlendTypeEnum.PostPP);
        }

        public static Vector3D GridToGlobal(Vector3I position, IMyCubeGrid grid)
        {
            return Vector3D.Rotate(((Vector3D)position) * 2.5f, grid.WorldMatrix) + grid.GetPosition();
        }

        protected const float OnTopColorMul = 0.5f;
        const float DepthRatioF = 0.01f;
        protected static float ToAlwaysOnTop(ref Vector3D position)
        {
            MatrixD camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            position = camMatrix.Translation + ((position - camMatrix.Translation) * DepthRatioF);

            return DepthRatioF;
        }
    }
}
