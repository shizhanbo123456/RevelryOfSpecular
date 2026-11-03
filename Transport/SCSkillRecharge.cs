using System.Collections.Generic;

namespace Ros.Transport
{
    // 服务器 → 客户端：本人技能栏快照（槽位技能 id 与各自的 cd 根时间戳一一对应）
    // 只在槽位改变或释放成功时下发一次，客户端按根戳自行算进度，不做每帧同步
    public class SCSkillRecharge
    {
        public List<int> skillIds = new();
        public List<float> roots = new();
    }

    public struct SCSkillRechargeSerializer
    {
        public static bool Serialize(SCSkillRecharge value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            int count = value.skillIds != null ? value.skillIds.Count : 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.skillIds == null) return true;
            for (int i = 0; i < count; i++)
            {
                if (!IntSerializer.Serialize(value.skillIds[i], result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.roots[i], result, ref indexStart)) return false;
            }
            return true;
        }

        public static SCSkillRecharge Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var r = new SCSkillRecharge();
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                r.skillIds.Add(IntSerializer.Deserialize(data, ref indexStart, invalidIndex));
                r.roots.Add(FloatSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            return r;
        }
    }
}
