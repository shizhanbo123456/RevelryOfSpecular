using System;

namespace Ros.Transport
{
    using UnityEngine;

    // 服务器 → 客户端：伤害飘字（高频包；数值与类型分开传，样式由 FGUI 预设档位按类型决定）
    public class SCDamage
    {
        public int value;        // 伤害数值（不再用正负号编码暴击）
        public int type;         // 伤害类型，见 DamageType
        public int targetId;     // 受击实体 id（无命中点时按实体头顶定位）
        public bool hasHitPos;
        public Vector3 hitPos;   // 命中点（可选，客户端做水平散布后投影）
    }

    public struct SCDamageSerializer
    {
        public static bool Serialize(SCDamage value, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);

            // 位级：1+16+4+16+1[+3*15]=38/83 位；命中点按 -500~500 / 0.05 量化
            BitField.SetBuffer(result, indexStart);
            BitField.WriteBool(value != null);
            if (value != null)
            {
                BitField.WriteInt(value.value, 0, 65535);
                BitField.WriteInt(value.type, 0, 15);
                BitField.WriteInt(value.targetId, -1, 32767);
                BitField.WriteBool(value.hasHitPos);
                if (value.hasHitPos)
                {
                    BitField.WriteFloat(value.hitPos.x, -500f, 500f, 0.05f);
                    BitField.WriteFloat(value.hitPos.y, -500f, 500f, 0.05f);
                    BitField.WriteFloat(value.hitPos.z, -500f, 500f, 0.05f);
                }
            }
            indexStart += BitField.Flush();
            return true;
        }

        public static SCDamage Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCDamage 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            if (!BitField.ReadBool())
            {
                indexStart += 1;
                return null;
            }

            var r = new SCDamage();
            r.value = BitField.ReadInt(0, 65535);
            r.type = BitField.ReadInt(0, 15);
            r.targetId = BitField.ReadInt(-1, 32767);
            r.hasHitPos = BitField.ReadBool();
            if (r.hasHitPos)
            {
                r.hitPos.x = BitField.ReadFloat(-500f, 500f, 0.05f);
                r.hitPos.y = BitField.ReadFloat(-500f, 500f, 0.05f);
                r.hitPos.z = BitField.ReadFloat(-500f, 500f, 0.05f);
            }
            indexStart += BitField.ReadByteLength;
            return r;
        }

        private static bool SerializeBytes(SCDamage value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.value, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.type, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.targetId, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.hasHitPos, result, ref indexStart)) return false;
            if (value.hasHitPos && !Vector3Serializer.Serialize(value.hitPos, result, ref indexStart)) return false;
            return true;
        }

        private static SCDamage DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var d = new SCDamage()
            {
                value = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                type = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                targetId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
            d.hasHitPos = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (d.hasHitPos) d.hitPos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
            return d;
        }
    }
}
