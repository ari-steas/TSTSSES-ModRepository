using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using Scanner.Data.Scripts.DeepSpaceScanner;
using VRageMath;

namespace DeepSpaceScanner
{
    [ProtoInclude(1, typeof(Register))]
    [ProtoInclude(2, typeof(ScanRequest))]
    [ProtoInclude(3, typeof(ScanResponse))]
    [ProtoInclude(4, typeof(ScanResponse))]
    [ProtoContract]
    public abstract class MessageBase
    {
        [ProtoMember(101)] public ulong SenderId;
        [ProtoMember(102)] public Guid Guid;

        public MessageBase()
        {
            SenderId = MyAPIGateway.Multiplayer.MyId;
        }
    }

    [ProtoContract]
    public class Register : MessageBase
    {
        public Register()
        {
            Guid = ModComponent.Guid;
        }
    }
    
    [ProtoContract]
    public class Scanned : MessageBase
    {
    }

    [ProtoContract]
    public class ScanRequest : MessageBase
    {
        [ProtoMember(1)] public long EntityId;
        [ProtoMember(2)] public float Strength;
        [ProtoMember(3)] public float Pitch;
        [ProtoMember(4)] public float Yaw;
        [ProtoMember(5)] public bool ScanAsteroids;

        public ScanRequest()
        {
        }

        public ScanRequest(long entityId, float strength, float pitch, float yaw, bool scanAsteroids = false)
        {
            EntityId = entityId;
            Guid = ModComponent.Guid;
            Strength = strength;
            Pitch = pitch;
            Yaw = yaw;
            ScanAsteroids = scanAsteroids;
        }
    }

    [ProtoContract]
    public class ScanResponse : MessageBase
    {
        [ProtoMember(1)] public long? EntityId;
        [ProtoMember(2)] public List<ScanResult> Results;
        [ProtoMember(3)] public string Error;

        public ScanResponse()
        {
        }

        public ScanResponse(long entityId, List<ScanResult> results)
        {
            Results = results;
            EntityId = entityId;
        }
    }

    [ProtoContract]
    public class ScanResult
    {
        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public string Size;
        [ProtoMember(3)] public string Signature;
        [ProtoMember(4)] public string Distance;
        [ProtoMember(5)] public string Signal;
        [ProtoMember(6)] public Vector3D Location;
        public int NumDistance = int.MaxValue;

        public ScanResult()
        {
        }

        public override string ToString()
        {
            return $"{Name ?? "Unknown"} ({Size ?? "Unknown"})\nSignal: {Signal}\nDistance: {Distance ?? "Unknown"}\nSignature: {Signature ?? "Unknown"}";
        }
    }
}