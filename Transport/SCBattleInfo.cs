using System.Collections.Generic;

namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：开局同步信息（进入世界后发送一次）。
    /// </summary>
    public class SCBattleInfo
    {
        /// <summary>本地玩家实体 id。</summary>
        public ushort playerEntityId;
        /// <summary>分配到的阵营。</summary>
        public EntityCamp camp;
        /// <summary>所选角色类型。</summary>
        public EntityType characterType;
        /// <summary>角色等级。</summary>
        public int characterLevel = 1;
        /// <summary>初始技能列表（进攻方=初始武器；防守方=角色主动技能/大招）。</summary>
        public List<int> skillIds = new();
        /// <summary>武器槽位数量（角色属性）。</summary>
        public int weaponSlotCount = 3;
        /// <summary>当前昼夜阶段（0白天 1黄昏 2夜晚 3黎明）。</summary>
        public int dayNightPhase;
        /// <summary>当前阶段已进行时间（秒）。</summary>
        public float phaseTime;
        /// <summary>守护点初始信息（含中心）。</summary>
        public List<SCBeaconInfo> beacons = new();
    }

    /// <summary>SCBattleInfo 网络序列化器。</summary>
    public struct SCBattleInfoSerializer
    {
        public static bool Serialize(SCBattleInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.playerEntityId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize((int)value.camp, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.characterType, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.characterLevel, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.weaponSlotCount, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.dayNightPhase, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.phaseTime, result, ref indexStart)) return false;

            int skillCount = value.skillIds?.Count ?? 0;
            if (!IntSerializer.Serialize(skillCount, result, ref indexStart)) return false;
            if (value.skillIds != null)
            {
                foreach (var id in value.skillIds)
                {
                    if (!IntSerializer.Serialize(id, result, ref indexStart)) return false;
                }
            }

            int beaconCount = value.beacons?.Count ?? 0;
            if (!IntSerializer.Serialize(beaconCount, result, ref indexStart)) return false;
            if (value.beacons != null)
            {
                foreach (var beacon in value.beacons)
                {
                    if (!SCBeaconInfoSerializer.Serialize(beacon, result, ref indexStart)) return false;
                }
            }
            return true;
        }

        public static SCBattleInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCBattleInfo()
            {
                playerEntityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                characterType = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                characterLevel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                weaponSlotCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                dayNightPhase = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                phaseTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            int skillCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < skillCount; i++)
            {
                info.skillIds.Add(IntSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            int beaconCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < beaconCount; i++)
            {
                info.beacons.Add(SCBeaconInfoSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            return info;
        }
    }
}
