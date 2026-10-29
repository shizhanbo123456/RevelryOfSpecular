namespace Ros.Transport
{
    // 客户端 → 服务器：请求 AI 单位表（加载期第 3 步）
    public class CSLoadingAiUnitRequest
    {
    }

    public struct CSLoadingAiUnitRequestSerializer
    {
        // 空包：无内容，不占字节，与位传输开关无关
        public static bool Serialize(CSLoadingAiUnitRequest value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static CSLoadingAiUnitRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSLoadingAiUnitRequest();
        }
    }
}
