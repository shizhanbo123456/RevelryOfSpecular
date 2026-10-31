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
        public int health;
        public int maxHealth;
        public List<BuffRuntime> buffs = new();

        public class BuffRuntime
        {
            public int type;
            public int level = 1;
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

                if (value.hasHealth)
                {
                    if (!IntSerializer.Serialize(value.health, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(value.maxHealth, result, ref indexStart)) return false;
                }

                if (value.hasBuffs)
                {
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
                }

                if (!value.hasTransform) return true;
                if (!Vector3Serializer.Serialize(value.position, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.yaw, result, ref indexStart)) return false;
                if (!Vector3Serializer.Serialize(value.velocity, result, ref indexStart)) return false;
                return FloatSerializer.Serialize(value.yawSpeed, result, ref indexStart);
            }
            else
            {
                // 位级：1+15+3+5+5+4flag + [health16*2] + [buffCount4+11/buff] + [位姿81]
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.entityId, 0, SyncRanges.EntityIdMax);
                    BitField.WriteInt((int)value.type.category, 0, 7);
                    BitField.WriteInt(value.type.value, 0, 31);
                    BitField.WriteInt((int)value.camp, 0, 31);
                    BitField.WriteBool(value.includeRuntime);
                    BitField.WriteBool(value.hasTransform);
                    BitField.WriteBool(value.hasHealth);
                    BitField.WriteBool(value.hasBuffs);

                    if (value.hasHealth)
                    {
                        BitField.WriteInt(value.health, 0, SyncRanges.MaxHealth);
                        BitField.WriteInt(value.maxHealth, 0, SyncRanges.MaxHealth);
                    }

                    if (value.hasBuffs)
                    {
                        // 条数按配表上限截断：写出的条数必须与计数一致，否则读端错位
                        int buffCount = value.buffs != null ? value.buffs.Count : 0;
                        if (buffCount > SyncRanges.MaxBuffCount) buffCount = SyncRanges.MaxBuffCount;
                        BitField.WriteInt(buffCount, 0, SyncRanges.MaxBuffCount);
                        for (int i = 0; i < buffCount; i++)
                        {
                            var buff = value.buffs[i];
                            BitField.WriteBool(buff != null);
                            if (buff == null) continue;
                            BitField.WriteInt(buff.type, 0, 63);
                            BitField.WriteInt(buff.level, 0, 15);
                        }
                    }

                    if (value.hasTransform)
                    {
                        BitField.WriteFloat(value.position.x, 0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                        BitField.WriteFloat(value.position.y, 0f, SyncRanges.PositionYMax, SyncRanges.PositionYStep);
                        BitField.WriteFloat(value.position.z, 0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                        BitField.WriteFloat(value.yaw, 0f, SyncRanges.YawMax, SyncRanges.YawStep);
                        BitField.WriteFloat(value.velocity.x, -SyncRanges.VelocityXZRange, SyncRanges.VelocityXZRange, SyncRanges.VelocityXZStep);
                        BitField.WriteFloat(value.velocity.y, -SyncRanges.VelocityYRange, SyncRanges.VelocityYRange, SyncRanges.VelocityYStep);
                        BitField.WriteFloat(value.velocity.z, -SyncRanges.VelocityXZRange, SyncRanges.VelocityXZRange, SyncRanges.VelocityXZStep);
                        BitField.WriteFloat(value.yawSpeed, -SyncRanges.YawSpeedRange, SyncRanges.YawSpeedRange, SyncRanges.YawSpeedStep);
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
                };
                if (info.hasHealth)
                {
                    info.health = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                    info.maxHealth = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                }
                if (info.hasBuffs)
                {
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
                r.entityId = (ushort)BitField.ReadInt(0, SyncRanges.EntityIdMax);
                r.type = new EntityType((EntityCategory)BitField.ReadInt(0, 7), BitField.ReadInt(0, 31));
                r.camp = (EntityCamp)BitField.ReadInt(0, 31);
                r.includeRuntime = BitField.ReadBool();
                r.hasTransform = BitField.ReadBool();
                r.hasHealth = BitField.ReadBool();
                r.hasBuffs = BitField.ReadBool();

                if (r.hasHealth)
                {
                    r.health = BitField.ReadInt(0, SyncRanges.MaxHealth);
                    r.maxHealth = BitField.ReadInt(0, SyncRanges.MaxHealth);
                }

                if (r.hasBuffs)
                {
                    int buffCount = BitField.ReadInt(0, SyncRanges.MaxBuffCount);
                    for (int i = 0; i < buffCount; i++)
                    {
                        if (!BitField.ReadBool()) { r.buffs.Add(null); continue; }
                        r.buffs.Add(new SCEntityDisplayInfo.BuffRuntime()
                        {
                            type = BitField.ReadInt(0, 63),
                            level = BitField.ReadInt(0, 15),
                        });
                    }
                }

                if (r.hasTransform)
                {
                    r.position.x = BitField.ReadFloat(0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                    r.position.y = BitField.ReadFloat(0f, SyncRanges.PositionYMax, SyncRanges.PositionYStep);
                    r.position.z = BitField.ReadFloat(0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                    r.yaw = BitField.ReadFloat(0f, SyncRanges.YawMax, SyncRanges.YawStep);
                    r.velocity.x = BitField.ReadFloat(-SyncRanges.VelocityXZRange, SyncRanges.VelocityXZRange, SyncRanges.VelocityXZStep);
                    r.velocity.y = BitField.ReadFloat(-SyncRanges.VelocityYRange, SyncRanges.VelocityYRange, SyncRanges.VelocityYStep);
                    r.velocity.z = BitField.ReadFloat(-SyncRanges.VelocityXZRange, SyncRanges.VelocityXZRange, SyncRanges.VelocityXZStep);
                    r.yawSpeed = BitField.ReadFloat(-SyncRanges.YawSpeedRange, SyncRanges.YawSpeedRange, SyncRanges.YawSpeedStep);
                }
                indexStart += BitField.ReadByteLength;
                return r;
            }
        }
    }
}
