using System;

namespace Ros.Transport
{
    // 玩家 → 实体 id（每玩家一条独立包），-1 = 无实体（死亡 / 观战 / 退场）；开局全量，此后仅变更时下发
    public class SCPlayerEntity
    {
        public short clientId;
        public int entityId;
    }

    public struct SCPlayerEntitySerializer
    {
        public static bool Serialize(SCPlayerEntity value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 4)) return false;

            // 位级：1+16+15=32 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt(value.clientId, -1, 32767);
                BitField.WriteInt(value.entityId, 0, 32767);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCPlayerEntity Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCPlayerEntity 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 4) throw new Exception("反序列化 SCPlayerEntity 失败：剩余数据不足");

            var r = new SCPlayerEntity();
            r.clientId = (short)BitField.ReadInt(-1, 32767);
            r.entityId = BitField.ReadInt(0, 32767);
            indexStart += 4;
            return r;
        }

        private static bool SerializeBytes(SCPlayerEntity value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.clientId, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.entityId, result, ref indexStart);
        }

        private static SCPlayerEntity DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCPlayerEntity()
            {
                clientId = (short)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                entityId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
