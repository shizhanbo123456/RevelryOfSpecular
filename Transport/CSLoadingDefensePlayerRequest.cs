using System;

namespace Ros.Transport
{
    // 客户端 → 服务器：请求防守方玩家表（加载期第 2 步）
    public class CSLoadingDefensePlayerRequest
    {
    }

    public struct CSLoadingDefensePlayerRequestSerializer
    {
        public static bool Serialize(CSLoadingDefensePlayerRequest value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < 1) return false;

            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            indexStart += BitField.Flush();
            return true;
        }

        public static CSLoadingDefensePlayerRequest Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 CSLoadingDefensePlayerRequest 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            indexStart += 1;
            return new CSLoadingDefensePlayerRequest();
        }

        // 字节级：无载荷，仅 null 标记
        private static bool SerializeBytes(CSLoadingDefensePlayerRequest value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        private static CSLoadingDefensePlayerRequest DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSLoadingDefensePlayerRequest();
        }
    }
}
