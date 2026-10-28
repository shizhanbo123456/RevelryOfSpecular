using System;

namespace Ros.Transport
{
    // 服务器 → 客户端：四个守护点的开采进度（0~100，被打掉的血量百分比），与实体可见性无关，值变更时下发
    public class SCDefensePointProgress
    {
        public int main;
        public int sub1;
        public int sub2;
        public int sub3;
    }

    public struct SCDefensePointProgressSerializer
    {
        public static bool Serialize(SCDefensePointProgress value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 4)) return false;

            // 位级：1+4*7=29 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt(value.main, 0, 100);
                BitField.WriteInt(value.sub1, 0, 100);
                BitField.WriteInt(value.sub2, 0, 100);
                BitField.WriteInt(value.sub3, 0, 100);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCDefensePointProgress Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCDefensePointProgress 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 4) throw new Exception("反序列化 SCDefensePointProgress 失败：剩余数据不足");

            var r = new SCDefensePointProgress();
            r.main = BitField.ReadInt(0, 100);
            r.sub1 = BitField.ReadInt(0, 100);
            r.sub2 = BitField.ReadInt(0, 100);
            r.sub3 = BitField.ReadInt(0, 100);
            indexStart += 4;
            return r;
        }

        private static bool SerializeBytes(SCDefensePointProgress value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.main, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.sub1, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.sub2, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.sub3, result, ref indexStart);
        }

        private static SCDefensePointProgress DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCDefensePointProgress()
            {
                main = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sub1 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sub2 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sub3 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
