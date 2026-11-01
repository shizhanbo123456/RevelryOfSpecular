namespace Ros.Transport
{
    public class SCReviveInfo
    {
        public int clientId;      // 归属玩家：客户端据此判定是不是自己（不依赖实体 id）
        public int deathCount;    // 第几次死亡：客户端查 revive_time.csv 得到复活总时长
        public float progress;    // 本段起点进度：死亡为 0，重连补发为服务器当前进度
        public bool ready;        // 复活完成，客户端收起进度条
        public ushort entityId;   // 复活后的新实体 id（死亡包为 0）
        public int furyStack;     // 愈战愈勇层数
    }

    public struct SCReviveInfoSerializer
    {
        public static bool Serialize(SCReviveInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.clientId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.deathCount, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.progress, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.ready, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.furyStack, result, ref indexStart);
        }

        public static SCReviveInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCReviveInfo()
            {
                clientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                deathCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                progress = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                ready = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                furyStack = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
