namespace Ros.Transport
{
    // 客户端 → 服务器：请求 AI 单位表（加载期第 3 步）
    public class CSLoadingAiUnitRequest
    {
    }

    public struct CSLoadingAiUnitRequestSerializer
    {
        public static bool Serialize(CSLoadingAiUnitRequest value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static CSLoadingAiUnitRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSLoadingAiUnitRequest();
        }
    }
}
