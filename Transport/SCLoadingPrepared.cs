using System;

namespace Ros.Transport
{
    // 服务器 → 客户端：加载预处理完成（实体 id 已预分配），客户端收到后才开始逐步请求数据
    public class SCLoadingPrepared
    {
        public ushort ownEntityId;     // 预分配的本体实体 id；观战方为 0
        public EntityType characterType; // 本地玩家本局使用的角色（结算算角色经验要用）
        public float serverTime;       // 服务器时间基准，客户端据此校准本地时钟
    }

    public struct SCLoadingPreparedSerializer
    {
        public static bool Serialize(SCLoadingPrepared value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 7)) return false;

            // 位级：1+16+3+8+24=52 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt(value.ownEntityId, 0, 65535);
                BitField.WriteInt((int)value.characterType.category, 0, 7);
                BitField.WriteInt(value.characterType.value, 0, 255);
                BitField.WriteFloat(value.serverTime, 0f, 86400f, 0.01f);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCLoadingPrepared Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCLoadingPrepared 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 7) throw new Exception("反序列化 SCLoadingPrepared 失败：剩余数据不足");

            var r = new SCLoadingPrepared();
            r.ownEntityId = (ushort)BitField.ReadInt(0, 65535);
            r.characterType = new EntityType((EntityCategory)BitField.ReadInt(0, 7), BitField.ReadInt(0, 255));
            r.serverTime = BitField.ReadFloat(0f, 86400f, 0.01f);
            indexStart += 7;
            return r;
        }

        private static bool SerializeBytes(SCLoadingPrepared value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.ownEntityId, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.characterType, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.serverTime, result, ref indexStart);
        }

        private static SCLoadingPrepared DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCLoadingPrepared()
            {
                ownEntityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                characterType = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                serverTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
