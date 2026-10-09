namespace Ros.Transport
{
    // 服务器 → 客户端：飘字提示（messageId = NoticeMessageMap 消息 id，客户端查表显示，颜色由 FGUI 预设）
    public class SCPrompt
    {
        public int messageId;
    }

    public struct SCPromptSerializer
    {
        public static bool Serialize(SCPrompt value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return IntSerializer.Serialize(value.messageId, result, ref indexStart);
        }

        public static SCPrompt Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCPrompt()
            {
                messageId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
