using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    // 服务器 → 客户端：上帝视角区域内的珊瑚（数量最多，单独成流，只带坐标）
    // 与实体流同样按块下发，同一 seq 拼齐后刷新
    public class SCGodViewCorals
    {
        public byte seq;
        public ushort chunkIndex;
        public ushort chunkCount;
        public List<Vector2> points = new(); // 世界 XZ
    }

    public struct SCGodViewCoralsSerializer
    {
        public static bool Serialize(SCGodViewCorals value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!ByteSerializer.Serialize(value.seq, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.chunkIndex, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.chunkCount, result, ref indexStart)) return false;

            int count = value.points != null ? value.points.Count : 0;
            if (!ByteSerializer.Serialize((byte)count, result, ref indexStart)) return false;
            if (!EnsInstance.enableBitTransform)
            {
                for (int i = 0; i < count; i++)
                    if (!Vector2Serializer.Serialize(value.points[i], result, ref indexStart)) return false;
                return true;
            }

            // 位级：11+11=22 位/条
            for (int i = 0; i < count; i++)
            {
                var point = value.points[i];
                if (result.Length - indexStart < 3) return false;
                BitField.SetBuffer(result, indexStart);
                BitField.WriteFloat(point.x, 0f, SyncRanges.PositionXZMax, SyncRanges.MinimapPositionStep);
                BitField.WriteFloat(point.y, 0f, SyncRanges.PositionXZMax, SyncRanges.MinimapPositionStep);
                indexStart += BitField.Flush();
            }
            return true;
        }

        public static SCGodViewCorals Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCGodViewCorals()
            {
                seq = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                chunkIndex = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                chunkCount = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                points = new List<Vector2>(),
            };
            int count = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (!EnsInstance.enableBitTransform)
            {
                for (int i = 0; i < count; i++)
                    info.points.Add(Vector2Serializer.Deserialize(data, ref indexStart, invalidIndex));
                return info;
            }

            for (int i = 0; i < count; i++)
            {
                if (invalidIndex - indexStart < 3) throw new System.Exception("反序列化 SCGodViewCorals 失败：剩余数据不足");
                BitField.SetBuffer(data, indexStart);
                float x = BitField.ReadFloat(0f, SyncRanges.PositionXZMax, SyncRanges.MinimapPositionStep);
                float z = BitField.ReadFloat(0f, SyncRanges.PositionXZMax, SyncRanges.MinimapPositionStep);
                info.points.Add(new Vector2(x, z));
                indexStart += BitField.ReadByteLength;
            }
            return info;
        }
    }
}
