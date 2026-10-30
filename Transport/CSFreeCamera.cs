using System;

namespace Ros.Transport
{
    // 客户端 → 服务器：观战自由视角相机位置（仅纯观战方上报）。free=false 表示已切回跟随实体，位置字段无效
    public class CSFreeCamera
    {
        public bool free;
        public float posX;
        public float posZ;
    }

    public struct CSFreeCameraSerializer
    {
        public static bool Serialize(CSFreeCamera value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!BoolSerializer.Serialize(value.free, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.posX, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.posZ, result, ref indexStart)) return false;
                return true;
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 4)) return false;

                // 位级：1+1+13+13=28 位（位置 ±4000m、0.5m 一格，相机可自由飞到地图外）
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteBool(value.free);
                    BitField.WriteFloat(value.posX, -4000f, 4000f, 0.5f);
                    BitField.WriteFloat(value.posZ, -4000f, 4000f, 0.5f);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static CSFreeCamera Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                return new CSFreeCamera()
                {
                    free = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    posX = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    posZ = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 CSFreeCamera 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 4) throw new Exception("反序列化 CSFreeCamera 失败：剩余数据不足");

                var r = new CSFreeCamera();
                r.free = BitField.ReadBool();
                r.posX = BitField.ReadFloat(-4000f, 4000f, 0.5f);
                r.posZ = BitField.ReadFloat(-4000f, 4000f, 0.5f);
                indexStart += 4;
                return r;
            }
        }
    }
}
