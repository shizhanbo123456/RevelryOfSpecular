namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：守护点（瘟疫信标）血量信息。
    /// 防守方 HUD 常驻显示，血量变化即偷拆预警。
    /// </summary>
    public class SCBeaconInfo
    {
        /// <summary>守护点实体 id。</summary>
        public ushort entityId;
        /// <summary>守护点类型（Beacon(0~2) 外围，Beacon(3) 中心）。</summary>
        public EntityType type;
        /// <summary>当前生命。</summary>
        public int health;
        /// <summary>最大生命。</summary>
        public int maxHealth;
        /// <summary>减伤护盾叠层（中心守护点：每存活外围 +1 层，0~3；外围恒 0）。</summary>
        public int shieldLayer;
        /// <summary>是否被摧毁。</summary>
        public bool destroyed;
    }

    /// <summary>SCBeaconInfo 网络序列化器。</summary>
    public struct SCBeaconInfoSerializer
    {
        public static bool Serialize(SCBeaconInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.health, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.maxHealth, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.shieldLayer, result, ref indexStart)) return false;
            return BoolSerializer.Serialize(value.destroyed, result, ref indexStart);
        }

        public static SCBeaconInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCBeaconInfo()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                health = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                maxHealth = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                shieldLayer = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                destroyed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
