namespace Ros.Transport
{
    public class SCBattleEvent
    {
        public static class Type
        {
            public const byte Kill = 0;
            public const byte BeaconDestroyed = 1;
            public const byte PlagueTreeCaptured = 6;
        }

        public byte type;
        public int value;    // Kill = 击杀者客户端 id（-1 无归属）；BeaconDestroyed = 守护点标识（-1 中心，0~2 外围）
        public int targetId; // Kill = 受害实体 id
    }

    public struct SCBattleEventSerializer
    {
        public static bool Serialize(SCBattleEvent value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!ByteSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.value, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.targetId, result, ref indexStart)) return false;
            return true;
        }

        public static SCBattleEvent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCBattleEvent()
            {
                type = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                value = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                targetId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
