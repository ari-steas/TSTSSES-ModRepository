using ProtoBuf;
using VRage;
using VRageMath;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts.Client
{
    internal class ClientSyncDefinitions
    {
        [ProtoContract]
        public class ProjectileContainer
        {
            public ProjectileContainer() { }

            public ProjectileContainer(ulong projectileId, MyTuple<bool, Vector3D, Vector3D, float> projectileData, long time)
            {
                this.ProjectileId = projectileId;
                this.ProjectileData = projectileData;
                Time = time;
            }

            [ProtoMember(1)] public ulong ProjectileId { get; set; }
            [ProtoMember(2)] public MyTuple<bool, Vector3D, Vector3D, float> ProjectileData { get; set; }
            [ProtoMember(3)] public long Time { get; set; }
        }
    }
}
