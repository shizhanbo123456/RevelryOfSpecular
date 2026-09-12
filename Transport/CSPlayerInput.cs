using System;

namespace Ros.Transport
{
    /// <summary>玩家按键位（传输用，与 UnityEngine.KeyCode 解耦）。</summary>
    [Flags]
    public enum PlayerKey : ushort
    {
        None = 0,
        W = 1 << 0, S = 1 << 1, A = 1 << 2, D = 1 << 3,                            // 移动
        J = 1 << 4, K = 1 << 5, LShift = 1 << 6,                                  // 攻击 / 跳跃 / 滑铲
        U = 1 << 7, I = 1 << 8, O = 1 << 9, L = 1 << 10, H = 1 << 11, Y = 1 << 12, // 技能槽 1~6
    }

    /// <summary>客户端 → 服务器：移动输入（WASD，高频不可靠）。</summary>
    public struct CSMoveInput
    {
        /// <summary>本帧按下的移动键</summary>
        public PlayerKey pressed;
        /// <summary>本帧抬起的移动键</summary>
        public PlayerKey released;
    }

    /// <summary>客户端 → 服务器：动作输入（攻击/跳跃/滑铲/技能槽，可靠；无抬起事件）。</summary>
    public struct CSActionInput
    {
        /// <summary>本帧按下的动作键</summary>
        public PlayerKey pressed;
    }

    /// <summary>CSMoveInput 网络序列化器。</summary>
    public struct CSMoveInputSerializer
    {
        public static bool Serialize(CSMoveInput value, byte[] result, ref int indexStart)
        {
            if (!UshortSerializer.Serialize((ushort)value.pressed, result, ref indexStart)) return false;
            return UshortSerializer.Serialize((ushort)value.released, result, ref indexStart);
        }

        public static CSMoveInput Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSMoveInput()
            {
                pressed = (PlayerKey)UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                released = (PlayerKey)UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }

    /// <summary>CSActionInput 网络序列化器。</summary>
    public struct CSActionInputSerializer
    {
        public static bool Serialize(CSActionInput value, byte[] result, ref int indexStart)
        {
            return UshortSerializer.Serialize((ushort)value.pressed, result, ref indexStart);
        }

        public static CSActionInput Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSActionInput()
            {
                pressed = (PlayerKey)UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
