namespace Ros.Transport
{
    public class SCBattleInfo
    {
        public ushort playerEntityId;
        public EntityCamp camp;
        public EntityType characterType;
    }

    public struct SCBattleInfoSerializer
    {
        public static bool Serialize(SCBattleInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.playerEntityId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize((int)value.camp, result, ref indexStart)) return false;
            return EntityTypeSerializer.Serialize(value.characterType, result, ref indexStart);
        }

        public static SCBattleInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCBattleInfo()
            {
                playerEntityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                characterType = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            return info;
        }
    }
}
