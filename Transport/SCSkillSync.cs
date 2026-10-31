using System.Collections.Generic;

namespace Ros.Transport
{
    // 服务器 → 客户端：本人持有技能。full = 全量替换（开局 / 重连补发），false = 增量新增（掉落 / GM）
    public class SCSkillSync
    {
        public bool full;
        public List<int> skills = new();
    }

    public struct SCSkillSyncSerializer
    {
        public static bool Serialize(SCSkillSync value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!BoolSerializer.Serialize(value.full, result, ref indexStart)) return false;
            int count = value.skills != null ? value.skills.Count : 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.skills == null) return true;
            foreach (var skillId in value.skills)
            {
                if (!IntSerializer.Serialize(skillId, result, ref indexStart)) return false;
            }
            return true;
        }

        public static SCSkillSync Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var r = new SCSkillSync
            {
                full = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                r.skills.Add(IntSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            return r;
        }
    }
}
