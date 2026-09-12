namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：昼夜同步快照（战斗开始、时长/时间被改动、以及每 Config.daynight_sync_interval 秒心跳时下发）。
    /// 三个摘要数据即可让客户端完整复现推演：周期时间 + 白天时长 + 晚上时长。
    /// **方向不需要单独传**：cycleTime &lt; 1 表示正在变亮，≥ 1 表示正在变暗。
    /// 客户端收到后按这三个参数自行推进；昼夜时长发生变化时会收到新快照并按新速率继续，不会跳变当前时间。
    /// </summary>
    public class SCDayNightInfo
    {
        /// <summary>归一化周期时间 [0,2)：0 与 2 = 午夜，1 = 正午；[0,1) 变亮、[1,2) 变暗。</summary>
        public float cycleTime = 1f;
        /// <summary>白天时长（秒）：光照值 ≥ 0.5 区间的总耗时。</summary>
        public float dayDuration = 120f;
        /// <summary>晚上时长（秒）：光照值 &lt; 0.5 区间的总耗时。</summary>
        public float nightDuration = 60f;
    }

    /// <summary>SCDayNightInfo 网络序列化器。</summary>
    public struct SCDayNightInfoSerializer
    {
        public static bool Serialize(SCDayNightInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!FloatSerializer.Serialize(value.cycleTime, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.dayDuration, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.nightDuration, result, ref indexStart);
        }

        public static SCDayNightInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCDayNightInfo()
            {
                cycleTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                dayDuration = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                nightDuration = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
