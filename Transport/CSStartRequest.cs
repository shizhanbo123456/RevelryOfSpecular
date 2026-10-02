namespace Ros.Transport
{
    public class CSStartRequest
    {
    }

    public struct CSStartRequestSerializer
    {
        public static bool Serialize(CSStartRequest value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static CSStartRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSStartRequest();
        }
    }
}
