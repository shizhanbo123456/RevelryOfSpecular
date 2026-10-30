namespace Ros.Transport
{
    // 客户端 → 服务器：按下的技能 id。技能栏只有客户端记录，服务器只校验该技能是否持有
    public struct CSSkillCast
    {
        public int skillId;
    }

    public struct CSSkillCastSerializer
    {
        public static bool Serialize(CSSkillCast value, byte[] result, ref int indexStart)
        {
            return IntSerializer.Serialize(value.skillId, result, ref indexStart);
        }

        public static CSSkillCast Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSSkillCast()
            {
                skillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
