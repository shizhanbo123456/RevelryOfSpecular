namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：组队大厅状态更新。
    /// camp = 发送者自己选择的队伍（0 进攻 / 1 防守 / -1 未选择）；
    /// AI 数量为房间共享值，房间中任意玩家可自由编辑双方数量（策划案 3.2，数量不限制）。
    /// 任何一项变化时整体发送一次，服务器以其为最新状态。
    /// </summary>
    public class CSRoomUpdate
    {
        /// <summary>发送者选择的队伍（0 进攻 / 1 防守 / -1 未选择）。</summary>
        public int camp = -1;
        /// <summary>进攻方 AI 玩家数量。</summary>
        public int attackAICount;
        /// <summary>防守方 AI 玩家数量。</summary>
        public int defenseAICount;
    }

    /// <summary>CSRoomUpdate 网络序列化器。</summary>
    public struct CSRoomUpdateSerializer
    {
        public static bool Serialize(CSRoomUpdate value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.camp, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.attackAICount, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.defenseAICount, result, ref indexStart);
        }

        public static CSRoomUpdate Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSRoomUpdate()
            {
                camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                attackAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
