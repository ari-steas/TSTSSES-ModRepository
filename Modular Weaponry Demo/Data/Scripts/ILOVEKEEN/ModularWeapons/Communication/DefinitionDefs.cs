using ProtoBuf;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Scripts.ILOVEKEEN.ModularWeaponry.Communication
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
            /// <summary>
            /// The name of this definition. Must be unique!
            /// </summary>
            [ProtoMember(1)] public string Name { get; set; }

            /// <summary>
            /// Called when a weapon is fired. Returns a MyTuple with contents: bool CloseImmediately, Vector3D ProjectilePosition, Vector3D AdditiveVelocity, float BaseDamagePool
            /// <para>
            /// Arg1 is PhysicalWeaponId, Arg2 is BlockEntityId, Arg3 is firerPartId, Arg4 is targetEntityId, Arg5 is projectilePosition
            /// </para>
            /// </summary>
            public Func<int, long, int, ulong, long, Vector3D, MyTuple<bool, Vector3D, Vector3D, float>> OnShoot { get; set; }

            /// <summary>
            /// Called when a valid part is placed.
            /// <para>
            /// Arg1 is PhysicalWeaponId, Arg2 is EntityId, Arg3 is IsBaseBlock
            /// </para>
            /// </summary>
            public Action<int, long, bool> OnPartAdd { get; set; }

            /// <summary>
            /// Called when a valid part is removed.
            /// <para>
            /// Arg1 is PhysicalWeaponId, Arg2 is EntityId, Arg3 is IsBaseBlock
            /// </para>
            /// </summary>
            public Action<int, long, bool> OnPartRemove { get; set; }

            /// <summary>
            /// Called when a component part is destroyed.
            /// <para>
            /// Arg1 is PhysicalWeaponId, Arg2 is EntityId, Arg3 is IsBaseBlock
            /// </para>
            /// </summary>
            public Action<int, long, bool> OnPartDestroy { get; set; }

            /// <summary>
            /// All allowed SubtypeIds. The mod will likely misbehave if two mods allow the same blocks, so please be cautious.
            /// </summary>
            [ProtoMember(2)] public string[] AllowedBlocks { get; set; }

            /// <summary>
            /// Allowed connection directions. Measured in blocks.
            /// </summary>
            [ProtoMember(3)] public Dictionary<string, Vector3I[]> AllowedConnections { get; set; }

            /// <summary>
            /// The primary block of a PhysicalWeapon. Make sure this is a Weaponcore block.
            /// </summary>
            [ProtoMember(4)] public string BaseBlock { get; set; }
        }

        [ProtoContract]
        public class FunctionCall
        {
            [ProtoMember(1)] public string DefinitionName { get; set; }
            [ProtoMember(2)] public int PhysicalWeaponId { get; set; }
            [ProtoMember(3)] public ActionType ActionId { get; set; }
            [ProtoMember(4)] public SerializedObjectArray Values { get; set; }

            public enum ActionType
            {
                OnShoot,
                OnPartAdd,
                OnPartRemove,
                OnPartDestroy,
                GetAllParts,
                GetAllWeapons,
                GetMemberParts,
                GetConnectedBlocks,
            }
        }

        [ProtoContract]
        public class SerializedObjectArray
        {
            public SerializedObjectArray() { }

            public SerializedObjectArray(params object[] array)
            {
                List<int> intValuesL = new List<int>();
                List<string> stringValuesL = new List<string>();
                List<long> longValuesL = new List<long>();
                List<ulong> ulongValuesL = new List<ulong>();
                List<Vector3D> vectorValuesL = new List<Vector3D>();
                List<float> floatValuesL = new List<float>();
                List<bool> boolValuesL = new List<bool>();
                List<double> doubleValuesL = new List<double>();
                List<MyTuple<bool, Vector3D, Vector3D, float>> projectileValuesL = new List<MyTuple<bool, Vector3D, Vector3D, float>>();

                foreach (var value in array)
                {
                    Type type = value.GetType();
                    if (type == typeof(int))
                        intValuesL.Add((int)value);
                    else if (type == typeof(string))
                        stringValuesL.Add((string)value);
                    else if (type == typeof(long))
                        longValuesL.Add((long)value);
                    else if (type == typeof(ulong))
                        ulongValuesL.Add((ulong)value);
                    else if (type == typeof(Vector3D))
                        vectorValuesL.Add((Vector3D)value);
                    else if (type == typeof(float))
                        floatValuesL.Add((float)value);
                    else if (type == typeof(bool))
                        boolValuesL.Add((bool)value);
                    else if (type == typeof(double))
                        doubleValuesL.Add((double)value);
                    else if (type == typeof(MyTuple<bool, Vector3D, Vector3D, float>))
                        projectileValuesL.Add((MyTuple<bool, Vector3D, Vector3D, float>)value);
                }

                intValues = intValuesL.ToArray();
                stringValues = stringValuesL.ToArray();
                longValues = longValuesL.ToArray();
                ulongValues = ulongValuesL.ToArray();
                vectorValues = vectorValuesL.ToArray();
                floatValues = floatValuesL.ToArray();
                boolValues = boolValuesL.ToArray();
                doubleValues = doubleValuesL.ToArray();
                projectileValues = projectileValuesL.ToArray();

                //MyLog.Default.WriteLine($"ModularDefinitions.DefinitionDefs: {array.Length} values packaged.");
            }

            [ProtoMember(1)] internal int[] intValues = new int[0];
            [ProtoMember(2)] internal string[] stringValues = new string[0];
            [ProtoMember(3)] internal long[] longValues = new long[0];
            [ProtoMember(4)] internal ulong[] ulongValues = new ulong[0];
            [ProtoMember(5)] internal Vector3D[] vectorValues = new Vector3D[0];
            [ProtoMember(6)] internal float[] floatValues = new float[0];
            [ProtoMember(7)] internal bool[] boolValues = new bool[0];
            [ProtoMember(8)] internal double[] doubleValues = new double[0];
            [ProtoMember(9)] internal MyTuple<bool, Vector3D, Vector3D, float>[] projectileValues = new MyTuple<bool, Vector3D, Vector3D, float>[0];

            public object[] Values()
            {
                List<object> values = new List<object>();

                foreach (var value in intValues)
                    values.Add(value);
                foreach (var value in stringValues)
                    values.Add(value);
                foreach (var value in longValues)
                    values.Add(value);
                foreach (var value in ulongValues)
                    values.Add(value);
                foreach (var value in vectorValues)
                    values.Add(value);
                foreach (var value in floatValues)
                    values.Add(value);
                foreach (var value in boolValues)
                    values.Add(value);
                foreach (var value in doubleValues)
                    values.Add(value);
                foreach (var value in projectileValues)
                    values.Add(value);

                //MyLog.Default.WriteLine($"ModularDefinitions.DefinitionDefs: {values.Count} values recieved.");
                return values.ToArray();
            }
        }
    }
}
