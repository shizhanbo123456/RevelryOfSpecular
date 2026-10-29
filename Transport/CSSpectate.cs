using System;

namespace Ros.Transport
{
    // 客户端 → 服务器：观战请求。targetClientId = 目标玩家 id（须同阵营且存活）；
    // 传入非法 / 死亡 / 异阵营目标 = 退出观战
    public class CSSpectate
    {
        public int targetClientId;
    }

    public struct CSSpectateSerializer
    {
        public static bool Serialize(CSSpectate value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                return IntSerializer.Serialize(value.targetClientId, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 3)) return false;

                // 位级：1+16=17 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.targetClientId, -1, 32767);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static CSSpectate Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                return new CSSpectate()
                {
                    targetClientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 CSSpectate 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 3) throw new Exception("反序列化 CSSpectate 失败：剩余数据不足");

                var r = new CSSpectate();
                r.targetClientId = BitField.ReadInt(-1, 32767);
                indexStart += 3;
                return r;
            }
        }
    }
}
