using System;

using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    public class SkillContext
    {
        public List<int> ints = new();
        public List<Vector3> vectors = new();
        public List<float> floats = new();

        public void AddInts(params int[] values)
        {
            foreach (var v in values) ints.Add(v);
        }

        public void AddVectors(params Vector3[] values)
        {
            foreach (var v in values) vectors.Add(v);
        }

        public void AddFloats(params float[] values)
        {
            foreach (var v in values) floats.Add(v);
        }
    }

    public struct SkillContextSerializer
    {
        public static bool Serialize(SkillContext value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);

            // 位级：数量各 4 位；int/float 原样 32 位，Vector3 按 -500~500 / 0.05 量化 45 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                int intCount = value.ints != null ? value.ints.Count : 0;
                BitField.WriteInt(intCount, 0, 15);
                if (value.ints != null)
                {
                    foreach (var v in value.ints) BitField.WriteInt(v, int.MinValue, int.MaxValue);
                }

                int vectorCount = value.vectors != null ? value.vectors.Count : 0;
                BitField.WriteInt(vectorCount, 0, 15);
                if (value.vectors != null)
                {
                    foreach (var v in value.vectors)
                    {
                        BitField.WriteFloat(v.x, -500f, 500f, 0.05f);
                        BitField.WriteFloat(v.y, -500f, 500f, 0.05f);
                        BitField.WriteFloat(v.z, -500f, 500f, 0.05f);
                    }
                }

                int floatCount = value.floats != null ? value.floats.Count : 0;
                BitField.WriteInt(floatCount, 0, 15);
                if (value.floats != null)
                {
                    foreach (var v in value.floats)
                    {
                        BitField.WriteInt(BitConverter.SingleToInt32Bits(v), int.MinValue, int.MaxValue);
                    }
                }
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SkillContext Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SkillContext 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }

            var context = new SkillContext();
            int intCount = BitField.ReadInt(0, 15);
            for (int i = 0; i < intCount; i++)
            {
                context.ints.Add(BitField.ReadInt(int.MinValue, int.MaxValue));
            }

            int vectorCount = BitField.ReadInt(0, 15);
            for (int i = 0; i < vectorCount; i++)
            {
                context.vectors.Add(new Vector3(
                    BitField.ReadFloat(-500f, 500f, 0.05f),
                    BitField.ReadFloat(-500f, 500f, 0.05f),
                    BitField.ReadFloat(-500f, 500f, 0.05f)));
            }

            int floatCount = BitField.ReadInt(0, 15);
            for (int i = 0; i < floatCount; i++)
            {
                context.floats.Add(BitConverter.Int32BitsToSingle(BitField.ReadInt(int.MinValue, int.MaxValue)));
            }

            indexStart += BitField.ReadByteLength;
            return context;
        }

        private static bool SerializeBytes(SkillContext value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;

            int intCount = value.ints != null ? value.ints.Count : 0;
            if (!IntSerializer.Serialize(intCount, result, ref indexStart)) return false;
            if (value.ints != null)
            {
                foreach (var v in value.ints)
                {
                    if (!IntSerializer.Serialize(v, result, ref indexStart)) return false;
                }
            }

            int vectorCount = value.vectors != null ? value.vectors.Count : 0;
            if (!IntSerializer.Serialize(vectorCount, result, ref indexStart)) return false;
            if (value.vectors != null)
            {
                foreach (var v in value.vectors)
                {
                    if (!Vector3Serializer.Serialize(v, result, ref indexStart)) return false;
                }
            }

            int floatCount = value.floats != null ? value.floats.Count : 0;
            if (!IntSerializer.Serialize(floatCount, result, ref indexStart)) return false;
            if (value.floats != null)
            {
                foreach (var v in value.floats)
                {
                    if (!FloatSerializer.Serialize(v, result, ref indexStart)) return false;
                }
            }
            return true;
        }

        private static SkillContext DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var context = new SkillContext();
            int intCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < intCount; i++)
            {
                context.ints.Add(IntSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            int vectorCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < vectorCount; i++)
            {
                context.vectors.Add(Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            int floatCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < floatCount; i++)
            {
                context.floats.Add(FloatSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            return context;
        }
    }
}
