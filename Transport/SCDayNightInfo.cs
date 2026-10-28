using System;

namespace Ros.Transport
{
    public class SCDayNightInfo
    {
        public float cycleTime = 1f;
        public float dayDuration = 90f;
        public float nightDuration = 90f;
    }

    public struct SCDayNightInfoSerializer
    {
        public static bool Serialize(SCDayNightInfo value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 8)) return false;

            // 位级：1+3*20=61 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteFloat(value.cycleTime, 0f, 86400f, 0.1f);
                BitField.WriteFloat(value.dayDuration, 0f, 86400f, 0.1f);
                BitField.WriteFloat(value.nightDuration, 0f, 86400f, 0.1f);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCDayNightInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCDayNightInfo 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 8) throw new Exception("反序列化 SCDayNightInfo 失败：剩余数据不足");

            var r = new SCDayNightInfo();
            r.cycleTime = BitField.ReadFloat(0f, 86400f, 0.1f);
            r.dayDuration = BitField.ReadFloat(0f, 86400f, 0.1f);
            r.nightDuration = BitField.ReadFloat(0f, 86400f, 0.1f);
            indexStart += 8;
            return r;
        }

        private static bool SerializeBytes(SCDayNightInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!FloatSerializer.Serialize(value.cycleTime, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.dayDuration, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.nightDuration, result, ref indexStart);
        }

        private static SCDayNightInfo DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCDayNightInfo()
            {
                cycleTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                dayDuration = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                nightDuration = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
