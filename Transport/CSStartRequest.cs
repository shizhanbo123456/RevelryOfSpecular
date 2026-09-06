namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：请求开始对局。
    /// 服务器校验：所有玩家已选队伍，且双方人数（人类 + AI）均 &gt; 0；不通过则回 ShowText 提示。
    /// </summary>
    public class CSStartRequest
    {
    }

    /// <summary>CSStartRequest 网络序列化器。</summary>
    public struct CSStartRequestSerializer
    {
        public static bool Serialize(CSStartRequest value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static CSStartRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSStartRequest();
        }
    }
}
