using System.Collections.Generic;

namespace Ros.Transport
{
    public class SCRoomInfo
    {
        public List<RoomMemberInfo> members = new();
        public int attackAICount;
        public int defenseAICount;
        public bool battleStarted;

        public class RoomMemberInfo
        {
            public short clientId;
            public int camp = -1;
            public int characterIndex = -1;
            public string name = "";
        }
    }

    public struct SCRoomInfoSerializer
    {
        public static bool Serialize(SCRoomInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.attackAICount, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.defenseAICount, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.battleStarted, result, ref indexStart)) return false;

            int count = value.members != null ? value.members.Count : 0;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.members != null)
            {
                foreach (var member in value.members)
                {
                    if (!BoolSerializer.Serialize(member != null, result, ref indexStart)) return false;
                    if (member == null) continue;
                    if (!IntSerializer.Serialize(member.clientId, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(member.camp, result, ref indexStart)) return false;
                    if (!IntSerializer.Serialize(member.characterIndex, result, ref indexStart)) return false;
                    if (!StringSerializer.Serialize(member.name, result, ref indexStart)) return false;
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
                    characterIndex = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    name = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            return info;
        }
    }
}
