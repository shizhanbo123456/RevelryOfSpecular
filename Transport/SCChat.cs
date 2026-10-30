namespace Ros.Transport
{
    public class SCChat
    {
        public int senderClientId; // 玩家令牌（非连接 id）
        public int channel;          // 0 全频道 / 1 阵营频道
        public int camp;             // 发送方阵营：0 进攻 / 1 防守 / -1 未定
        public int characterIndex;   // 发送方角色下标（客户端取头像用）
        public string name = "";
        public string content = "";
    }

    public struct SCChatSerializer
    {
        public static bool Serialize(SCChat value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.senderClientId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.channel, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.camp, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.characterIndex, result, ref indexStart)) return false;
            if (!StringSerializer.Serialize(value.name, result, ref indexStart)) return false;
            return StringSerializer.Serialize(value.content, result, ref indexStart);
        }

        public static SCChat Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCChat()
            {
                senderClientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                channel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                characterIndex = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                name = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
                content = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
