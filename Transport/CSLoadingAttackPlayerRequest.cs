namespace Ros.Transport
{
    // 客户端 → 服务器：请求进攻方玩家表（加载期第 1 步）
    public class CSLoadingAttackPlayerRequest
    {
    }

    public struct CSLoadingAttackPlayerRequestSerializer
    {
        public static bool Serialize(CSLoadingAttackPlayerRequest value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static CSLoadingAttackPlayerRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSLoadingAttackPlayerRequest();
        }
    }
}
