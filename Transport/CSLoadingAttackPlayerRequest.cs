namespace Ros.Transport
{
    // 客户端 → 服务器：请求进攻方玩家表（加载期第 1 步）
    public class CSLoadingAttackPlayerRequest
    {
    }

    public struct CSLoadingAttackPlayerRequestSerializer
    {
        // 空包：无内容，不占字节，与位传输开关无关
        public static bool Serialize(CSLoadingAttackPlayerRequest value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static CSLoadingAttackPlayerRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSLoadingAttackPlayerRequest();
        }
    }
}
