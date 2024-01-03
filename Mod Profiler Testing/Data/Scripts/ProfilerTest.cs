using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.Components;
using VRage.Scripting.CompilerMethods;

namespace Mod_Profiler_Testing.Data.Scripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ProfilerTest : MySessionComponentBase
    {
        const int maxRuns = 1000000;
        int runCt = -2;
        List<long> runTimes = new List<long>();

        public override void BeforeStart()
        {
            //MySimpleProfiler.ENABLE_SIMPLE_PROFILER = false;
            MyAPIGateway.Utilities.ShowMessage("ModProfilerTest", "Run !mpStart");
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Utilities.MessageEnteredSender += ChatHandler;
        }

        private void ChatHandler(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("!"))
                return;

            string[] split = messageText.Split(' ');
            switch (split[0].ToLower())
            {
                case "!mpstart":
                    runCt = 60;
                    sendToOthers = false;
                    MyAPIGateway.Utilities.SendMessage("Profiling for 1s");
                    MyAPIGateway.Utilities.ShowMessage("", "Profiling for 1s");
                    break;
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (runCt >= 0)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int i = 0; i < maxRuns; i++)
                {
                    TestMethod(i);
                }
                stopwatch.Stop();
                runCt--;
                runTimes.Add(stopwatch.ElapsedMilliseconds);
                //MyAPIGateway.Utilities.ShowMessage("" + runCt, $"{stopwatch.ElapsedTicks}t");
            }
            else if (runCt == -1)
            {
                long sum = 0;
                foreach (long i in runTimes)
                    sum += i;
                double avg = Math.Round(sum / (double)runTimes.Count, 3);
                MyAPIGateway.Utilities.SendMessage($"Profiled time: {avg}ms");
                MyAPIGateway.Utilities.ShowMessage("Profiled Time", $"{avg}ms");
                runTimes.Clear();
                runCt = -2;
            }
        }

        public void TestMethod(int i)
        {
            long j = i * i;
            int l = (int) Math.Sqrt(j);
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Utilities.MessageEnteredSender -= ChatHandler;
        }
    }
}
