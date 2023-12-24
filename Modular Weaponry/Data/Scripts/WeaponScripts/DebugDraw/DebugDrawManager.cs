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
        public static DebugDrawManager Instance;
        protected static readonly MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");

        private Dictionary<Vector3D, MyTuple<long, Color>> QueuedPoints = new Dictionary<Vector3D, MyTuple<long, Color>>();
        private Dictionary<Vector3I, MyTuple<long, Color, IMyCubeGrid>> QueuedGridPoints = new Dictionary<Vector3I, MyTuple<long, Color, IMyCubeGrid>>();
        private List<Vector3D> ToRemove = new List<Vector3D>();
        private List<Vector3I> ToGridRemove = new List<Vector3I>();

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

        public void DrawPoint0(Vector3D globalPos, Color color)
        {
            MyTransparentGeometry.AddPointBillboard(MaterialDot, color, globalPos, 1.25f, 0, blendType: BlendTypeEnum.PostPP);
        }

        public void DrawGridPoint0(Vector3I blockPos, IMyCubeGrid grid, Color color)
        {
            MyTransparentGeometry.AddPointBillboard(MaterialDot, color, GridToGlobal(blockPos, grid), 1.25f, 0, blendType: BlendTypeEnum.PostPP);
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

        public void AddGridPoint(Vector3I blockPos, IMyCubeGrid grid, int duration, Color color)
        {
            if (QueuedGridPoints.ContainsKey(blockPos))
                QueuedGridPoints[blockPos] = new MyTuple<long, Color, IMyCubeGrid>(DateTime.Now.Ticks + duration * TimeSpan.TicksPerSecond, color, grid);
            else
                QueuedGridPoints.Add(blockPos, new MyTuple<long, Color, IMyCubeGrid>(DateTime.Now.Ticks + duration * TimeSpan.TicksPerSecond, color, grid));
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();


            foreach (var kvp in QueuedPoints)
            {
                MyTransparentGeometry.AddPointBillboard(MaterialDot, kvp.Value.Item2, kvp.Key, 1.25f, 0, blendType: BlendTypeEnum.PostPP);

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
                MyTransparentGeometry.AddPointBillboard(MaterialDot, kvp.Value.Item2, GridToGlobal(kvp.Key, kvp.Value.Item3), 1.25f, 0, blendType: BlendTypeEnum.PostPP);

                if (DateTime.Now.Ticks > kvp.Value.Item1)
                    ToGridRemove.Add(kvp.Key);
            }

            if (ToGridRemove.Count > 0)
            {
                foreach (var key in ToGridRemove)
                    QueuedGridPoints.Remove(key);

                ToGridRemove.Clear();
            }
        }

        public Vector3D GridToGlobal(Vector3I position, IMyCubeGrid grid)
        {
            return Vector3D.Rotate(((Vector3D)position) * 2.5f, grid.WorldMatrix) + grid.GetPosition();
        }
    }
}
