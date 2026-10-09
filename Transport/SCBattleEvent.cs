namespace Ros.Transport
{
    public class SCBattleEvent
    {
        /// <summary>事件类型：语义判别唯一依据；icon 档位与条目版式由客户端按类型映射（FGUI 表现不进协议）</summary>
        public static class Type
        {
            public const byte PlayerKill = 0;          // 玩家击杀玩家：textId=击杀者名, textId2=受害者名
            public const byte DeathUnattributed = 1;   // 玩家死亡无归属：textId=死者名
            public const byte PlayerRespawn = 2;       // 复活：textId=玩家名
            public const byte PlagueTreeCaptured = 3;  // 攻占瘟疫树
            public const byte PlagueTreeRespawn = 4;   // 瘟疫树刷新
            public const byte Nightfall = 5;           // 天黑
            public const byte Daybreak = 6;            // 天亮
            public const byte BeaconDestroyed = 7;     // 守护点被摧毁：textId 区分哪一座（驱动对应血条）
            public const byte TowerDestroyed = 8;      // 防御塔被摧毁
            public const byte Notice = 9;              // 纯文字提示：textId=NoticeMessageMap 消息 id
        }

        /// <summary>
        /// 文本 id 规则（文本统一用数字传输）：
        /// &lt;PlayerNameBase = NoticeMessageMap 消息 id；
        /// &gt;=PlayerNameBase = 玩家名（clientId = id - PlayerNameBase）
        /// </summary>
        public const int PlayerNameBase = 10000;

        public byte type;
        public int textId;   // 第一段文本
        public int textId2;  // 第二段文本（仅 PlayerKill 使用）
    }

    public struct SCBattleEventSerializer
    {
        public static bool Serialize(SCBattleEvent value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!ByteSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.textId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.textId2, result, ref indexStart)) return false;
            return true;
        }

        public static SCBattleEvent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCBattleEvent()
            {
                type = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                textId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                textId2 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
