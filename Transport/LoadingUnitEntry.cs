namespace Ros.Transport
{
    // 加载期下发的单位条目：真人玩家与 AI 同结构（AI 用虚拟负数 clientId）
    public class LoadingUnitEntry
    {
        public short clientId;
        public ushort entityId;
        public string name = "";
        public EntityCamp camp;
    }

    public struct LoadingUnitEntrySerializer
    {
        public static bool Serialize(LoadingUnitEntry value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.clientId, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!StringSerializer.Serialize(value.name, result, ref indexStart)) return false;
            return IntSerializer.Serialize((int)value.camp, result, ref indexStart);
        }

        public static LoadingUnitEntry Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new LoadingUnitEntry()
            {
                clientId = (short)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                name = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
