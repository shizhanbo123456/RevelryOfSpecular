using System;

namespace Ros.Transport
{
    // 服务器 → 客户端：开始战斗（已加载完成的客户端才收；未完成的观战方完成后补发），客户端据此隐藏加载界面并切页
    public class SCBattleStart
    {
    }

    public struct SCBattleStartSerializer
    {
        public static bool Serialize(SCBattleStart value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < 1) return false;

            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            indexStart += BitField.Flush();
            return true;
        }

        public static SCBattleStart Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCBattleStart 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            indexStart += 1;
            return new SCBattleStart();
        }

        // 字节级：无载荷，仅 null 标记
        private static bool SerializeBytes(SCBattleStart value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        private static SCBattleStart DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCBattleStart();
        }
    }
}
