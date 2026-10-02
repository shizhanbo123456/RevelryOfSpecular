namespace Ros.Transport
{
    public class CSPlayerInfo
    {
        public EntityType attackCharacter;
        public int attackLevel = 1;
        public EntityType defenseCharacter;
        public int defenseLevel = 1;
        public string name = "";
    }

    public struct CSPlayerInfoSerializer
    {
        public static bool Serialize(CSPlayerInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!EntityTypeSerializer.Serialize(value.attackCharacter, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.attackLevel, result, ref indexStart)) return false;
            if (!EntityTypeSerializer.Serialize(value.defenseCharacter, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.defenseLevel, result, ref indexStart)) return false;
            return StringSerializer.Serialize(value.name, result, ref indexStart);
        }

        public static CSPlayerInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new CSPlayerInfo()
            {
                attackCharacter = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackLevel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseCharacter = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseLevel = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                name = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            return info;
        }
    }
}
