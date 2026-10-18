namespace Ros.Transport
{
    // 客户端 → 服务器：加载完成（三步数据都收齐后发送）
    public class CSLoadingComplete
    {
    }

    public struct CSLoadingCompleteSerializer
    {
        public static bool Serialize(CSLoadingComplete value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static CSLoadingComplete Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSLoadingComplete();
        }
    }
}
