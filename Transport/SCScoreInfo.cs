using System;

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
        public float guardStatueHealth;
    }

    public struct SCScoreInfoSerializer
    {
        public static bool Serialize(SCScoreInfo value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!IntSerializer.Serialize(value.gameState, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.attackScore, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.defenseScore, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.killScore, result, ref indexStart)) return false;
                if (!FloatSerializer.Serialize(value.remainTime, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.expGain, result, ref indexStart)) return false;
                return FloatSerializer.Serialize(value.guardStatueHealth, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 15)) return false;

                // 位级：1+4+20+20+14+16+20+20=115 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.gameState, 0, 15);
                    BitField.WriteFloat(value.attackScore, 0f, 1000000f, 1f);
                    BitField.WriteFloat(value.defenseScore, 0f, 1000000f, 1f);
                    BitField.WriteInt(value.killScore, 0, 10000);
                    BitField.WriteFloat(value.remainTime, 0f, 3600f, 0.1f);
                    BitField.WriteInt(value.expGain, 0, 1000000);
                    BitField.WriteFloat(value.guardStatueHealth, 0f, 1000000f, 1f);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCScoreInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
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
                    guardStatueHealth = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCScoreInfo 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 15) throw new Exception("反序列化 SCScoreInfo 失败：剩余数据不足");

                var r = new SCScoreInfo();
                r.gameState = BitField.ReadInt(0, 15);
                r.attackScore = BitField.ReadFloat(0f, 1000000f, 1f);
                r.defenseScore = BitField.ReadFloat(0f, 1000000f, 1f);
                r.killScore = BitField.ReadInt(0, 10000);
                r.remainTime = BitField.ReadFloat(0f, 3600f, 0.1f);
                r.expGain = BitField.ReadInt(0, 1000000);
                r.guardStatueHealth = BitField.ReadFloat(0f, 1000000f, 1f);
                indexStart += 15;
                return r;
            }
        }
    }
}
