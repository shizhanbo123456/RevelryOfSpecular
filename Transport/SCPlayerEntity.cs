namespace Ros.Transport
{
    // 玩家 → 实体 id（每玩家一条独立包），-1 = 无实体（死亡 / 观战 / 退场）；开局全量，此后仅变更时下发
    public class SCPlayerEntity
    {
        public short clientId;
        public int entityId;
    }

    public struct SCPlayerEntitySerializer
    {
        public static bool Serialize(SCPlayerEntity value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.clientId, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.entityId, result, ref indexStart);
        }

        public static SCPlayerEntity Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCPlayerEntity()
            {
                clientId = (short)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                entityId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
