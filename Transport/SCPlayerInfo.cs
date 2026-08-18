namespace Ros.Transport
{
    public struct SCPlayerInfo
    {
        public ushort playerEntityId;
        public int spawnPosId;
    }

    public struct SCPlayerInfoSerializer
    {
        public static bool Serialize(SCPlayerInfo value, byte[] result, ref int indexStart)
        {
            return UshortSerializer.Serialize(value.playerEntityId, result, ref indexStart) &&
                   IntSerializer.Serialize(value.spawnPosId, result, ref indexStart);
        }

        public static SCPlayerInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new SCPlayerInfo
            {
                playerEntityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                spawnPosId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex)
            };
        }
    }
}
