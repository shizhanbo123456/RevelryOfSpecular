using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 服务器 → 客户端：**动画事件**（事件驱动，低频）。
    /// 仅在实体动画的"状态 hash 变化"或"播放速度（倍率 / 强控暂停）变化"时下发——
    /// trigger 不参与传输（触发必然引起状态变化，状态变化本身就是同步信号）。
    /// 客户端收到后还原持久参数、按服务器状态对齐播放，并镜像播放速度（含强控暂停）。
    /// **这是动画的唯一同步通道**：姿态包（SCEntityDisplayInfo）不含任何动画信息，
    /// 新进入视野的实体由服务器补发一条本事件给出初始状态。
    /// </summary>
    public class SCEntityAnimInfo
    {
        /// <summary>实体 id。</summary>
        public ushort entityId;
        /// <summary>状态标识 = 状态的 fullPathHash（两端一致）。</summary>
        public int animId;
        /// <summary>发包时刻该状态的归一化播放进度（0~1）。</summary>
        public float animFrame;
        /// <summary>持久参数快照（CharacterType / AttackId / InAir / Moving / Slide）。</summary>
        public AnimParamPack animParams;
        /// <summary>动画播放倍率（移速类 Buff 的载体；客户镜像写 animator.speed）。</summary>
        public float moveSpeedScale = 1f;
        /// <summary>是否被强控暂停（true = 动画播放速度置 0）。</summary>
        public bool paused;
    }

    /// <summary>SCEntityAnimInfo 网络序列化器。</summary>
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
            return true;
        }

        public static SCEntityAnimInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCEntityAnimInfo()
            {
                entityId = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animFrame = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                animParams = AnimParamPackSerializer.Deserialize(data, ref indexStart, invalidIndex),
                moveSpeedScale = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                paused = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
