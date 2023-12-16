using System;
using System.IO;
using Microsoft.Win32;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;

namespace Scanner.Data.Scripts.DeepSpaceScanner
{
    public class ModConfig
    {
        public static readonly MyDefinitionId E = MyResourceDistributorComponent.ElectricityId;
        
        // for large grid (x0.2 for small)
        public static int[] RangeLimits;
        
        // for large grid (x0.2 for small)
        public static float[] PowerLimits;
        
        public static float ScanAngle;
        
        // 1m per 1MWt
        public static float SignaturePowerMultiplier; 
        
        // 1MN = 13.89kWt as per small ion thruster
        public static float SignatureThermalMultiplier;
        
        // 1m per 100 PCU for large grid (x0.2 for small)
        public static float SignaturePcuMultiplier; 
        
        // 1m per 100 blocks for large grid (x0.2 for small)
        public static float SignatureBlocksMultiplier; 

        public static float MaxDistanceDeviation;
        
        // in case scan distance exceeds target distance
        public static uint MaxSignatureMultiplier;
        
        public static double ScanDuration;

        public static bool NotifyWhenScanned;

        public static int SignatureUpdateFrequency;
        
        public static uint AsteroidScanMaxDistance;

        public static bool DrawHud;

        const string NAME = "DeepSpaceScanner.cfg";

        static ModConfig()
        {
            TextReader reader = null;
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInGlobalStorage(NAME)) throw new Exception("Config doesn't exist");
                reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(NAME);
                var values = MyAPIGateway.Utilities.SerializeFromXML<Values>(reader.ReadToEnd());
                reader.Close();
                Init(values);
            }
            catch (Exception e)
            {
                reader?.Close();
                var values = new Values();
                SaveCfg(values);
                Init(values);
            }
        }

        static void Init(Values values)
        {
            RangeLimits = values.RangeLimits;
            PowerLimits = values.PowerLimits;
            ScanAngle = values.ScanAngle;
            SignaturePowerMultiplier = values.SignaturePowerMultiplier;
            SignatureThermalMultiplier = values.SignatureThermalMultiplier;
            SignaturePcuMultiplier = values.SignaturePcuMultiplier;
            SignatureBlocksMultiplier = values.SignatureBlocksMultiplier;
            MaxDistanceDeviation = values.MaxDistanceDeviation;
            MaxSignatureMultiplier = values.MaxSignatureMultiplier;
            ScanDuration = values.ScanDuration;
            NotifyWhenScanned = values.NotifyWhenScanned;
            SignatureUpdateFrequency = values.SignatureUpdateFrequency;
            AsteroidScanMaxDistance = values.AsteroidScanMaxDistance;
            DrawHud = values.DrawHud;
        }

        static void SaveCfg(Values values)
        {
            var writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(NAME);
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(values));
            writer.Close();
        }

        public class Values
        {
            public int[] RangeLimits = {20, 10000};
            public float[] PowerLimits = {0.05f, 100};
            public float ScanAngle = 10;
            public float SignaturePowerMultiplier = 1f; 
            public float SignatureThermalMultiplier = 0.01389f;
            public float SignaturePcuMultiplier = 0.01f; 
            public float SignatureBlocksMultiplier = 0.01f; 
            public float MaxDistanceDeviation = 0.3f;
            public uint MaxSignatureMultiplier = 2;
            public double ScanDuration = 5000;
            public bool NotifyWhenScanned = true;
            public int SignatureUpdateFrequency = 10;
            public uint AsteroidScanMaxDistance = 30;
            public bool DrawHud = true;
        }
    }
}