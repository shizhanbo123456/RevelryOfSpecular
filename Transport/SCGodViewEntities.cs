using System.Collections.Generic;

namespace Ros.Transport
{
    // 服务器 → 客户端：上帝视角区域内的实体（不含珊瑚，珊瑚走 SCGodViewCorals）
    // 单包上限 256B，故按块下发：同一 seq 的若干块在客户端拼齐后才刷新界面
    public class SCGodViewEntities
    {
        public byte seq;
        public ushort chunkIndex;
        public ushort chunkCount;
        public List<SCMinimapEntity> entries = new();
    }

    public struct SCGodViewEntitiesSerializer
    {
        public static bool Serialize(SCGodViewEntities value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!ByteSerializer.Serialize(value.seq, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.chunkIndex, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.chunkCount, result, ref indexStart)) return false;

            int count = value.entries != null ? value.entries.Count : 0;
            if (!ByteSerializer.Serialize((byte)count, result, ref indexStart)) return false;
            for (int i = 0; i < count; i++)
                if (!SCMinimapEntitySerializer.Serialize(value.entries[i], result, ref indexStart)) return false;
            return true;
        }

        public static SCGodViewEntities Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCGodViewEntities()
            {
                seq = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                chunkIndex = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                chunkCount = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                entries = new List<SCMinimapEntity>(),
            };
            int count = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                var entry = SCMinimapEntitySerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (entry == null) continue;
                info.entries.Add(entry);
            }
            return info;
        }
    }
}
