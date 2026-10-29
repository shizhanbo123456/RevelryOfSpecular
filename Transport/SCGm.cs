using System;

using Ros.Gm;

namespace Ros.Transport
{ 
    //gm指令允许冗余字段
    public class SCGm
    {
        public GmEnum type;
        public int int1;
        public float float1;
    }

    public struct SCGmSerializer
    {
        public static bool Serialize(SCGm value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!IntSerializer.Serialize((int)value.type, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.int1, result, ref indexStart)) return false;
                return FloatSerializer.Serialize(value.float1, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 9)) return false;

                // 位级：1+6+2*32=71 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt((int)value.type, 0, 63);
                    BitField.WriteInt(value.int1, int.MinValue, int.MaxValue);
                    BitField.WriteInt(BitConverter.SingleToInt32Bits(value.float1), int.MinValue, int.MaxValue);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCGm Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                return new SCGm()
                {
                    type = (GmEnum)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    int1 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    float1 = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCGm 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 9) throw new Exception("反序列化 SCGm 失败：剩余数据不足");

                var r = new SCGm();
                r.type = (GmEnum)BitField.ReadInt(0, 63);
                r.int1 = BitField.ReadInt(int.MinValue, int.MaxValue);
                r.float1 = BitConverter.Int32BitsToSingle(BitField.ReadInt(int.MinValue, int.MaxValue));
                indexStart += 9;
                return r;
            }
        }
    }
}
