namespace Ros.Transport
{
    // 服务器 → 客户端：AI 操控的服务器侧回执（目前只有移动失败：目的地算不出路）
    public class SCAiFeedback
    {
        public int kind;        // 0 移动失败
        public float posX;      // 实体当前位置
        public float posZ;
        public int targetId;    // 失败时下发的目标实体 id（移动意图为 0）
    }

    public struct SCAiFeedbackSerializer
    {
        public static bool Serialize(SCAiFeedback value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.kind, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.posX, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.posZ, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.targetId, result, ref indexStart);
        }

        public static SCAiFeedback Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCAiFeedback()
            {
                kind = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posX = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posZ = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                targetId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
