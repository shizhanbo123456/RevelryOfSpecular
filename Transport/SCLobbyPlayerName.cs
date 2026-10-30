namespace Ros.Transport
{
    // 服务器 → 客户端：单个玩家的名字（组队期专用；与成员列表分开下发，避免成员包被长名字撑大）
    public class SCLobbyPlayerName
    {
        public int clientId; // 玩家令牌（非连接 id）
        public string name = "";
    }

    public struct SCLobbyPlayerNameSerializer
    {
        public static bool Serialize(SCLobbyPlayerName value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.clientId, result, ref indexStart)) return false;
            return StringSerializer.Serialize(value.name, result, ref indexStart);
        }

        public static SCLobbyPlayerName Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCLobbyPlayerName()
            {
                clientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                name = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
