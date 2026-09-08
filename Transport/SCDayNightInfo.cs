namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：昼夜同步（战斗开始与阶段切换时下发）。
    /// 客户端收到后按配置的阶段时长与时间流速自行推演昼夜推进，服务器只做权威校正。
    /// </summary>
    public class SCDayNightInfo
    {
        /// <summary>昼夜阶段（0白天 1黄昏 2夜晚 3黎明）。</summary>
        public int phase;
        /// <summary>当前阶段已进行时间（秒）。</summary>
        public float phaseTime;
        /// <summary>时间流速倍率（1 = 正常；&lt;=0 视为 1）。</summary>
        public float rate = 1f;
    }

    /// <summary>SCDayNightInfo 网络序列化器。</summary>
    public struct SCDayNightInfoSerializer
    {
        public static bool Serialize(SCDayNightInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.phase, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.phaseTime, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.rate, result, ref indexStart);
        }

        public static SCDayNightInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCDayNightInfo()
            {
                phase = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                phaseTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                rate = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
