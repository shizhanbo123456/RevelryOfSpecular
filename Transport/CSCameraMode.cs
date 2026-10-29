using System;

namespace Ros.Transport
{
    // 客户端 → 服务器：同步本地相机是否处于俯视模式（俯视下移动语义为四向，服务器须按同一口径解释 WASD）
    public class CSCameraMode
    {
        public bool topDown;
    }

    public struct CSCameraModeSerializer
    {
        public static bool Serialize(CSCameraMode value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                return BoolSerializer.Serialize(value.topDown, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < 1) return false;

                // 位级：null 标记 + 模式标志，共 2 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteBool(value.topDown);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static CSCameraMode Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                bool hasValue = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (!hasValue) return null;
                return new CSCameraMode()
                {
                    topDown = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 CSCameraMode 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }

                var r = new CSCameraMode();
                r.topDown = BitField.ReadBool();
                indexStart += 1;
                return r;
            }
        }
    }
}
