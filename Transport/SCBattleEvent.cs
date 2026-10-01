namespace Ros.Transport
{
    using UnityEngine;

    /// <summary>
    /// 服务器 → 客户端：战斗事件（击杀/拆除守护点/采集水晶/攻占瘟疫树等）。
    /// 客户端据此播放表现与提示，不参与规则判定。
    /// 注意：蘑菇感染不使用事件——客户端按实体同步 Buff 中的 MushroomInfect 显隐切换模型。
    /// </summary>
    public class SCBattleEvent
    {
        /// <summary>事件类型常量表（本类的嵌套类，与 System.Type 无关；引用形如 SCBattleEvent.Type.Kill）。</summary>
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
            /// <summary>单次伤害飘字（value：0=无效，>0=普通伤害，<0=暴击伤害取绝对值；targetId=受击实体）。</summary>
            public const byte Damage = 10;
        }

        /// <summary>事件类型（Type 常量）。</summary>
        public byte type;
        /// <summary>附加值（伤害/得分/ShowText 时 = NoticeMessageMap 消息 id）。</summary>
        public int value;
        /// <summary>关联实体 id（Damage 时 = 受击实体，客户端据此定位飘字）。</summary>
        public int targetId;
        /// <summary>Damage 时 = 命中位置（子弹位置/判定球心），飘字定位优先用它而非实体头顶。</summary>
        public Vector3 hitPos;
        /// <summary>hitPos 是否有效（无范围伤害如 DoT 为 false，客户端回退实体头顶定位）。</summary>
        public bool hasHitPos;
    }

    /// <summary>SCBattleEvent 网络序列化器。</summary>
    public struct SCBattleEventSerializer
    {
        public static bool Serialize(SCBattleEvent value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!ByteSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.value, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.targetId, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.hasHitPos, result, ref indexStart)) return false;
            if (value.hasHitPos && !Vector3Serializer.Serialize(value.hitPos, result, ref indexStart)) return false;
            return true;
        }

        public static SCBattleEvent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var e = new SCBattleEvent()
            {
                type = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                value = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                targetId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            e.hasHitPos = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (e.hasHitPos) e.hitPos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
            return e;
        }
    }
}
