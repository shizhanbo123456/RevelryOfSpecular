namespace Ros.Transport
{
    using UnityEngine;

    // 服务器 → 客户端：伤害飘字（高频包，只传数字与位置；value 暴击取负、0=无效，样式由 FGUI 预设档位决定）
    public class SCDamage
    {
        public int value;
        public int targetId;     // 受击实体 id（无命中点时按实体头顶定位）
        public bool hasHitPos;
        public Vector3 hitPos;   // 命中点（可选，客户端做水平散布后投影）
    }

    public struct SCDamageSerializer
    {
        public static bool Serialize(SCDamage value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.value, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.targetId, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.hasHitPos, result, ref indexStart)) return false;
            if (value.hasHitPos && !Vector3Serializer.Serialize(value.hitPos, result, ref indexStart)) return false;
            return true;
        }

        public static SCDamage Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var d = new SCDamage()
            {
                value = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                targetId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            d.hasHitPos = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (d.hasHitPos) d.hitPos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
            return d;
        }
    }
}
