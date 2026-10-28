using System;

using Ros.Gm;

namespace Ros.Transport
{
    //gm指令允许冗余字段
    public class CSGm
    {
        public GmEnum type;
        public int int1;
        public int int2;
        public float float1;
        public float float2;
        public float float3;
    }

    public struct CSGmSerializer
    {
        public static bool Serialize(CSGm value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 21)) return false;

            // 位级：1+6+4*32=135 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt((int)value.type, 0, 63);
                BitField.WriteInt(value.int1, int.MinValue, int.MaxValue);
                BitField.WriteInt(value.int2, int.MinValue, int.MaxValue);
                BitField.WriteInt(BitConverter.SingleToInt32Bits(value.float1), int.MinValue, int.MaxValue);
                BitField.WriteInt(BitConverter.SingleToInt32Bits(value.float2), int.MinValue, int.MaxValue);
                BitField.WriteInt(BitConverter.SingleToInt32Bits(value.float3), int.MinValue, int.MaxValue);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static CSGm Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 CSGm 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 21) throw new Exception("反序列化 CSGm 失败：剩余数据不足");

            var r = new CSGm();
            r.type = (GmEnum)BitField.ReadInt(0, 63);
            r.int1 = BitField.ReadInt(int.MinValue, int.MaxValue);
            r.int2 = BitField.ReadInt(int.MinValue, int.MaxValue);
            r.float1 = BitConverter.Int32BitsToSingle(BitField.ReadInt(int.MinValue, int.MaxValue));
            r.float2 = BitConverter.Int32BitsToSingle(BitField.ReadInt(int.MinValue, int.MaxValue));
            r.float3 = BitConverter.Int32BitsToSingle(BitField.ReadInt(int.MinValue, int.MaxValue));
            indexStart += 21;
            return r;
        }

        private static bool SerializeBytes(CSGm value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize((int)value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.int1, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.int2, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.float1, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.float2, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.float3, result, ref indexStart);
        }

        private static CSGm DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSGm()
            {
                type = (GmEnum)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                int1 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                int2 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                float1 = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                float2 = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                float3 = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
