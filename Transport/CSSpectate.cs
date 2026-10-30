namespace Ros.Transport
{
    // 客户端 → 服务器：观战请求。targetClientId = 目标玩家 id（须同阵营且存活）；
    // 传入非法 / 死亡 / 异阵营目标 = 退出观战
    public class CSSpectate
    {
        public int targetClientId;
    }

    public struct CSSpectateSerializer
    {
        public static bool Serialize(CSSpectate value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return IntSerializer.Serialize(value.targetClientId, result, ref indexStart);
        }

        public static CSSpectate Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSSpectate()
            {
                targetClientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
