namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：玩家进场选角信息。
    /// 组队阶段双方角色各自选择并随时可调整（策划案 3.1/3.2）；
    /// 阵营完全在房间内确定，服务器按最终阵营取对应一侧的角色与等级；
    /// 角色随玩家等级自动解锁，不校验、不上传账号等级。
    /// </summary>
    public class CSPlayerInfo
    {
        /// <summary>进攻方所选角色（EntityType.Attack 下标）。</summary>
        public EntityType attackCharacter;
        /// <summary>进攻方所选角色等级（局外养成）。</summary>
        public int attackLevel = 1;
        /// <summary>防守方所选角色（EntityType.Defense 下标）。</summary>
        public EntityType defenseCharacter;
        /// <summary>防守方所选角色等级（局外养成）。</summary>
        public int defenseLevel = 1;
    }

    /// <summary>CSPlayerInfo 网络序列化器。</summary>
    public struct CSPlayerInfoSerializer
    {
        public static bool Serialize(CSPlayerInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!EntityTypeSerializer.Serialize(value.attackCharacter, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.attackLevel, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.defenseCharacter, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.defenseLevel, result, ref indexStart);
        }

        public static CSPlayerInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new CSPlayerInfo()
            {
                attackCharacter = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackLevel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseCharacter = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseLevel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            return info;
        }
    }
}
