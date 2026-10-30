namespace Ros.Transport
{
    // 服务器 → 客户端：技能受理成功，仅发给施法者本人；客户端收到才扣一次可用次数
    public class SCSkillUsed
    {
        public int skillId;
    }

    public struct SCSkillUsedSerializer
    {
        public static bool Serialize(SCSkillUsed value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return IntSerializer.Serialize(value.skillId, result, ref indexStart);
        }

        public static SCSkillUsed Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCSkillUsed()
            {
                skillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
