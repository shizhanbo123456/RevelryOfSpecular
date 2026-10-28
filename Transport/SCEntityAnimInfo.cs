using System;

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
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < (value == null ? 1 : 10)) return false;

            // 位级：1+16+8+15+15+9+1+4+6=75 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt(value.entityId, 0, 65535);
                BitField.WriteInt(value.animId, 0, 255);
                BitField.WriteFloat(value.animFrame, 0f, 256f, 0.01f);
                BitField.WriteInt(value.animParams.characterType, 0, 15);
                BitField.WriteInt(value.animParams.attackId, 0, 255);
                BitField.WriteBool(value.animParams.inAir);
                BitField.WriteBool(value.animParams.moving);
                BitField.WriteBool(value.animParams.slide);
                BitField.WriteFloat(value.moveSpeedScale, 0f, 4f, 0.01f);
                BitField.WriteBool(value.paused);
                BitField.WriteInt(value.heldWeaponCategory, 0, 15);
                BitField.WriteInt(value.heldWeaponIndex, 0, 63);
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCEntityAnimInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCEntityAnimInfo 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }
            if (invalidIndex - indexStart < 10) throw new Exception("反序列化 SCEntityAnimInfo 失败：剩余数据不足");

            var r = new SCEntityAnimInfo();
            r.entityId = (ushort)BitField.ReadInt(0, 65535);
            r.animId = BitField.ReadInt(0, 255);
            r.animFrame = BitField.ReadFloat(0f, 256f, 0.01f);
            r.animParams = new AnimParamPack() { characterType = BitField.ReadInt(0, 15), attackId = BitField.ReadInt(0, 255), inAir = BitField.ReadBool(), moving = BitField.ReadBool(), slide = BitField.ReadBool() };
            r.moveSpeedScale = BitField.ReadFloat(0f, 4f, 0.01f);
            r.paused = BitField.ReadBool();
            r.heldWeaponCategory = BitField.ReadInt(0, 15);
            r.heldWeaponIndex = BitField.ReadInt(0, 63);
            indexStart += 10;
            return r;
        }

        private static bool SerializeBytes(SCEntityAnimInfo value, byte[] result, ref int indexStart)
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

        private static SCEntityAnimInfo DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
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
