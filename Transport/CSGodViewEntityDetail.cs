namespace Ros.Transport
{
    // 客户端 → 服务器：上帝视角里点中某个实体，请求它的详情（一次性快照）
    public class CSGodViewEntityDetail
    {
        public ushort entityId;
    }

    public struct CSGodViewEntityDetailSerializer
    {
        public static bool Serialize(CSGodViewEntityDetail value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return UshortSerializer.Serialize(value.entityId, result, ref indexStart);
        }

        public static CSGodViewEntityDetail Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSGodViewEntityDetail()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
