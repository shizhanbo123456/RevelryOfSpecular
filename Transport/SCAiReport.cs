using UnityEngine;

namespace Ros.Transport
{
    // 上报状态：聊天文本由客户端本地拼，协议只传枚举值，不传字符串
    public enum AiReportState
    {
        None = 0,
        Search,
        SearchCombat,
        Chase,
        Combat,
        EscortApproach,
        Escort,
        EscortCombat,
        DangerEvade,
        DangerClear,
        DangerClearCombat,
        RallyApproach,
        RallyWait,
        RallyGuard,
        Assault,
        AssaultCombat,
    }

    // 服务器 → 客户端：同阵营的状态上报（人机或真人均可发）
    public class SCAiReport
    {
        public int senderClientId;
        public int state;          // AiReportState
        public int healthPercent;  // 0~100
        public int camp;           // 0 进攻 / 1 防守 / -1 未定
        public int characterIndex; // 头像下标，-1 无
        public Vector3 position;   // 上报时位置
    }

    public struct SCAiReportSerializer
    {
        public static bool Serialize(SCAiReport value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.senderClientId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.state, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.healthPercent, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.camp, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.characterIndex, result, ref indexStart)) return false;
            return Vector3Serializer.Serialize(value.position, result, ref indexStart);
        }

        public static SCAiReport Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var value = new SCAiReport();
            value.senderClientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.state = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.healthPercent = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.characterIndex = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.position = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
            return value;
        }
    }
}
