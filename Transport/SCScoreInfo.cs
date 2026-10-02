namespace Ros.Transport
{
    public class SCScoreInfo
    {
        public int gameState;
        public float attackScore;
        public float defenseScore;
        public int killScore;
        public float remainTime;
        public int expGain;
        public float beaconHealth;
    }

    public struct SCScoreInfoSerializer
    {
        public static bool Serialize(SCScoreInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.gameState, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.attackScore, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.defenseScore, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.killScore, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.remainTime, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.expGain, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.beaconHealth, result, ref indexStart);
        }

        public static SCScoreInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCScoreInfo()
            {
                gameState = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackScore = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseScore = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                killScore = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                remainTime = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                expGain = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                beaconHealth = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
