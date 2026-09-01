namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：玩家进场选角信息。
    /// </summary>
    public class CSPlayerInfo
    {
        /// <summary>所选角色类型（进攻/防守角色池）。</summary>
        public EntityType type;
        /// <summary>角色等级（局外养成）。</summary>
        public int level = 1;
        /// <summary>阵营意向：0 进攻 / 1 防守 / 2 任意。</summary>
        public int campIntention = 2;
        /// <summary>玩家等级（账号级，用于解锁校验）。</summary>
        public int playerLevel = 1;
    }

    /// <summary>CSPlayerInfo 网络序列化器。</summary>
    public struct CSPlayerInfoSerializer
    {
        public static bool Serialize(CSPlayerInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!EntityTypeSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.level, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.campIntention, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.playerLevel, result, ref indexStart);
        }

        public static CSPlayerInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new CSPlayerInfo()
            {
                type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                level = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                campIntention = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                playerLevel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            return info;
        }
    }
}
