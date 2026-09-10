using UnityEngine;

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
            public const byte TowerDestroyed = 5;
            public const byte PlagueTreeCaptured = 6;
            public const byte Respawn = 7;
            public const byte YzStackUp = 8; // 愈战愈勇叠层
            public const byte ShowText = 9;  // 飘字
            public const byte DayNight = 10; // 昼夜阶段切换（value=阶段 0白天 1黄昏 2夜晚 3黎明）
        }

        /// <summary>事件类型（Type 常量）。</summary>
        public byte type;
        /// <summary>事件发起者实体 id。</summary>
        public ushort sourceId;
        /// <summary>事件目标实体 id。</summary>
        public ushort targetId;
        /// <summary>附加值（伤害/得分/叠层等）。</summary>
        public int value;
        /// <summary>事件位置（飘字/特效用）。</summary>
        public Vector3 position;
        /// <summary>文本内容（ShowText 用）。</summary>
        public string text = "";
    }

    /// <summary>SCBattleEvent 网络序列化器。</summary>
    public struct SCBattleEventSerializer
    {
        public static bool Serialize(SCBattleEvent value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!ByteSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.sourceId, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.targetId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.value, result, ref indexStart)) return false;
            if (!Vector3Serializer.Serialize(value.position, result, ref indexStart)) return false;
            return StringSerializer.Serialize(value.text ?? "", result, ref indexStart);
        }

        public static SCBattleEvent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCBattleEvent()
            {
                type = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sourceId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                targetId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                value = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                position = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
                text = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
