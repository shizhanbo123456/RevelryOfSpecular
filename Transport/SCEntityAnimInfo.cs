using UnityEngine;

namespace Ros.Transport
{
    public class SCEntityAnimInfo
    {
        public ushort entityId;
        public int animId;
        public float animFrame;
        public AnimParamPack animParams;
        public float moveSpeedScale = 1f;
        public bool paused;
        public int heldWeaponCategory;
        public int heldWeaponIndex = -1;
    }

    public struct SCEntityAnimInfoSerializer
    {
        public static bool Serialize(SCEntityAnimInfo value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!UshortSerializer.Serialize(value.entityId, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.animId, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.animFrame, result, ref indexStart)) return false;
            if (!AnimParamPackSerializer.Serialize(value.animParams, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.moveSpeedScale, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.paused, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.heldWeaponCategory, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.heldWeaponIndex, result, ref indexStart);
        }

        public static SCEntityAnimInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var info = new SCEntityAnimInfo()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animFrame = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animParams = AnimParamPackSerializer.Deserialize(data, ref indexStart, invalidIndex),
                moveSpeedScale = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                paused = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            info.heldWeaponCategory = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            info.heldWeaponIndex = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            return info;
        }
    }
}
