using UnityEngine;

namespace Ros.Transport
{
    public struct SkillTarget
    {
        public ushort id;
        public Vector3 pos;

        // pos = 打包瞬间的体中心；客户端查不到该实体时才用它
        public SkillTarget(EntityData data)
        {
            id = data.id;
            pos = EntityAnchor.Center.GetTransform(data.ModelInfo, null).position;
        }

        public SkillTarget(ushort id, Vector3 pos)
        {
            this.id = id;
            this.pos = pos;
        }
    }

    // 一个波次包：SkillId / SkillWave / caster 是每个包都有的头，其余由技能类按自己的顺序写进 buffer
    public class SCSkillDisplayContext
    {
        // EnsTemporaryBuffer 256B（首位是函数号）− 线上头 18B = payload 可写上限
        public const int BufferSize = 237;

        public byte SkillId;
        public byte SkillWave;
        public SkillTarget caster;
        public byte[] buffer;
        public int length; // 已写字节数，客户端可读的上界

        private int cursor;

        public SCSkillDisplayContext()
        {
            // 池按 2 的幂分档，实际给的数组可能比预算大，写入一律以 BufferSize 为准
            buffer = BytesPool.GetBuffer(BufferSize);
        }

        ~SCSkillDisplayContext()
        {
            if (buffer != null)
            {
                BytesPool.ReturnBuffer(buffer);
            }
        }

        #region 写（服务器）
        public void WriteByte(byte value)
        {
            if (!ByteSerializer.Serialize(value, buffer, ref cursor)) Overflow(1);
            Commit(1);
        }

        public void WriteInt(int value)
        {
            if (!IntSerializer.Serialize(value, buffer, ref cursor)) Overflow(4);
            Commit(4);
        }

        public void WriteFloat(float value)
        {
            if (!FloatSerializer.Serialize(value, buffer, ref cursor)) Overflow(4);
            Commit(4);
        }

        public void WriteVector3(Vector3 value)
        {
            if (!Vector3Serializer.Serialize(value, buffer, ref cursor)) Overflow(12);
            Commit(12);
        }

        // 目标占 14B：id(2) + pos(12)
        public void WriteTarget(SkillTarget value)
        {
            if (!UshortSerializer.Serialize(value.id, buffer, ref cursor)) Overflow(2);
            if (!Vector3Serializer.Serialize(value.pos, buffer, ref cursor)) Overflow(14);
            Commit(14);
        }
        #endregion

        #region 读（客户端）
        public byte ReadByte()
        {
            EnsureRead(1);
            return ByteSerializer.Deserialize(buffer, ref cursor, length);
        }

        public int ReadInt()
        {
            EnsureRead(4);
            return IntSerializer.Deserialize(buffer, ref cursor, length);
        }

        public float ReadFloat()
        {
            EnsureRead(4);
            return FloatSerializer.Deserialize(buffer, ref cursor, length);
        }

        public Vector3 ReadVector3()
        {
            EnsureRead(12);
            return Vector3Serializer.Deserialize(buffer, ref cursor, length);
        }

        public SkillTarget ReadTarget()
        {
            EnsureRead(14);
            ushort id = UshortSerializer.Deserialize(buffer, ref cursor, length);
            Vector3 pos = Vector3Serializer.Deserialize(buffer, ref cursor, length);
            return new SkillTarget(id, pos);
        }
        #endregion

        // 池给的数组可能大于预算，按 BufferSize 卡：超出即整包放不进 EnsTemporaryBuffer
        private void Commit(int need)
        {
            if (cursor > BufferSize) Overflow(need);
            length = cursor;
        }

        private void Overflow(int need)
        {
            throw new System.Exception(
                $"[技能包] 超长：skill={SkillId} wave={SkillWave} 已写 {cursor} 字节，本项需 {need}，上限 {BufferSize}");
        }

        private void EnsureRead(int need)
        {
            if (cursor + need <= length) return;
            throw new System.Exception(
                $"[技能包] 读越界：skill={SkillId} wave={SkillWave} 已读 {cursor} / {length} 字节，本项需 {need}");
        }
    }

    // 线上布局：SkillId(1) + SkillWave(1) + caster.id(2) + caster.pos(12) + payload 长度(2) + payload
    public struct SCSkillDisplayContextSerializer
    {
        public static bool Serialize(SCSkillDisplayContext value, byte[] result, ref int indexStart)
        {
            if (value == null) throw new System.Exception("[技能包] 序列化收到空上下文");
            if (!ByteSerializer.Serialize(value.SkillId, result, ref indexStart)) return false;
            if (!ByteSerializer.Serialize(value.SkillWave, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize(value.caster.id, result, ref indexStart)) return false;
            if (!Vector3Serializer.Serialize(value.caster.pos, result, ref indexStart)) return false;
            if (!UshortSerializer.Serialize((ushort)value.length, result, ref indexStart)) return false;
            for (int i = 0; i < value.length; i++)
            {
                if (!ByteSerializer.Serialize(value.buffer[i], result, ref indexStart)) return false;
            }
            return true;
        }

        public static SCSkillDisplayContext Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            var context = new SCSkillDisplayContext();
            context.SkillId = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            context.SkillWave = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            ushort casterId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            Vector3 casterPos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex);
            context.caster = new SkillTarget(casterId, casterPos);
            int length = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (length > SCSkillDisplayContext.BufferSize)
                throw new System.Exception($"[技能包] payload 长度 {length} 超出预算 {SCSkillDisplayContext.BufferSize}");
            for (int i = 0; i < length; i++)
            {
                context.buffer[i] = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            }
            context.length = length;
            return context;
        }
    }
}
