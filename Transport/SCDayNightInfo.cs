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
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!FloatSerializer.Serialize(value.cycleTime, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.dayDuration, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.nightDuration, result, ref indexStart);
        }

        public static SCDayNightInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
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
