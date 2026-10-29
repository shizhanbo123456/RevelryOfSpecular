namespace Ros.Transport
{
    // 客户端 → 服务器：加载完成（三步数据都收齐后发送）
    public class CSLoadingComplete
    {
    }

    public struct CSLoadingCompleteSerializer
    {
        // 空包：无内容，不占字节，与位传输开关无关
        public static bool Serialize(CSLoadingComplete value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static CSLoadingComplete Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSLoadingComplete();
        }
    }
}
