namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：开局同步信息（进入世界后发送一次）。
    /// 不传槽位数/技能列表/守护点：槽位与技能随 SCEntityDisplayInfo 同步（客户端可经 InfoManager
    /// 按阵营+角色读取属性配置）；守护点与其它实体一样走统一的实体表现同步。
    /// 昼夜状态不在此处：统一由 SCDayNightInfo 快照下发（战斗开始时必发一次）。
    /// </summary>
    public class SCBattleInfo
    {
        /// <summary>本地玩家实体 id。</summary>
        public ushort playerEntityId;
        /// <summary>分配到的阵营（房间内确定）。</summary>
        public EntityCamp camp;
        /// <summary>所选角色类型（按阵营取 CSPlayerInfo 对应一侧）。</summary>
        public EntityType characterType;
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
            return EntityTypeSerializer.Serialize(value.characterType, result, ref indexStart);
        }

        public static SCBattleInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCBattleInfo()
            {
                playerEntityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                characterType = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            return info;
        }
    }
}
