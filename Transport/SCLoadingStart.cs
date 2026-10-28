using System;

namespace Ros.Transport
{
    // 服务器 → 客户端：开始加载（只用于切到加载界面；实体 id 等预处理结果走 SCLoadingPrepared）
    public class SCLoadingStart
    {
        public EntityCamp camp; // Attack / Defense / None（None = 观战方）
    }

    public struct SCLoadingStartSerializer
    {
        public static bool Serialize(SCLoadingStart value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 1)) return false;

            // 位级：1+5=6 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt((int)value.camp, 0, 31);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCLoadingStart Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCLoadingStart 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCLoadingStart 失败：剩余数据不足");

            var r = new SCLoadingStart();
            r.camp = (EntityCamp)BitField.ReadInt(0, 31);
            indexStart += 1;
            return r;
        }

        private static bool SerializeBytes(SCLoadingStart value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return IntSerializer.Serialize((int)value.camp, result, ref indexStart);
        }

        private static SCLoadingStart DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCLoadingStart()
            {
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
