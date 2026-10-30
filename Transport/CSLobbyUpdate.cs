namespace Ros.Transport
{
    public class CSLobbyUpdate
    {
        public int camp = -1;      // 0 进攻 / 1 防守 / 2 观战（-1 = 不变更）
        public bool ready;         // 准备状态
        public int attackAICount;
        public int defenseAICount;
    }

    public struct CSLobbyUpdateSerializer
    {
        public static bool Serialize(CSLobbyUpdate value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.camp, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.ready, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.attackAICount, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.defenseAICount, result, ref indexStart);
        }

        public static CSLobbyUpdate Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSLobbyUpdate()
            {
                camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                ready = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
