using System;
using Sandbox.ModAPI;
using VRage.Utils;

namespace DeepSpaceScanner
{
    public static class Log
    {
        static bool _debug;

        public static void Info(string str)
        {
            if (_debug) MyLog.Default.WriteLine(str);
            MyAPIGateway.Utilities.InvokeOnGameThread(() => MyAPIGateway.Utilities.ShowMessage("INFO", str));
        }

        public static void Error(Exception e, string str = null)
        {
            if (str != null) MyLog.Default.WriteLine(str);
            MyLog.Default.WriteLine(e);
            
            if (_debug)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    MyAPIGateway.Utilities.ShowMessage("ERROR", $"{e.Message} {str}");
                    MyAPIGateway.Utilities.ShowMessage("ERROR", e.StackTrace);
                });
        }
    }
}