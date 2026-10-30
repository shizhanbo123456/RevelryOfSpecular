namespace Ros.Transport
{
    // 服务器 → 客户端：双方阵营玩家（真人+AI）总人数、存活人数与观战人数，值变更时下发
    public class SCCampAliveCount
    {
        public int attackerTotal;
        public int defenseTotal;
        public int attackerAlive;
        public int defenseAlive;
        public int spectatorCount;
    }

    public struct SCCampAliveCountSerializer
    {
        public static bool Serialize(SCCampAliveCount value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.attackerTotal, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.defenseTotal, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.attackerAlive, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.defenseAlive, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.spectatorCount, result, ref indexStart);
        }

        public static SCCampAliveCount Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCCampAliveCount()
            {
                attackerTotal = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseTotal = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackerAlive = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseAlive = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                spectatorCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
