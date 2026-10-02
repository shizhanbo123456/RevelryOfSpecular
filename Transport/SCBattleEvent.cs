namespace Ros.Transport
{
    using UnityEngine;

    public class SCBattleEvent
    {
        public static class Type
        {
            public const byte Kill = 0;
            public const byte BeaconDestroyed = 1;
            public const byte CrystalCollected = 2;
            public const byte CrystalBroken = 3;
            public const byte PlagueTreeCaptured = 6;
            public const byte ShowText = 9; // 飘字（value = NoticeMessageMap 消息 id）
            public const byte Damage = 10;
        }

        public byte type;
        public int value;
        public int targetId;
        public Vector3 hitPos;
        public bool hasHitPos;
    }

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
