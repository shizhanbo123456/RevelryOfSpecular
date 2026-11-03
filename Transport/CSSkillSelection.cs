using System.Collections.Generic;

namespace Ros.Transport
{
    // 客户端 → 服务器：本人技能栏选择，顺序即按键槽位顺序
    public class CSSkillSelection
    {
        public List<int> skillIds = new();
    }

    public struct CSSkillSelectionSerializer
    {
        public static bool Serialize(CSSkillSelection value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            int count = value.skillIds != null ? value.skillIds.Count : 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.skillIds == null) return true;
            foreach (var skillId in value.skillIds)
            {
                if (!IntSerializer.Serialize(skillId, result, ref indexStart)) return false;
            }
            return true;
        }

        public static CSSkillSelection Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var r = new CSSkillSelection();
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                r.skillIds.Add(IntSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            return r;
        }
    }
}
