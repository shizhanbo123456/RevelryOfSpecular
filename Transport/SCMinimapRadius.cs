using System;

namespace Ros.Transport
{
    // 服务器 → 客户端：回应切换后的雷达显示半径（客户端据此调整裁剪与缩放）
    public class SCMinimapRadius
    {
        public float radius;
    }

    public struct SCMinimapRadiusSerializer
    {
        public static bool Serialize(SCMinimapRadius value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 2)) return false;

            // 位级：1+10=11 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteFloat(value.radius, 0f, 1000f, 1f);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCMinimapRadius Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCMinimapRadius 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 2) throw new Exception("反序列化 SCMinimapRadius 失败：剩余数据不足");

            var r = new SCMinimapRadius();
            r.radius = BitField.ReadFloat(0f, 1000f, 1f);
            indexStart += 2;
            return r;
        }

        private static bool SerializeBytes(SCMinimapRadius value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return FloatSerializer.Serialize(value.radius, result, ref indexStart);
        }

        private static SCMinimapRadius DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCMinimapRadius()
            {
                radius = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
