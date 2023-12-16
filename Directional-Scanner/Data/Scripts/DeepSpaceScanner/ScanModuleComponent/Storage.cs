using System;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;

namespace DeepSpaceScanner
{
    public partial class ScanLogic
    {
        readonly Guid _storageGuid = new Guid("480b84bd-2d02-45ec-bca3-5858bbd67d1c");

        void LoadBlockSettings()
        {
            try
            {
                string data = null;
                _block.Storage?.TryGetValue(_storageGuid, out data);
                var settings = data?.Length > 0 ? MyAPIGateway.Utilities.SerializeFromBinary<ModuleSettings>(Convert.FromBase64String(data)) : new ModuleSettings();
                
                NextYaw = settings.Yaw;
                NextPitch = settings.Pitch;
                ShowPopup = settings.ShowPopup;
                ScannerStrength = settings.ScannerStrength;
                ScanAsteroids = settings.ScanAsteroids;
                TextSurface = settings.TextSurface;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        void SaveBlockSettings()
        {
            try
            {
                var settings = new ModuleSettings
                {
                    Pitch = NextPitch,
                    Yaw = NextYaw,
                    ShowPopup = ShowPopup,
                    ScannerStrength = ScannerStrength,
                    ScanAsteroids = ScanAsteroids,
                    TextSurface = TextSurface
                };

                if (Entity.Storage == null) Entity.Storage = new MyModStorageComponent();
                var binary = MyAPIGateway.Utilities.SerializeToBinary(settings);
                Entity.Storage[_storageGuid] = Convert.ToBase64String(binary);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        [ProtoContract]
        class ModuleSettings
        {
            [ProtoMember(1)] public float ScannerStrength = 5;
            [ProtoMember(2)] public bool ShowPopup = true;
            [ProtoMember(3)] public float Pitch;
            [ProtoMember(4)] public float Yaw;
            [ProtoMember(5)] public bool ScanAsteroids;
            [ProtoMember(6)] public MyTuple<long, int> TextSurface;
        }
        
    }
}