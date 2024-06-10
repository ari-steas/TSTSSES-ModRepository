using DynamicAsteroids.AsteroidEntities;
using VRageMath;
using ProtoBuf;

namespace DynamicAsteroids
{
    [ProtoContract]
    public struct AsteroidNetworkMessage
    {
        [ProtoMember(1)]
        public Vector3D Position;

        [ProtoMember(2)]
        public float Size;

        [ProtoMember(3)]
        public Vector3D InitialVelocity;

        [ProtoMember(4)]
        public Vector3D AngularVelocity;

        [ProtoMember(5)]
        public AsteroidType Type;

        [ProtoMember(6)]
        public bool IsSubChunk;

        [ProtoMember(7)]
        public long EntityId;

        [ProtoMember(8)]
        public bool IsRemoval;

        [ProtoMember(9)]
        public bool IsInitialCreation;

        public AsteroidNetworkMessage(Vector3D position, float size, Vector3D initialVelocity, Vector3D angularVelocity, AsteroidType type, bool isSubChunk, long entityId, bool isRemoval, bool isInitialCreation)
        {
            Position = position;
            Size = size;
            InitialVelocity = initialVelocity;
            AngularVelocity = angularVelocity;
            Type = type;
            IsSubChunk = isSubChunk;
            EntityId = entityId;
            IsRemoval = isRemoval;
            IsInitialCreation = isInitialCreation;
        }
    }
}