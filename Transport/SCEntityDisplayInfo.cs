using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：实体表现同步信息（高频）。
    /// 客户端不持有完整实体逻辑，仅根据该摘要更新表现。
    /// 包含：位姿 / 血量 / 动画状态与播放进度（受击=Hit 状态，死亡=Die 状态）/ Buff 列表 / 技能槽列表。
    /// 守护点等所有实体共用本结构，不再有独立 DTO。
    /// </summary>
    public class SCEntityDisplayInfo
    {
        /// <summary>实体 id。</summary>
        public ushort entityId;
        /// <summary>实体类型。</summary>
        public EntityType type;
        /// <summary>阵营。</summary>
        public EntityCamp camp;
        /// <summary>世界坐标。</summary>
        public Vector3 position;
        /// <summary>朝向（欧拉角 Y，度）。</summary>
        public float yaw;
        /// <summary>速度（米/秒，客户端包间推演用；= 权威移动方向×速度 + 位移效果速度，静止为零）。</summary>
        public Vector3 velocity;
        /// <summary>绕 Y 轴角速度（度/秒，客户端推演朝向用；静止为零）。</summary>
        public float yawSpeed;
        /// <summary>是否包含运行时数据（血量/Buff/技能槽）：高频同步(0.02s)=false 只含位姿动画，完整同步(0.2s)=true；客户端 false 时保留上一次运行时数据。</summary>
        public bool includeRuntime;
        /// <summary>当前生命（守护点 HUD 等直接读取；&lt;=0 视为已摧毁/死亡）。</summary>
        public int health;
        /// <summary>最大生命。</summary>
        public int maxHealth;
        /// <summary>当前动画状态（EntityAnim.AnimState：0Spawn 1Motion 2Attack 3Hit 4Die）。</summary>
        public int animState;
        /// <summary>动画片段标识 = 状态的 fullPathHash（两端一致，客户端据此定位并播放）。</summary>
        public int animId;
        /// <summary>动画播放进度（归一化 0~1）。</summary>
        public float animFrame;
        /// <summary>
        /// 当前正在释放的技能 id（-1 = 无）。客户端据此经 SkillManager.GetWeapon 取「使用的武器」，
        /// 从 AssetsManager 取悬浮武器模型挂到手部；技能结束（或攻击动画播完）即切回空手。
        /// </summary>
        public int castSkillId = -1;
        /// <summary>最近触发槽位下标（键盘槽位直触）（-1 无；仅对玩家实体有意义，服务器权威）。</summary>
        public int selectedIndex = -1;
        /// <summary>所属客户端 id（非玩家实体 = -1；客户端据此显示玩家名字）。</summary>
        public int ownerClientId = -1;
        /// <summary>当前 Buff 列表（部分表现需按 Buff 判断，如守护点减伤叠层/迷雾）。</summary>
        public List<BuffRuntime> buffs = new();
        /// <summary>技能槽列表（顺序即键盘槽位顺序；含装载技能与 CD 情况）。</summary>
        public List<SkillSlotRuntime> skills = new();

        /// <summary>单个 Buff 的同步数据。</summary>
        public class BuffRuntime
        {
            /// <summary>Buff 类型（EntityEffectController.EffectType 的 int 值）。</summary>
            public int type;
            /// <summary>等级/叠层。</summary>
            public int level = 1;
            /// <summary>剩余时长（秒，&lt;0 = 永久）。</summary>
            public float remainTime = -1f;
        }

        /// <summary>单个技能槽的同步数据。</summary>
        public class SkillSlotRuntime
        {
            /// <summary>技能 id（-1 空槽）。</summary>
            public int skillId = -1;
            /// <summary>武器经验（仅对局内，经验直接加成伤害）。</summary>
            public int exp;
            /// <summary>剩余 CD（秒）。</summary>
            public float cdRemain;
            /// <summary>总 CD（秒）。</summary>
            public float cdTotal;
            /// <summary>剩余库存（-1=无库存限制）。</summary>
            public int store = -1;
        }
    }

    /// <summary>SCEntityDisplayInfo 网络序列化器。</summary>
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
            if (!IntSerializer.Serialize(value.animState, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.animId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.castSkillId, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.animFrame, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.selectedIndex, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.ownerClientId, result, ref indexStart)) return false;

            int buffCount = value.buffs?.Count ?? 0;
            if (!IntSerializer.Serialize(buffCount, result, ref indexStart)) return false;
            if (value.buffs != null)
            {
                foreach (var buff in value.buffs)
                {
                    if (!BoolSerializer.Serialize(buff != null, result, ref indexStart)) return false;
                    if (buff == null) continue;
                    if (!IntSerializer.Serialize(buff.type, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(buff.level, result, ref indexStart)) return false;
                    if (!FloatSerializer.Serialize(buff.remainTime, result, ref indexStart)) return false;
                }
            }

            int skillCount = value.skills?.Count ?? 0;
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
                animState = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                castSkillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animFrame = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
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
                    remainTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
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
