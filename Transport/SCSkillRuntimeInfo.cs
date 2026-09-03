using System.Collections.Generic;

namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：技能运行时信息（技能列表/选中项/CD/库存/武器经验）。
    /// 滚轮循环的技能列表由服务器权威下发。
    /// </summary>
    public class SCSkillRuntimeInfo
    {
        /// <summary>单个技能槽位运行时数据。</summary>
        public class SkillSlotRuntime
        {
            /// <summary>技能 id。</summary>
            public int skillId = -1;
            /// <summary>武器经验（仅对局内；武器无等级，经验直接加成伤害，见策划案 11.4）。</summary>
            public int exp;
            /// <summary>剩余 CD（秒）。</summary>
            public float cdRemain;
            /// <summary>总 CD（秒）。</summary>
            public float cdTotal;
            /// <summary>剩余库存（可释放次数，-1=无库存限制）。</summary>
            public int store = -1;
            /// <summary>该技能是否为远程/施法类（决定右键是否可触发）。</summary>
            public bool ranged;
            /// <summary>该技能是否有武器显示（悬浮武器）。</summary>
            public bool hasWeaponDisplay;
        }

        /// <summary>当前滚轮选中槽位下标（-1 无）。</summary>
        public int selectedIndex = -1;
        /// <summary>技能槽位列表（顺序即滚轮循环顺序）。</summary>
        public List<SkillSlotRuntime> slots = new();

        /// <summary>便利：按槽位下标取槽位。</summary>
        public SkillSlotRuntime Get(int index)
        {
            if (slots == null || index < 0 || index >= slots.Count) return null;
            return slots[index];
        }
    }

    /// <summary>SCSkillRuntimeInfo 网络序列化器。</summary>
    public struct SCSkillRuntimeInfoSerializer
    {
        public static bool Serialize(SCSkillRuntimeInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.selectedIndex, result, ref indexStart)) return false;
            int count = value.slots?.Count ?? 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.slots == null) return true;
            foreach (var slot in value.slots)
            {
                if (!BoolSerializer.Serialize(slot != null, result, ref indexStart)) return false;
                if (slot == null) continue;
                if (!IntSerializer.Serialize(slot.skillId, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(slot.exp, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(slot.cdRemain, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(slot.cdTotal, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(slot.store, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(slot.ranged, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(slot.hasWeaponDisplay, result, ref indexStart)) return false;
            }
            return true;
        }

        public static SCSkillRuntimeInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCSkillRuntimeInfo()
            {
                selectedIndex = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                bool notNull = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (!notNull)
                {
                    info.slots.Add(null);
                    continue;
                }
                info.slots.Add(new SCSkillRuntimeInfo.SkillSlotRuntime()
                {
                    skillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    exp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    cdRemain = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    cdTotal = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    store = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    ranged = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    hasWeaponDisplay = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            return info;
        }
    }
}
