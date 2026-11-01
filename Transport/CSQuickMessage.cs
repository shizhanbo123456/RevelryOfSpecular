using UnityEngine;

namespace Ros.Transport
{
    // 快捷消息类型，与 quick_message.csv 的 id 一一对应
    public enum QuickMessageType
    {
        Rally = 0,          // 集结
        ReleaseSignal = 1,  // 解除信号
        Attack = 2,         // 进攻
        Danger = 3,         // 危险警告
        RequestReport = 4,  // 请求上报各自状况
        ReportStatus = 5,   // 上报状况
        ContestNeutral = 6, // 争夺中立
        RequestEscort = 7,  // 请求护送
    }

    // 客户端 → 服务器：senderClientId 由服务器按连接补齐，客户端只给类型与可选位置
    public class CSQuickMessage
    {
        public int messageId;
        public bool hasTargetPosition;
        public Vector3 targetPosition;
    }

    public struct CSQuickMessageSerializer
    {
        public static bool Serialize(CSQuickMessage value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.messageId, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.hasTargetPosition, result, ref indexStart)) return false;
            if (value.hasTargetPosition && !Vector3Serializer.Serialize(value.targetPosition, result, ref indexStart)) return false;
            return true;
        }

        public static CSQuickMessage Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var value = new CSQuickMessage();
            value.messageId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.hasTargetPosition = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (value.hasTargetPosition) value.targetPosition = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
            return value;
        }
    }
}
