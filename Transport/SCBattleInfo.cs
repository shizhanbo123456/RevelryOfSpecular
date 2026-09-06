namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：开局同步信息（进入世界后发送一次）。
    /// 不传槽位数/技能列表/守护点：槽位与技能随 SCEntityDisplayInfo 同步（客户端可经 InfoManager
    /// 按阵营+角色读取属性配置）；守护点与其它实体一样走统一的实体表现同步。
    /// </summary>
    public class SCBattleInfo
    {
        /// <summary>本地玩家实体 id。</summary>
        public ushort playerEntityId;
        /// <summary>分配到的阵营（房间内确定）。</summary>
        public EntityCamp camp;
        /// <summary>所选角色类型（按阵营取 CSPlayerInfo 对应一侧）。</summary>
        public EntityType characterType;
        /// <summary>角色等级。</summary>
        public int characterLevel = 1;
        /// <summary>当前昼夜阶段（0白天 1黄昏 2夜晚 3黎明）。</summary>
        public int dayNightPhase;
        /// <summary>当前阶段已进行时间（秒）。</summary>
        public float phaseTime;
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
            if (!IntSerializer.Serialize(value.dayNightPhase, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.phaseTime, result, ref indexStart);
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
                dayNightPhase = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                phaseTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            return info;
        }
    }
}
