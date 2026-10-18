using System.Collections.Generic;

namespace Ros.Transport
{
    // 服务器 → 客户端：进攻方玩家表（clientId + 预分配实体 id + 名字）
    public class SCLoadingAttackPlayerResponse
    {
        public List<LoadingUnitEntry> list = new();
    }

    public struct SCLoadingAttackPlayerResponseSerializer
    {
        public static bool Serialize(SCLoadingAttackPlayerResponse value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            int count = value.list != null ? value.list.Count : 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.list == null) return true;
            foreach (var entry in value.list)
            {
                if (!BoolSerializer.Serialize(entry != null, result, ref indexStart)) return false;
                if (entry == null) continue;
                if (!LoadingUnitEntrySerializer.Serialize(entry, result, ref indexStart)) return false;
            }
            return true;
        }

        public static SCLoadingAttackPlayerResponse Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCLoadingAttackPlayerResponse();
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                bool hasEntry = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (!hasEntry) { info.list.Add(null); continue; }
                info.list.Add(LoadingUnitEntrySerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            return info;
        }
    }
}
