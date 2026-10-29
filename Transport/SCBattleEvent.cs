using System;

namespace Ros.Transport
{
    public class SCBattleEvent
    {
        // 事件语义的唯一判别依据；icon 档位与条目版式由客户端按类型映射，不进协议
        public static class Type
        {
            public const byte PlayerKill = 0;          // 玩家击杀玩家：textId=击杀者名, textId2=受害者名
            public const byte DeathUnattributed = 1;   // 玩家死亡无归属：textId=死者名
            public const byte PlayerRespawn = 2;       // 复活：textId=玩家名
            public const byte NeutralStatueCaptured = 3;  // 攻占中立雕像
            public const byte NeutralStatueRespawn = 4;   // 中立雕像刷新
            public const byte Nightfall = 5;           // 天黑
            public const byte Daybreak = 6;            // 天亮
            public const byte StatueDestroyed = 7;     // 雕像被摧毁：textId 区分哪一座（驱动对应血条）
            public const byte CrystalStatueDestroyed = 8;      // 水晶雕像被摧毁
            public const byte Notice = 9;              // 纯文字提示：textId=notice.csv 消息 id
            public const byte SkillBroadcast = 10;     // 特殊技能播报：textId=技能 id（名称由客户端查 skill.csv）
        }

        // 文本 id：<10000 查 notice.csv；≥10000 为玩家名（clientId = id - PlayerNameBase）
        public const int PlayerNameBase = 10000;

        public byte type;
        public int textId;   // 第一段文本
        public int textId2;  // 第二段文本（仅 PlayerKill 使用）
    }

    public struct SCBattleEventSerializer
    {
        public static bool Serialize(SCBattleEvent value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
                if (value == null) return true;
                if (!ByteSerializer.Serialize(value.type, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.textId, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(value.textId2, result, ref indexStart)) return false;
                return true;
            }
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 5)) return false;

                // 位级：1+4+16+16=37 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.type, 0, 9);
                    BitField.WriteInt(value.textId, 0, 65535);
                    BitField.WriteInt(value.textId2, 0, 65535);
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCBattleEvent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                return new SCBattleEvent()
                {
                    type = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    textId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    textId2 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCBattleEvent 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 5) throw new Exception("反序列化 SCBattleEvent 失败：剩余数据不足");

                var r = new SCBattleEvent();
                r.type = (byte)BitField.ReadInt(0, 9);
                r.textId = BitField.ReadInt(0, 65535);
                r.textId2 = BitField.ReadInt(0, 65535);
                indexStart += 5;
                return r;
            }
        }
    }
}
