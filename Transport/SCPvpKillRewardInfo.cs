
namespace Ros.Transport
{
    public class SCPvpKillRewardInfo
    {
        public EntityType victimType;
        public System.Collections.Generic.Dictionary<int, int> runes = new();
    }

    public struct SCPvpKillRewardInfoSerializer
    {
        public static bool Serialize(SCPvpKillRewardInfo value, byte[] result, ref int indexStart)
        {
            bool hasValue = value != null;
            if (!BoolSerializer.Serialize(hasValue, result, ref indexStart)) return false;
            if (!hasValue) return true;

            if (!EntityTypeSerializer.Serialize(value.victimType, result, ref indexStart)) return false;
            int count = value.runes == null ? 0 : value.runes.Count;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.runes == null) return true;

            foreach (var rune in value.runes)
            {
                if (!IntSerializer.Serialize(rune.Key, result, ref indexStart) ||
                    !IntSerializer.Serialize(rune.Value, result, ref indexStart))
                {
                    return false;
                }
            }
            return true;
        }

        public static SCPvpKillRewardInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            bool hasValue = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (!hasValue) return null;

            SCPvpKillRewardInfo value = new()
            {
                victimType = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                runes = new System.Collections.Generic.Dictionary<int, int>()
            };

            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                int skillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                int runeCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                value.runes[skillId] = runeCount;
            }
            return value;
        }
    }
}
