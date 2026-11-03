using System.Collections.Generic;

namespace Ros.Transport
{
    // 服务器 → 客户端：上帝视角下点选的实体详情（一次性快照，不随帧刷新）
    // 技能分两组：技能栏槽位带当前冷却与库存；未装备的持有技能只给 id
    public class SCGodViewEntityDetail
    {
        public ushort entityId;   // 0 = 实体已不存在（客户端收起详情面板）
        public EntityType type;
        public EntityCamp camp;
        public int level;
        public string name = "";
        public int health;
        public int maxHealth;
        public List<Buff> buffs = new();
        public List<SlotSkill> slotSkills = new();
        public List<ushort> heldSkills = new();

        public struct Buff
        {
            public byte type;  // effect.csv 主键
            public byte level;
        }

        public struct SlotSkill
        {
            public ushort skillId;
            public byte recharge01; // 0~100，本轮恢复进度
            public int store;       // 剩余可用次数，-1 = 无限库存
        }
    }

    public struct SCGodViewEntityDetailSerializer
    {
        public static bool Serialize(SCGodViewEntityDetail value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize((int)value.camp, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.level, result, ref indexStart)) return false;
            if (!StringSerializer.Serialize(value.name, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.health, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.maxHealth, result, ref indexStart)) return false;

            int buffCount = value.buffs != null ? value.buffs.Count : 0;
            if (!ByteSerializer.Serialize((byte)buffCount, result, ref indexStart)) return false;
            for (int i = 0; i < buffCount; i++)
            {
                if (!ByteSerializer.Serialize(value.buffs[i].type, result, ref indexStart)) return false;
                if (!ByteSerializer.Serialize(value.buffs[i].level, result, ref indexStart)) return false;
            }

            int slotCount = value.slotSkills != null ? value.slotSkills.Count : 0;
            if (!ByteSerializer.Serialize((byte)slotCount, result, ref indexStart)) return false;
            for (int i = 0; i < slotCount; i++)
            {
                var slot = value.slotSkills[i];
                if (!UshortSerializer.Serialize(slot.skillId, result, ref indexStart)) return false;
                if (!ByteSerializer.Serialize(slot.recharge01, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(slot.store, result, ref indexStart)) return false;
            }

            int heldCount = value.heldSkills != null ? value.heldSkills.Count : 0;
            if (!ByteSerializer.Serialize((byte)heldCount, result, ref indexStart)) return false;
            for (int i = 0; i < heldCount; i++)
                if (!UshortSerializer.Serialize(value.heldSkills[i], result, ref indexStart)) return false;
            return true;
        }

        public static SCGodViewEntityDetail Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCGodViewEntityDetail()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                level = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                name = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
                health = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                maxHealth = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                slotSkills = new List<SCGodViewEntityDetail.SlotSkill>(),
                heldSkills = new List<ushort>(),
                buffs = new List<SCGodViewEntityDetail.Buff>(),
            };
            int buffCount = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < buffCount; i++)
            {
                info.buffs.Add(new SCGodViewEntityDetail.Buff()
                {
                    type = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    level = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            int slotCount = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < slotCount; i++)
            {
                info.slotSkills.Add(new SCGodViewEntityDetail.SlotSkill()
                {
                    skillId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    recharge01 = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    store = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            int heldCount = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < heldCount; i++)
                info.heldSkills.Add(UshortSerializer.Deserialize(data, ref indexStart, invalidIndex));
            return info;
        }
    }
}
