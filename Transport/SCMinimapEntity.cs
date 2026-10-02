namespace Ros.Transport
{
    // 服务器 → 客户端：小地图上单个单位（每个实体一个独立数据包，不拼装、不编号、无标志位）
    public class SCMinimapEntity
    {
        // 单个单位信息；分帧累积，客户端对长时间未更新的点位做超时隐藏
        public MinimapEntity entity;

        public class MinimapEntity
        {
            public ushort entityId;   // 实体 id（客户端据此定位本地玩家自己）
            public EntityType type;   // 实体类型（UI 按类别取图标 / 尺寸）
            public EntityCamp camp;   // 阵营（UI 着色：己方 / 敌方 / 中立）
            public float posX;        // 世界坐标 X
            public float posZ;        // 世界坐标 Z
            public bool marked;       // 是否被「白眼标记」（UI 高亮）
        }
    }

    public struct SCMinimapEntitySerializer
    {
        public static bool Serialize(SCMinimapEntity value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!BoolSerializer.Serialize(value.entity != null, result, ref indexStart)) return false;
            if (value.entity == null) return true;
            var e = value.entity;
            if (!UshortSerializer.Serialize(e.entityId, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(e.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize((int)e.camp, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(e.posX, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(e.posZ, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(e.marked, result, ref indexStart)) return false;
            return true;
        }

        public static SCMinimapEntity Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var v = new SCMinimapEntity();
            if (BoolSerializer.Deserialize(data, ref indexStart, invalidIndex))
            {
                v.entity = new SCMinimapEntity.MinimapEntity()
                {
                    entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    posX = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    posZ = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    marked = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            return v;
        }
    }
}
