using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：实体表现同步信息（高频）。
    /// 客户端不持有完整实体逻辑，仅根据该摘要更新表现（模型/动画/血条）。
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
        /// <summary>当前生命。</summary>
        public int health;
        /// <summary>最大生命。</summary>
        public int maxHealth;
        /// <summary>是否移动。</summary>
        public bool moving;
        /// <summary>是否在空中。</summary>
        public bool inAir;
        /// <summary>是否滑铲中。</summary>
        public bool sliding;
        /// <summary>攻击动作（EntityAnim.AttackType，0=无）。</summary>
        public int attackType;
        /// <summary>是否死亡。</summary>
        public bool dead;
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
            if (!IntSerializer.Serialize(value.health, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.maxHealth, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.moving, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.inAir, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.sliding, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.attackType, result, ref indexStart)) return false;
            return BoolSerializer.Serialize(value.dead, result, ref indexStart);
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
                health = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                maxHealth = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                moving = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                inAir = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sliding = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackType = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                dead = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            return info;
        }
    }
}
