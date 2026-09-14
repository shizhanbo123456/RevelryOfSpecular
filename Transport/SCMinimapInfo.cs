using System.Collections.Generic;

namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：小地图可见单位（策划案第十五章）。
    /// 小地图 = 己方（团队）**共享视野**：内容按阵营在服务器算好后下发，同一阵营各成员收到同一份
    /// （己方单位恒显示；敌方 / 中立单位只要在本阵营任一成员视野内就显示）。
    /// 夜间进攻方处于「小地图失联」时改为按客户端下发，只含自己与自身视野内的单位（minimapLost = true）。
    /// 坐标只传世界 XZ，客户端按 Landscape.MapSize 归一化到小地图矩形（地图为俯视图，Z 向上）。
    /// </summary>
    public class SCMinimapInfo
    {
        /// <summary>本条消息包含的可见单位。</summary>
        public List<MinimapEntity> entities = new();

        /// <summary>是否处于「小地图失联」（夜间进攻方）：本条只含自己与自身视野，不含队友提供的视野。</summary>
        public bool minimapLost;

        /// <summary>小地图上的一个单位。</summary>
        public class MinimapEntity
        {
            /// <summary>实体 id（客户端据此定位本地玩家自己）。</summary>
            public ushort entityId;
            /// <summary>实体类型（UI 按类别取图标 / 尺寸）。</summary>
            public EntityType type;
            /// <summary>阵营（UI 着色：己方 / 敌方 / 中立）。</summary>
            public EntityCamp camp;
            /// <summary>世界坐标 X。</summary>
            public float posX;
            /// <summary>世界坐标 Z。</summary>
            public float posZ;
            /// <summary>是否被「白眼标记」（UI 高亮）。</summary>
            public bool marked;
        }
    }

    /// <summary>SCMinimapInfo 网络序列化器。</summary>
    public struct SCMinimapInfoSerializer
    {
        public static bool Serialize(SCMinimapInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!BoolSerializer.Serialize(value.minimapLost, result, ref indexStart)) return false;

            int count = value.entities?.Count ?? 0;
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
