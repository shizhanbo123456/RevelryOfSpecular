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
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize((int)value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.int1, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.int2, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.float1, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.float2, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.float3, result, ref indexStart);
        }

        public static CSGm Deserialize(byte[] data, ref int indexStart, int invalidIndex)
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
