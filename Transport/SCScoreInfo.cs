namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：对局分数信息（分数制胜负，见策划案 14 章）。
    /// 进攻方分数 = 守护点拆除量；防守方分数 = 守护点剩余血量 + 击杀小分。
    /// </summary>
    public class SCScoreInfo
    {
        /// <summary>对局状态：0 进行中 / 1 进攻方胜 / 2 防守方胜 / 3 平局。</summary>
        public int gameState;
        /// <summary>进攻方分数（守护点拆除量）。</summary>
        public float attackScore;
        /// <summary>防守方分数（守护点剩余血量+击杀小分）。</summary>
        public float defenseScore;
        /// <summary>击杀小分（防守方）。</summary>
        public int killScore;
        /// <summary>剩余时间（秒）。</summary>
        public float remainTime;
        /// <summary>中心守护点是否被摧毁。</summary>
        public bool coreDestroyed;
        /// <summary>本局获得经验（= 对水晶造成的伤害量，策划案 17.3；客户端结算写入存档）。</summary>
        public int expGain;
    }

    /// <summary>SCScoreInfo 网络序列化器。</summary>
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
            if (!BoolSerializer.Serialize(value.coreDestroyed, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.expGain, result, ref indexStart);
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
                coreDestroyed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                expGain = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
