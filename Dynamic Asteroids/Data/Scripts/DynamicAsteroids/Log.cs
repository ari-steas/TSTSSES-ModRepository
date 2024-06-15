using System;
using System.IO;
using DynamicAsteroids;
using Sandbox.ModAPI;

namespace Invalid.DynamicRoids
{
    internal class Log
    {
        private static Log I;
        private readonly TextWriter _writer;

        private Log()
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage("DynamicAsteroids.log");
            _writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage("DynamicAsteroids.log");
            _writer.WriteLine($"      Dynamic Asteroids - {(MyAPIGateway.Session.IsServer ? "Server" : "Client")} Debug Log\n===========================================\n");
            _writer.Flush();
        }

        public static void Info(string message)
        {
            if (AsteroidSettings.EnableLogging)
                I?._Log(message);
        }

        public static void Warning(string message)
        {
            if (AsteroidSettings.EnableLogging)
                I?._Log("WARNING: " + message);
        }

        public static void Exception(Exception ex, Type callingType, string prefix = "")
        {
            if (AsteroidSettings.EnableLogging)
                I?._LogException(ex, callingType, prefix);
        }

        public static void Init()
        {
            Close();
            I = new Log();
        }

        public static void Close()
        {
            if (I != null)
            {
                Info("Closing log writer.");
                I._writer.Close();
            }

            I = null;
        }

        private void _Log(string message)
        {
            _writer.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: {message}");
            _writer.Flush();
        }

        private void _LogException(Exception ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                _Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            _Log(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
            MyAPIGateway.Utilities.ShowNotification($"{ex.GetType().Name} in Dynamic Asteroids! Check logs for more info.", 10000, "Red");
        }
    }
}