using System;

using System.Collections.Generic;

namespace Ros.Transport
{
    public class SCLobbyInfo
    {
        public List<LobbyMemberInfo> members = new();
        public int attackAICount;
        public int defenseAICount;
        public bool battleStarted;

        public class LobbyMemberInfo
        {
            public short clientId;
            public int camp = -1;      // 0 进攻 / 1 防守 / 2 观战（-1 仅协议兼容，进大厅即默认观战）
            public int characterIndex = -1;
            public bool ready;         // 组队界面准备状态（全员准备后服务器倒计时开战）
        }
    }

    public struct SCLobbyInfoSerializer
    {
        public static bool Serialize(SCLobbyInfo value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);

            // 位级：AI数量4*2+battleStarted1+成员数4+成员(1+16+3+6+1)*n
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt(value.attackAICount, 0, 15);
                BitField.WriteInt(value.defenseAICount, 0, 15);
                BitField.WriteBool(value.battleStarted);

                int count = value.members != null ? value.members.Count : 0;
                BitField.WriteInt(count, 0, 15);
                if (value.members != null)
                {
                    foreach (var member in value.members)
                    {
                        BitField.WriteBool(member != null);
                        if (member == null) continue;
                        BitField.WriteInt(member.clientId, -1, 32767);
                        BitField.WriteInt(member.camp, -1, 2);
                        BitField.WriteInt(member.characterIndex, -1, 31);
                        BitField.WriteBool(member.ready);
                    }
                }
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCLobbyInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCLobbyInfo 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }

            var r = new SCLobbyInfo();
            r.attackAICount = BitField.ReadInt(0, 15);
            r.defenseAICount = BitField.ReadInt(0, 15);
            r.battleStarted = BitField.ReadBool();

            int count = BitField.ReadInt(0, 15);
            for (int i = 0; i < count; i++)
            {
                if (!BitField.ReadBool()) { r.members.Add(null); continue; }
                r.members.Add(new SCLobbyInfo.LobbyMemberInfo()
                {
                    clientId = (short)BitField.ReadInt(-1, 32767),
                    camp = BitField.ReadInt(-1, 2),
                    characterIndex = BitField.ReadInt(-1, 31),
                    ready = BitField.ReadBool(),
                });
            }
            indexStart += BitField.ReadByteLength;
            return r;
        }

        private static bool SerializeBytes(SCLobbyInfo value, byte[] result, ref int indexStart)
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
                    if (!BoolSerializer.Serialize(member.ready, result, ref indexStart)) return false;
                }
            }
            return true;
        }

        private static SCLobbyInfo DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCLobbyInfo()
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
                info.members.Add(new SCLobbyInfo.LobbyMemberInfo()
                {
                    clientId = (short)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    characterIndex = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    ready = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                });
            }
            return info;
        }
    }
}
