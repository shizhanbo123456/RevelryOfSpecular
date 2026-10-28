using System;

namespace Ros.Transport
{
    // 服务器 → 客户端：小地图上单个单位（每个实体一个独立数据包，不拼装、不编号、无标志位）
    // 直接承载实体字段，不再嵌套 MinimapEntity，省去一层对象分配与 GC
    public class SCMinimapEntity
    {
        public ushort entityId;   // 实体 id（客户端据此定位本地玩家自己）
        public EntityType type;   // 实体类型（UI 按类别取图标 / 尺寸）
        public EntityCamp camp;   // 阵营（UI 着色：己方 / 敌方 / 中立）
        public float posX;        // 世界坐标 X
        public float posZ;        // 世界坐标 Z
        public bool marked;       // 是否被「白眼标记」（UI 高亮）
    }

    public struct SCMinimapEntitySerializer
    {
        public static bool Serialize(SCMinimapEntity value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 8)) return false;

            // 位级：1+16+3+8+5+15+15+1=64 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt(value.entityId, 0, 65535);
                BitField.WriteInt((int)value.type.category, 0, 7);
                BitField.WriteInt(value.type.value, 0, 255);
                BitField.WriteInt((int)value.camp, 0, 31);
                BitField.WriteFloat(value.posX, -500f, 500f, 0.05f);
                BitField.WriteFloat(value.posZ, -500f, 500f, 0.05f);
                BitField.WriteBool(value.marked);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCMinimapEntity Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCMinimapEntity 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 8) throw new Exception("反序列化 SCMinimapEntity 失败：剩余数据不足");

            var r = new SCMinimapEntity();
            r.entityId = (ushort)BitField.ReadInt(0, 65535);
            r.type = new EntityType((EntityCategory)BitField.ReadInt(0, 7), BitField.ReadInt(0, 255));
            r.camp = (EntityCamp)BitField.ReadInt(0, 31);
            r.posX = BitField.ReadFloat(-500f, 500f, 0.05f);
            r.posZ = BitField.ReadFloat(-500f, 500f, 0.05f);
            r.marked = BitField.ReadBool();
            indexStart += 8;
            return r;
        }

        private static bool SerializeBytes(SCMinimapEntity value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize((int)value.camp, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.posX, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.posZ, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.marked, result, ref indexStart)) return false;
            return true;
        }

        private static SCMinimapEntity DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCMinimapEntity()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posX = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posZ = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                marked = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
