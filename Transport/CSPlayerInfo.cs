using System.Collections.Generic;

namespace Ros.Transport
{
    public class CSPlayerInfo
    {
        public EntityType type;
        public int level;

        public Dictionary<int, int> imprints = new Dictionary<int, int>();//length<=3,key/value=[0,255]
        public List<int> collectionLevels = new List<int>();//length=Config.note_count,element=[0,255]
        public Dictionary<int, int> skills = new Dictionary<int, int>();//key=skillId,value=count

        public CSPlayerInfo()
        {
            for (int i = 0; i < Config.note_count; i++)
            {
                collectionLevels.Add(0);
            }
        }
    }

    public struct CSPlayerInfoSerializer
    {
        public static bool Serialize(CSPlayerInfo value, byte[] result, ref int indexStart)
        {
            bool hasValue = value != null;
            if (!BoolSerializer.Serialize(hasValue, result, ref indexStart)) return false;
            if (!hasValue) return true;

            if (!EntityTypeSerializer.Serialize(value.type, result, ref indexStart) ||
                !IntSerializer.Serialize(value.level, result, ref indexStart))
            {
                return false;
            }

            int imprintCount = value.imprints == null ? 0 : value.imprints.Count;
            if (!IntSerializer.Serialize(imprintCount, result, ref indexStart)) return false;
            if (value.imprints != null)
            {
                foreach (var pair in value.imprints)
                {
                    if (!IntSerializer.Serialize(pair.Key, result, ref indexStart) ||
                        !IntSerializer.Serialize(pair.Value, result, ref indexStart))
                    {
                        return false;
                    }
                }
            }

            int collectionCount = value.collectionLevels == null ? 0 : value.collectionLevels.Count;
            if (!IntSerializer.Serialize(collectionCount, result, ref indexStart)) return false;
            if (value.collectionLevels != null)
            {
                foreach (var level in value.collectionLevels)
                {
                    if (!IntSerializer.Serialize(level, result, ref indexStart)) return false;
                }
            }

            int skillCount = value.skills == null ? 0 : value.skills.Count;
            if (!IntSerializer.Serialize(skillCount, result, ref indexStart)) return false;
            if (value.skills == null) return true;

            foreach (var skill in value.skills)
            {
                if (!IntSerializer.Serialize(skill.Key, result, ref indexStart) ||
                    !IntSerializer.Serialize(skill.Value, result, ref indexStart))
                {
                    return false;
                }
            }
            return true;
        }

        public static CSPlayerInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            bool hasValue = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (!hasValue) return null;

            CSPlayerInfo value = new()
            {
                type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                level = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                imprints = new Dictionary<int, int>(),
                collectionLevels = new List<int>(),
                skills = new Dictionary<int, int>()
            };

            int imprintCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < imprintCount; i++)
            {
                int key = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                int imprintLevel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                value.imprints[key] = imprintLevel;
            }

            int collectionCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < collectionCount; i++)
            {
                value.collectionLevels.Add(IntSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }

            int skillCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < skillCount; i++)
            {
                int skillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
                value.skills[skillId] = count;
            }
            return value;
        }
    }
}
