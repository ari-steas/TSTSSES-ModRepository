using ProtoBuf;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Scripts.IHATEKEEN.ModularWeapons.Communication
{
    public class DefinitionDefs
    {
        [ProtoContract]
        public class DefinitionContainer
        {
            [ProtoMember(1)] internal PhysicalDefinition[] PhysicalDefs;
        }

        [ProtoContract]
        public class PhysicalDefinition
        {
            [ProtoMember(1)] public string Name { get; set; }

            /// <summary>
            /// Called when a weapon is fired. Returns a MyTuple with contents: bool CloseImmediately, Vector3D ProjectilePosition, Vector3D AdditiveVelocity, float BaseDamagePool
            /// <para>
            /// Arg1 is PhysicalWeaponId, Arg2 is firerPartId, Arg3 is targetEntityId, Arg4 is projectilePosition
            /// </para>
            /// </summary>
            public Func<int, int, ulong, long, Vector3D, VRage.MyTuple<bool, Vector3D, Vector3D, float>> OnShoot { get; set; }

            /// <summary>
            /// Called when a valid part is placed.
            /// <para>
            /// Arg1 is PhysicalWeaponId, Arg2 is EntityId.
            /// </para>
            /// </summary>
            public Action<int, long> OnPartPlace { get; set; }

            /// <summary>
            /// Called when a valid part is removed. Arg1 is PhysicalWeaponId, Arg2 is EntityId
            /// </summary>
            public Action<int, long> OnPartRemove { get; set; }

            [ProtoMember(2)] public string[] AllowedBlocks { get; set; }
            [ProtoMember(3)] public Dictionary<string, Vector3I[]> AllowedConnections { get; set; }
            [ProtoMember(4)] public string BaseBlock { get; set; }
        }

        [ProtoContract]
        public class FunctionCall
        {
            public string DefinitionName { get; set; }
            public int PhysicalWeaponId { get; set; }
            public ActionType ActionId { get; set; }
            public object[] values { get; set; }

            public enum ActionType
            {
                OnShoot,
                OnPartPlace,
                OnPartRemove,
            }
        }
    }
}
