namespace Ros.Transport
{
    // 客户端 → 服务器：请求防守方玩家表（加载期第 2 步）
    public class CSLoadingDefensePlayerRequest
    {
    }

    public struct CSLoadingDefensePlayerRequestSerializer
    {
        public static bool Serialize(CSLoadingDefensePlayerRequest value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static CSLoadingDefensePlayerRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSLoadingDefensePlayerRequest();
        }
    }
}
