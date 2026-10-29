using System;

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
        public bool hasTransform; // 本包含有效位姿（position/yaw/velocity/yawSpeed）；无变化时整包可能不下发
        public bool hasHealth;   // 本包含有效 health/maxHealth（实体 health 脏标记置位 / 完整同步 / 首见）
        public bool hasBuffs;    // 本包含有效 buffs（实体 buff 脏标记置位 / 完整同步 / 首见）
        public bool hasSkills;   // 本包含有效 skills（实体 skill 脏标记置位 / 完整同步 / 首见）；仅技能 id，无冷却/库存
        public int health;
        public int maxHealth;
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
        }
    }

    public struct SCEntityDisplayInfoSerializer
    {
        public static bool Serialize(SCEntityDisplayInfo value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
                if (!EntityTypeSerializer.Serialize(value.type, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize((int)value.camp, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(value.includeRuntime, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(value.hasTransform, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(value.hasHealth, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(value.hasBuffs, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(value.hasSkills, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.health, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.maxHealth, result, ref indexStart)) return false;

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
                    }
                }
                if (!value.hasTransform) return true;
                if (!Vector3Serializer.Serialize(value.position, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.yaw, result, ref indexStart)) return false;
                if (!Vector3Serializer.Serialize(value.velocity, result, ref indexStart)) return false;
                return FloatSerializer.Serialize(value.yawSpeed, result, ref indexStart);
            }
            else
            {
                // 位级：id16+type11+camp5+5flag+health17*2+列表+可选位姿45/12/33/11
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.entityId, 0, 65535);
                    BitField.WriteInt((int)value.type.category, 0, 7);
                    BitField.WriteInt(value.type.value, 0, 255);
                    BitField.WriteInt((int)value.camp, 0, 31);
                    BitField.WriteBool(value.includeRuntime);
                    BitField.WriteBool(value.hasTransform);
                    BitField.WriteBool(value.hasHealth);
                    BitField.WriteBool(value.hasBuffs);
                    BitField.WriteBool(value.hasSkills);
                    BitField.WriteInt(value.health, 0, 100000);
                    BitField.WriteInt(value.maxHealth, 0, 100000);

                    int buffCount = value.buffs != null ? value.buffs.Count : 0;
                    BitField.WriteInt(buffCount, 0, 15);
                    if (value.buffs != null)
                    {
                        foreach (var buff in value.buffs)
                        {
                            BitField.WriteBool(buff != null);
                            if (buff == null) continue;
                            BitField.WriteInt(buff.type, 0, 255);
                            BitField.WriteInt(buff.level, 0, 15);
                        }
                    }

                    int skillCount = value.skills != null ? value.skills.Count : 0;
                    BitField.WriteInt(skillCount, 0, 15);
                    if (value.skills != null)
                    {
                        foreach (var slot in value.skills)
                        {
                            BitField.WriteBool(slot != null);
                            if (slot == null) continue;
                            BitField.WriteInt(slot.skillId, -1, 255);
                        }
                    }

                    if (value.hasTransform)
                    {
                        BitField.WriteFloat(value.position.x, -500f, 500f, 0.05f);
                        BitField.WriteFloat(value.position.y, -500f, 500f, 0.05f);
                        BitField.WriteFloat(value.position.z, -500f, 500f, 0.05f);
                        BitField.WriteFloat(value.yaw, 0f, 360f, 0.1f);
                        BitField.WriteFloat(value.velocity.x, -50f, 50f, 0.05f);
                        BitField.WriteFloat(value.velocity.y, -50f, 50f, 0.05f);
                        BitField.WriteFloat(value.velocity.z, -50f, 50f, 0.05f);
                        BitField.WriteFloat(value.yawSpeed, -720f, 720f, 1f);
                    }
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCEntityDisplayInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                var info = new SCEntityDisplayInfo()
                {
                    entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    includeRuntime = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    hasTransform = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    hasHealth = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    hasBuffs = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    hasSkills = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    health = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    maxHealth = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
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
                    });
                }
                if (!info.hasTransform) return info;
                info.position = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
                info.yaw = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
                info.velocity = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
                info.yawSpeed = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
                return info;
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCEntityDisplayInfo 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }

                var r = new SCEntityDisplayInfo();
                r.entityId = (ushort)BitField.ReadInt(0, 65535);
                r.type = new EntityType((EntityCategory)BitField.ReadInt(0, 7), BitField.ReadInt(0, 255));
                r.camp = (EntityCamp)BitField.ReadInt(0, 31);
                r.includeRuntime = BitField.ReadBool();
                r.hasTransform = BitField.ReadBool();
                r.hasHealth = BitField.ReadBool();
                r.hasBuffs = BitField.ReadBool();
                r.hasSkills = BitField.ReadBool();
                r.health = BitField.ReadInt(0, 100000);
                r.maxHealth = BitField.ReadInt(0, 100000);

                int buffCount = BitField.ReadInt(0, 15);
                for (int i = 0; i < buffCount; i++)
                {
                    if (!BitField.ReadBool()) { r.buffs.Add(null); continue; }
                    r.buffs.Add(new SCEntityDisplayInfo.BuffRuntime()
                    {
                        type = BitField.ReadInt(0, 255),
                        level = BitField.ReadInt(0, 15),
                    });
                }

                int skillCount = BitField.ReadInt(0, 15);
                for (int i = 0; i < skillCount; i++)
                {
                    if (!BitField.ReadBool()) { r.skills.Add(null); continue; }
                    r.skills.Add(new SCEntityDisplayInfo.SkillSlotRuntime()
                    {
                        skillId = BitField.ReadInt(-1, 255),
                    });
                }

                if (r.hasTransform)
                {
                    r.position.x = BitField.ReadFloat(-500f, 500f, 0.05f);
                    r.position.y = BitField.ReadFloat(-500f, 500f, 0.05f);
                    r.position.z = BitField.ReadFloat(-500f, 500f, 0.05f);
                    r.yaw = BitField.ReadFloat(0f, 360f, 0.1f);
                    r.velocity.x = BitField.ReadFloat(-50f, 50f, 0.05f);
                    r.velocity.y = BitField.ReadFloat(-50f, 50f, 0.05f);
                    r.velocity.z = BitField.ReadFloat(-50f, 50f, 0.05f);
                    r.yawSpeed = BitField.ReadFloat(-720f, 720f, 1f);
                }
                indexStart += BitField.ReadByteLength;
                return r;
            }
        }
    }
}
