namespace Ros.Transport
{
    // 客户端 → 服务器：请求防守方玩家表（加载期第 2 步）
    public class CSLoadingDefensePlayerRequest
    {
    }

    public struct CSLoadingDefensePlayerRequestSerializer
    {
        // 空包：无内容，不占字节，与位传输开关无关
        public static bool Serialize(CSLoadingDefensePlayerRequest value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static CSLoadingDefensePlayerRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSLoadingDefensePlayerRequest();
        }
    }
}
