namespace Ros.Transport
{
    // 服务器 → 客户端：开始战斗（已加载完成的客户端才收；未完成的观战方完成后补发），客户端据此隐藏加载界面并切页
    public class SCBattleStart
    {
    }

    public struct SCBattleStartSerializer
    {
        public static bool Serialize(SCBattleStart value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static SCBattleStart Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCBattleStart();
        }
    }
}
