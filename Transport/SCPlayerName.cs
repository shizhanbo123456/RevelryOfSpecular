namespace Ros.Transport
{
    // 服务器 → 客户端：单个玩家的名字（与房间成员列表分开下发，避免成员包被长名字撑大）
    public class SCPlayerName
    {
        public short clientId;
        public string name = "";
    }

    public struct SCPlayerNameSerializer
    {
        public static bool Serialize(SCPlayerName value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.clientId, result, ref indexStart)) return false;
            return StringSerializer.Serialize(value.name, result, ref indexStart);
        }

        public static SCPlayerName Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCPlayerName()
            {
                clientId = (short)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                name = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
