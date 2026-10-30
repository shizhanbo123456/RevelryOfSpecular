using System;

namespace Ros.Transport
{
    using UnityEngine;

    // 服务器 → 客户端：战斗飘字（伤害/治疗共用；数值与类型分开传，样式由 FGUI 预设档位按类型决定）
    public class SCCombatLabel
    {
        public int amount;       // 本次数值（伤害量或治疗量，不再用正负号编码暴击）
        public int type;         // 飘字类型，见 CombatLabelType
        public int targetId;     // 受击实体 id（无命中点时按实体头顶定位）
        public bool hasHitPos;
        public Vector3 hitPos;   // 命中点（可选，客户端做水平散布后投影）
    }

    public struct SCCombatLabelSerializer
    {
        public static bool Serialize(SCCombatLabel label, byte[] result, ref int indexStart)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Serialize(label != null, result, ref indexStart)) return false;
                if (label == null) return true;
                if (!IntSerializer.Serialize(label.amount, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(label.type, result, ref indexStart)) return false;
                if (!IntSerializer.Serialize(label.targetId, result, ref indexStart)) return false;
                if (!BoolSerializer.Serialize(label.hasHitPos, result, ref indexStart)) return false;
                if (label.hasHitPos && !Vector3Serializer.Serialize(label.hitPos, result, ref indexStart)) return false;
                return true;
            }
            else
            {
                // 位级：1+20+4+15+1[+15+13+15]=41/84 位
                BitField.SetBuffer(result, indexStart);
                BitField.WriteBool(label != null);
                if (label != null)
                {
                    BitField.WriteInt(label.amount, 0, SyncRanges.CombatLabelAmountMax);
                    BitField.WriteInt(label.type, 0, 15);
                    BitField.WriteInt(label.targetId, -1, SyncRanges.EntityIdMax);
                    BitField.WriteBool(label.hasHitPos);
                    if (label.hasHitPos)
                    {
                        BitField.WriteFloat(label.hitPos.x, 0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                        BitField.WriteFloat(label.hitPos.y, 0f, SyncRanges.PositionYMax, SyncRanges.PositionYStep);
                        BitField.WriteFloat(label.hitPos.z, 0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                    }
                }
                indexStart += BitField.Flush();
                return true;
            }
        }

        public static SCCombatLabel Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform)
            {
                if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
                var d = new SCCombatLabel()
                {
                    amount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    type = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                    targetId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                };
                d.hasHitPos = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
                if (d.hasHitPos) d.hitPos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
                return d;
            }
            else
            {
                if (invalidIndex - indexStart < 1) throw new Exception("反序列化 SCCombatLabel 失败：剩余数据不足");

                BitField.SetBuffer(data, indexStart);
                if (!BitField.ReadBool())
                {
                    indexStart += 1;
                    return null;
                }

                var r = new SCCombatLabel();
                r.amount = BitField.ReadInt(0, SyncRanges.CombatLabelAmountMax);
                r.type = BitField.ReadInt(0, 15);
                r.targetId = BitField.ReadInt(-1, SyncRanges.EntityIdMax);
                r.hasHitPos = BitField.ReadBool();
                if (r.hasHitPos)
                {
                    r.hitPos.x = BitField.ReadFloat(0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                    r.hitPos.y = BitField.ReadFloat(0f, SyncRanges.PositionYMax, SyncRanges.PositionYStep);
                    r.hitPos.z = BitField.ReadFloat(0f, SyncRanges.PositionXZMax, SyncRanges.PositionXZStep);
                }
                indexStart += BitField.ReadByteLength;
                return r;
            }
        }
    }
}
