using UnityEngine;

namespace Ros.Transport
{
    // 客户端 → 服务器：上帝视角当前显示区域（左下角 + 右上角围成的包围盒，世界 XZ）与请求序号
    // 服务器按 seq 原样回填，客户端据此拼块并丢弃上一轮残留
    public class CSGodViewArea
    {
        public Vector2 min;
        public Vector2 max;
        public byte seq;
    }

    public struct CSGodViewAreaSerializer
    {
        public static bool Serialize(CSGodViewArea value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!Vector2Serializer.Serialize(value.min, result, ref indexStart)) return false;
            if (!Vector2Serializer.Serialize(value.max, result, ref indexStart)) return false;
            return ByteSerializer.Serialize(value.seq, result, ref indexStart);
        }

        public static CSGodViewArea Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSGodViewArea()
            {
                min = Vector2Serializer.Deserialize(data, ref indexStart, invalidIndex),
                max = Vector2Serializer.Deserialize(data, ref indexStart, invalidIndex),
                seq = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
