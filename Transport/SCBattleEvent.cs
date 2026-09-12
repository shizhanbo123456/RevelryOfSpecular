namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：战斗事件（击杀/拆除守护点/采集水晶/攻占瘟疫树等）。
    /// 客户端据此播放表现与提示，不参与规则判定。
    /// 注意：蘑菇感染不使用事件——客户端按实体同步 Buff 中的 MushroomInfect 显隐切换模型。
    /// </summary>
    public class SCBattleEvent
    {
        /// <summary>事件类型常量。</summary>
        public static class Type
        {
            public const byte Kill = 0;
            public const byte BeaconDestroyed = 1;
            /// <summary>水晶被摧毁且有产出（正常资源收益）。</summary>
            public const byte CrystalCollected = 2;
            /// <summary>水晶被摧毁但无产出（被「蘑菇感染」的水晶被进攻方摧毁）。</summary>
            public const byte CrystalBroken = 3;
            public const byte PlagueTreeCaptured = 6;
            public const byte ShowText = 9; // 飘字（value = NoticeMessageMap 消息 id）
        }

        /// <summary>事件类型（Type 常量）。</summary>
        public byte type;
        /// <summary>附加值（伤害/得分/ShowText 时 = NoticeMessageMap 消息 id）。</summary>
        public int value;
    }

    /// <summary>SCBattleEvent 网络序列化器。</summary>
    public struct SCBattleEventSerializer
    {
        public static bool Serialize(SCBattleEvent value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!ByteSerializer.Serialize(value.type, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.value, result, ref indexStart);
        }

        public static SCBattleEvent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCBattleEvent()
            {
                type = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                value = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
