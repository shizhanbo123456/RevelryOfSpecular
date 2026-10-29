using System;

namespace Ros.Transport
{
    // 服务器 → 客户端：飘字提示（messageId = notice.csv 消息 id，客户端查表显示，颜色由 FGUI 预设）
    public class SCPrompt
    {
        public int messageId;
    }

    public struct SCPromptSerializer
    {
        public static bool Serialize(SCPrompt value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                return IntSerializer.Serialize(value.messageId, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 3)) return false;

                // 位级：1+16=17 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.messageId, 0, 65535);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCPrompt Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                return new SCPrompt()
                {
                    messageId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCPrompt 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 3) throw new Exception("反序列化 SCPrompt 失败：剩余数据不足");

                var r = new SCPrompt();
                r.messageId = BitField.ReadInt(0, 65535);
                indexStart += 3;
                return r;
            }
        }
    }
}
