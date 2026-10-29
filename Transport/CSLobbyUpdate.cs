using System;

namespace Ros.Transport
{
    public class CSLobbyUpdate
    {
        public int camp = -1;      // 0 进攻 / 1 防守 / 2 观战（-1 = 不变更）
        public bool ready;         // 准备状态
        public int attackAICount;
        public int defenseAICount;
    }

    public struct CSLobbyUpdateSerializer
    {
        // 位级格式：null标记1位 + camp3位(-1~3) + ready1位 + 两个AI数量各4位(0~15)，共13位，补齐占2字节
        private const int CampMin = -1;
        private const int CampMax = 3;
        private const int AiCountMin = 0;
        private const int AiCountMax = 15;
        private const int NullBytes = 1;
        private const int ValueBytes = 2;

        public static bool Serialize(CSLobbyUpdate value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!IntSerializer.Serialize(value.camp, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(value.ready, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.attackAICount, result, ref indexStart)) return false;
                return IntSerializer.Serialize(value.defenseAICount, result, ref indexStart);
            }
            else
            {
                if (result.Length - indexStart < (value == null ? NullBytes : ValueBytes)) return false;

                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.camp, CampMin, CampMax);
                    BitField.WriteBool(value.ready);
                    BitField.WriteInt(value.attackAICount, AiCountMin, AiCountMax);
                    BitField.WriteInt(value.defenseAICount, AiCountMin, AiCountMax);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static CSLobbyUpdate Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                return new CSLobbyUpdate()
                {
                    camp = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    ready = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    attackAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    defenseAICount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < NullBytes) throw new Exception("反序列化 CSLobbyUpdate 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += NullBytes;
                    return null;
                }
                if (invalidIndex - indexStart < ValueBytes) throw new Exception("反序列化 CSLobbyUpdate 失败：剩余数据不足");

                var value = new CSLobbyUpdate()
                {
                    camp = BitField.ReadInt(CampMin, CampMax),
                    ready = BitField.ReadBool(),
                    attackAICount = BitField.ReadInt(AiCountMin, AiCountMax),
                    defenseAICount = BitField.ReadInt(AiCountMin, AiCountMax),
                };
                indexStart += ValueBytes;
                return value;
            }
        }
    }
}
