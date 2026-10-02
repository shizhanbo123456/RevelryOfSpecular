namespace Ros.Transport
{
    public class CSRoomUpdate
    {
        public int camp = -1;
        public int attackAICount;
        public int defenseAICount;
    }

    public struct CSRoomUpdateSerializer
    {
        public static bool Serialize(CSRoomUpdate value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.camp, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.attackAICount, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.defenseAICount, result, ref indexStart);
        }

        public static CSRoomUpdate Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSRoomUpdate()
            {
                camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
