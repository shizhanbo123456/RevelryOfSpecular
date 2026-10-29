using System;

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
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!IntSerializer.Serialize(value.attackerTotal, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.defenseTotal, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.attackerAlive, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.defenseAlive, result, ref indexStart)) return false;
                return IntSerializer.Serialize(value.spectatorCount, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 5)) return false;

                // 位级：1+5*7=36 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.attackerTotal, 0, 64);
                    BitField.WriteInt(value.defenseTotal, 0, 64);
                    BitField.WriteInt(value.attackerAlive, 0, 64);
                    BitField.WriteInt(value.defenseAlive, 0, 64);
                    BitField.WriteInt(value.spectatorCount, 0, 64);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCCampAliveCount Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
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
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCCampAliveCount 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 5) throw new Exception("反序列化 SCCampAliveCount 失败：剩余数据不足");

                var r = new SCCampAliveCount();
                r.attackerTotal = BitField.ReadInt(0, 64);
                r.defenseTotal = BitField.ReadInt(0, 64);
                r.attackerAlive = BitField.ReadInt(0, 64);
                r.defenseAlive = BitField.ReadInt(0, 64);
                r.spectatorCount = BitField.ReadInt(0, 64);
                indexStart += 5;
                return r;
            }
        }
    }
}
