using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    public class SCEntityDisplayInfo
    {
        public ushort entityId;
        public EntityType type;
        public EntityCamp camp;
        public Vector3 position;
        public float yaw;
        public Vector3 velocity;
        public float yawSpeed;
        public bool includeRuntime;
        public int health;
        public int maxHealth;
        public int selectedIndex = -1;
        public int ownerClientId = -1;
        public List<BuffRuntime> buffs = new();
        public List<SkillSlotRuntime> skills = new();

        public class BuffRuntime
        {
            public int type;
            public int level = 1;
        }

        public class SkillSlotRuntime
        {
            public int skillId = -1;
            public int exp;
            public float cdRemain;
            public float cdTotal;
            public int store = -1;
        }
    }

    public struct SCEntityDisplayInfoSerializer
    {
        public static bool Serialize(SCEntityDisplayInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize((int)value.camp, result, ref indexStart)) return false;
            if (!Vector3Serializer.Serialize(value.position, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.yaw, result, ref indexStart)) return false;
            if (!Vector3Serializer.Serialize(value.velocity, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.yawSpeed, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.includeRuntime, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.health, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.maxHealth, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.selectedIndex, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.ownerClientId, result, ref indexStart)) return false;

            int buffCount = value.buffs != null ? value.buffs.Count : 0;
            if (!IntSerializer.Serialize(buffCount, result, ref indexStart)) return false;
            if (value.buffs != null)
            {
                foreach (var buff in value.buffs)
                {
                    if (!BoolSerializer.Serialize(buff != null, result, ref indexStart)) return false;
                    if (buff == null) continue;
                    if (!IntSerializer.Serialize(buff.type, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(buff.level, result, ref indexStart)) return false;
                }
            }

            int skillCount = value.skills != null ? value.skills.Count : 0;
            if (!IntSerializer.Serialize(skillCount, result, ref indexStart)) return false;
            if (value.skills != null)
            {
                foreach (var slot in value.skills)
                {
                    if (!BoolSerializer.Serialize(slot != null, result, ref indexStart)) return false;
                    if (slot == null) continue;
                    if (!IntSerializer.Serialize(slot.skillId, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(slot.exp, result, ref indexStart)) return false;
                    if (!FloatSerializer.Serialize(slot.cdRemain, result, ref indexStart)) return false;
                    if (!FloatSerializer.Serialize(slot.cdTotal, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(slot.store, result, ref indexStart)) return false;
                }
            }
            return true;
        }

        public static SCEntityDisplayInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCEntityDisplayInfo()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                position = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
                yaw = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                velocity = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
                yawSpeed = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                includeRuntime = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                health = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                maxHealth = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                selectedIndex = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                ownerClientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            int buffCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < buffCount; i++)
            {
                bool hasBuff = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (!hasBuff) { info.buffs.Add(null); continue; }
                info.buffs.Add(new SCEntityDisplayInfo.BuffRuntime()
                {
                    type = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    level = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            int skillCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < skillCount; i++)
            {
                bool hasSlot = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (!hasSlot) { info.skills.Add(null); continue; }
                info.skills.Add(new SCEntityDisplayInfo.SkillSlotRuntime()
                {
                    skillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    exp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    cdRemain = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    cdTotal = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    store = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            return info;
        }
    }
}
