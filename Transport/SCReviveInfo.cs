namespace Ros.Transport
{
    public class SCReviveInfo
    {
        public ushort entityId;
        public float progress;
        public bool ready;
        public int yzStack;
    }

    public struct SCReviveInfoSerializer
    {
        public static bool Serialize(SCReviveInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.progress, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.ready, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.yzStack, result, ref indexStart);
        }

        public static SCReviveInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCReviveInfo()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                progress = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                ready = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                yzStack = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
