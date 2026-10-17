using System;

namespace Ros.Transport
{
    // 客户端 → 服务器：延迟测量探测（客户端带上本地时刻，服务器原样带回并附服务器时刻）
    public class CSLatency
    {
        public float clientTime;
    }

    public struct CSLatencySerializer
    {
        public static bool Serialize(CSLatency value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return FloatSerializer.Serialize(value.clientTime, result, ref indexStart);
        }

        public static CSLatency Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSLatency()
            {
                clientTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }

    // 服务器 → 客户端：延迟测量回包（clientTime 原样带回，serverTime = 服务器 Time.time）
    public class SCLatency
    {
        public float clientTime;
        public float serverTime;
    }

    public struct SCLatencySerializer
    {
        public static bool Serialize(SCLatency value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!FloatSerializer.Serialize(value.clientTime, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.serverTime, result, ref indexStart);
        }

        public static SCLatency Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCLatency()
            {
                clientTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                serverTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
