namespace Ros.Transport
{
    public class CSChat
    {
        public int channel;      // 0 全频道 / 1 阵营频道
        public string content = "";
    }

    public struct CSChatSerializer
    {
        public static bool Serialize(CSChat value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.channel, result, ref indexStart)) return false;
            return StringSerializer.Serialize(value.content, result, ref indexStart);
        }

        public static CSChat Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSChat()
            {
                channel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                content = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
