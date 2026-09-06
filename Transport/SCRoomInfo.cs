using System.Collections.Generic;

namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：组队大厅房间状态（成员广播，任何变化时全房间同步）。
    /// 客户端据此显示双方队伍人数与 AI 数量，判断是否满足开局条件（双方人数均 &gt; 0）。
    /// </summary>
    public class SCRoomInfo
    {
        /// <summary>房间成员。</summary>
        public List<RoomMemberInfo> members = new();
        /// <summary>进攻方 AI 玩家数量。</summary>
        public int attackAICount;
        /// <summary>防守方 AI 玩家数量。</summary>
        public int defenseAICount;
        /// <summary>对局是否已开始（开始后大厅只读）。</summary>
        public bool battleStarted;

        /// <summary>单个房间成员。</summary>
        public class RoomMemberInfo
        {
            /// <summary>客户端 id。</summary>
            public short clientId;
            /// <summary>所在队伍（0 进攻 / 1 防守 / -1 未选择）。</summary>
            public int camp = -1;
        }
    }

    /// <summary>SCRoomInfo 网络序列化器。</summary>
    public struct SCRoomInfoSerializer
    {
        public static bool Serialize(SCRoomInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.attackAICount, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.defenseAICount, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.battleStarted, result, ref indexStart)) return false;

            int count = value.members?.Count ?? 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.members != null)
            {
                foreach (var member in value.members)
                {
                    if (!BoolSerializer.Serialize(member != null, result, ref indexStart)) return false;
                    if (member == null) continue;
                    if (!IntSerializer.Serialize(member.clientId, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(member.camp, result, ref indexStart)) return false;
                }
            }
            return true;
        }

        public static SCRoomInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCRoomInfo()
            {
                attackAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                defenseAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                battleStarted = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                bool hasMember = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (!hasMember) { info.members.Add(null); continue; }
                info.members.Add(new SCRoomInfo.RoomMemberInfo()
                {
                    clientId = (short)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            return info;
        }
    }
}
