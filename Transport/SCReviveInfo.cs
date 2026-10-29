using System;

namespace Ros.Transport
{
    public class SCReviveInfo
    {
        public ushort entityId;
        public float progress;
        public bool ready;
        public int furyStack;
    }

    public struct SCReviveInfoSerializer
    {
        public static bool Serialize(SCReviveInfo value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.progress, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(value.ready, result, ref indexStart)) return false;
                return IntSerializer.Serialize(value.furyStack, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 5)) return false;

                // 位级：1+16+10+1+7=35 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.entityId, 0, 65535);
                    BitField.WriteFloat(value.progress, 0f, 1f, 0.001f);
                    BitField.WriteBool(value.ready);
                    BitField.WriteInt(value.furyStack, 0, 99);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCReviveInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                return new SCReviveInfo()
                {
                    entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    progress = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    ready = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    furyStack = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCReviveInfo 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 5) throw new Exception("反序列化 SCReviveInfo 失败：剩余数据不足");

                var r = new SCReviveInfo();
                r.entityId = (ushort)BitField.ReadInt(0, 65535);
                r.progress = BitField.ReadFloat(0f, 1f, 0.001f);
                r.ready = BitField.ReadBool();
                r.furyStack = BitField.ReadInt(0, 99);
                indexStart += 5;
                return r;
            }
        }
    }
}
