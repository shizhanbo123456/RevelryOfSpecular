using UnityEngine;

namespace Ros.Transport
{
    // 服务器 → 客户端：同一阵营的快捷消息广播（人机在服务器侧直接消费，不下发）
    public class SCQuickMessage
    {
        public int messageId;
        public int senderClientId;
        public bool hasTargetPosition;
        public Vector3 targetPosition;
        public string extra = "";   // 附加上报文本（上报状况用，其余为空）
    }

    public struct SCQuickMessageSerializer
    {
        public static bool Serialize(SCQuickMessage value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.messageId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.senderClientId, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.hasTargetPosition, result, ref indexStart)) return false;
            if (value.hasTargetPosition && !Vector3Serializer.Serialize(value.targetPosition, result, ref indexStart)) return false;
            return StringSerializer.Serialize(value.extra, result, ref indexStart);
        }

        public static SCQuickMessage Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var value = new SCQuickMessage();
            value.messageId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.senderClientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            value.hasTargetPosition = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (value.hasTargetPosition) value.targetPosition = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
            value.extra = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            return value;
        }
    }
}
