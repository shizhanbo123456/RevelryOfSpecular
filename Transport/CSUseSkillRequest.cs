using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：技能释放请求（键盘技能槽 U I O L H 直接触发对应槽位技能）。
    /// </summary>
    public struct CSUseSkillRequest
    {
        /// <summary>技能 id（技能列表项：武器或角色主动技能/大招）。</summary>
        public int skillId;
        /// <summary>目标点（世界坐标，由客户端准星提供）。</summary>
        public Vector3 dest;

        public CSUseSkillRequest(int skillId, Vector3 dest)
        {
            this.skillId = skillId;
            this.dest = dest;
        }
    }

    /// <summary>CSUseSkillRequest 网络序列化器。</summary>
    public struct CSUseSkillRequestSerializer
    {
        public static bool Serialize(CSUseSkillRequest value, byte[] result, ref int indexStart)
        {
            if (!IntSerializer.Serialize(value.skillId, result, ref indexStart)) return false;
            return Vector3Serializer.Serialize(value.dest, result, ref indexStart);
        }

        public static CSUseSkillRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSUseSkillRequest()
            {
                skillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                dest = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
