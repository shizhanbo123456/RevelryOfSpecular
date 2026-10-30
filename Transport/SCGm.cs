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
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize((int)value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.int1, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.float1, result, ref indexStart);
        }

        public static SCGm Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCGm()
            {
                type = (GmEnum)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                int1 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                float1 = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
