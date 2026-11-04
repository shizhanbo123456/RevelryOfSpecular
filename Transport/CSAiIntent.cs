namespace Ros.Transport
{
    // 客户端 → 服务器：AI 操控意图（站立 / 移动到位置 / 攻击目标 / 停止），服务器翻译成行为层请求
    public class CSAiIntent
    {
        public int intent;      // 0 站立 / 1 移动到位置 / 2 攻击目标 / 3 停止
        public float posX;      // 移动到位置用
        public float posZ;
        public int targetId;    // 攻击目标的实体 id
    }

    public struct CSAiIntentSerializer
    {
        public static bool Serialize(CSAiIntent value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.intent, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.posX, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.posZ, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.targetId, result, ref indexStart);
        }

        public static CSAiIntent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSAiIntent()
            {
                intent = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posX = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posZ = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                targetId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
