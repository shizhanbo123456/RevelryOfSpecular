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
        public const int BufferSize = 256;

        public byte SkillId;
        public byte SkillWave;
        public SkillTarget caster;
        public byte[] buffer;
        public int length; // 已写字节数，客户端可读的上界

        private int cursor;

        public SCSkillDisplayContext()
        {
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
            length = cursor;
        }

        public void WriteInt(int value)
        {
            if (!IntSerializer.Serialize(value, buffer, ref cursor)) Overflow(4);
            length = cursor;
        }

        public void WriteFloat(float value)
        {
            if (!FloatSerializer.Serialize(value, buffer, ref cursor)) Overflow(4);
            length = cursor;
        }

        public void WriteVector3(Vector3 value)
        {
            if (!Vector3Serializer.Serialize(value, buffer, ref cursor)) Overflow(12);
            length = cursor;
        }

        // 目标占 14B：id(2) + pos(12)
        public void WriteTarget(SkillTarget value)
        {
            if (!UshortSerializer.Serialize(value.id, buffer, ref cursor)) Overflow(2);
            if (!Vector3Serializer.Serialize(value.pos, buffer, ref cursor)) Overflow(14);
            length = cursor;
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

        private void Overflow(int need)
        {
            throw new System.Exception(
                $"[技能包] 超长：skill={SkillId} wave={SkillWave} 已写 {cursor} 字节，本项需 {need}，上限 {buffer.Length}");
        }

        private void EnsureRead(int need)
        {
            if (cursor + need <= length) return;
            throw new System.Exception(
                $"[技能包] 读越界：skill={SkillId} wave={SkillWave} 已读 {cursor} / {length} 字节，本项需 {need}");
        }
    }
}
