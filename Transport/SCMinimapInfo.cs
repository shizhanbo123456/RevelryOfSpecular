using System.Collections.Generic;

namespace Ros.Transport
{
    // 服务器 → 客户端：小地图可见单位
    public class SCMinimapInfo
    {
        public List<MinimapEntity> entities = new();

        public bool minimapLost;

        // 小地图上的一个单位
        public class MinimapEntity
        {
            //实体 id（客户端据此定位本地玩家自己）
            public ushort entityId;
            //实体类型（UI 按类别取图标 / 尺寸）
            public EntityType type;
            //阵营（UI 着色：己方 / 敌方 / 中立）
            public EntityCamp camp;
            //世界坐标 X
            public float posX;
            //世界坐标 Z
            public float posZ;
            //是否被「白眼标记」（UI 高亮）
            public bool marked;
        }
    }

    public struct SCMinimapInfoSerializer
    {
        public static bool Serialize(SCMinimapInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!BoolSerializer.Serialize(value.minimapLost, result, ref indexStart)) return false;

            int count = value.entities != null ? value.entities.Count : 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.entities == null) return true;
            foreach (var entity in value.entities)
            {
                if (!BoolSerializer.Serialize(entity != null, result, ref indexStart)) return false;
                if (entity == null) continue;
                if (!UshortSerializer.Serialize(entity.entityId, result, ref indexStart)) return false;
                if (!EntityTypeSerializer.Serialize(entity.type, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize((int)entity.camp, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(entity.posX, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(entity.posZ, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(entity.marked, result, ref indexStart)) return false;
            }
            return true;
        }

        public static SCMinimapInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCMinimapInfo()
            {
                minimapLost = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex))
                {
                    info.entities.Add(null);
                    continue;
                }
                info.entities.Add(new SCMinimapInfo.MinimapEntity()
                {
                    entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    posX = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    posZ = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    marked = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            return info;
        }
    }
}
