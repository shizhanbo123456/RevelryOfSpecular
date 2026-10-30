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
            if (!EnsInstance.enableBitTransform)
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
            else
            {
                if (result.Length - indexStart < (value == null ? 1 : 12)) return false;

                // 位级：1+15+32+10+2+6+3+12+1+3+7=92 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(value != null);
                if (value != null)
                {
                    BitField.WriteInt(value.entityId, 0, SyncRanges.EntityIdMax);
                    BitField.WriteInt(value.animId, int.MinValue, int.MaxValue);   // 动画状态 fullPathHash，可为负
                    BitField.WriteFloat(value.animFrame, 0f, 1f, 0.001f);          // 归一化播放进度
                    BitField.WriteInt(value.animParams.characterType, 0, 3);
                    BitField.WriteInt(value.animParams.attackId, 0, 63);
                    BitField.WriteBool(value.animParams.inAir);
                    BitField.WriteBool(value.animParams.moving);
                    BitField.WriteBool(value.animParams.slide);
                    BitField.WriteFloat(value.moveSpeedScale, 0f, SyncRanges.MoveSpeedScaleMax, 0.01f);
                    BitField.WriteBool(value.paused);
                    BitField.WriteInt(value.heldWeaponCategory, 0, 7);
                    BitField.WriteInt(value.heldWeaponIndex, -1, 63);              // -1 = 空手 / 无武器
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCEntityAnimInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
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
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCEntityAnimInfo 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }
                if (invalidIndex - indexStart < 12) throw new Exception("反序列化 SCEntityAnimInfo 失败：剩余数据不足");

                var r = new SCEntityAnimInfo();
                r.entityId = (ushort)BitField.ReadInt(0, SyncRanges.EntityIdMax);
                r.animId = BitField.ReadInt(int.MinValue, int.MaxValue);
                r.animFrame = BitField.ReadFloat(0f, 1f, 0.001f);
                r.animParams = new AnimParamPack() { characterType = BitField.ReadInt(0, 3), attackId = BitField.ReadInt(0, 63), inAir = BitField.ReadBool(), moving = BitField.ReadBool(), slide = BitField.ReadBool() };
                r.moveSpeedScale = BitField.ReadFloat(0f, SyncRanges.MoveSpeedScaleMax, 0.01f);
                r.paused = BitField.ReadBool();
                r.heldWeaponCategory = BitField.ReadInt(0, 7);
                r.heldWeaponIndex = BitField.ReadInt(-1, 63);
                indexStart += 12;
                return r;
            }
        }
    }
}
