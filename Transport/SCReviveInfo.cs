namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：复活进度信息（复活进度系统，见策划案 13/14 章）。
    /// 白天积累快、夜晚极慢；死亡次数越多越慢；愈战愈勇按死亡次数叠层。
    /// </summary>
    public class SCReviveInfo
    {
        /// <summary>死亡实体 id。</summary>
        public ushort entityId;
        /// <summary>复活进度（0~1，攒满即可复活）。</summary>
        public float progress;
        /// <summary>是否已可复活（攒满，等待黎明统一复活时为 false）。</summary>
        public bool ready;
        /// <summary>累计死亡次数（愈战愈勇层数依据）。</summary>
        public int deadCount;
        /// <summary>愈战愈勇当前叠层（0~max）。</summary>
        public int yzStack;
    }

    /// <summary>SCReviveInfo 网络序列化器。</summary>
    public struct SCReviveInfoSerializer
    {
        public static bool Serialize(SCReviveInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.progress, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.ready, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.deadCount, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.yzStack, result, ref indexStart);
        }

        public static SCReviveInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCReviveInfo()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                progress = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                ready = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                deadCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                yzStack = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
