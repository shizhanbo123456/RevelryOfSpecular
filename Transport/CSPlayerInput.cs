using System;

namespace Ros.Transport
{
    /// <summary>
    /// 玩家按键位（传输用，与 UnityEngine.KeyCode 解耦）。
    /// 全部为触发式边沿位：WASD 拆分为按下/抬起两位（如 WPress/WRelease），
    /// 动作键只有按下位（服务器不消费抬起）。
    /// WASD 按住状态由服务器用按住位（Press 位）掩码维护，释放位右移一位即对应按住位。
    /// </summary>
    [Flags]
    public enum PlayerKey : uint
    {
        None = 0,
        WPress = 1 << 0, WRelease = 1 << 1,
        SPress = 1 << 2, SRelease = 1 << 3,
        APress = 1 << 4, ARelease = 1 << 5,
        DPress = 1 << 6, DRelease = 1 << 7,  // 移动（按下/抬起边沿）
        J = 1 << 8, K = 1 << 9, LShift = 1 << 10,                        // 攻击 / 跳跃 / 滑铲
        U = 1 << 11, I = 1 << 12, O = 1 << 13, L = 1 << 14, H = 1 << 15, Y = 1 << 16, // 技能槽 1~6

        /// <summary>WASD 按下位掩码（服务器按住状态只存这四位）。</summary>
        MovePressMask = WPress | SPress | APress | DPress,
        /// <summary>WASD 抬起位掩码（右移一位 = 对应按下位）。</summary>
        MoveReleaseMask = WRelease | SRelease | ARelease | DRelease,
    }

    /// <summary>客户端 → 服务器：输入（可靠单通道，只在本帧有按键边沿时发送；无字段区分含义，按键位自解释）。</summary>
    public struct CSPlayerInput
    {
        /// <summary>本帧触发的按键位（WASD 含抬起位，其余仅按下位）</summary>
        public PlayerKey pressed;
    }

    /// <summary>CSPlayerInput 网络序列化器。</summary>
    public struct CSPlayerInputSerializer
    {
        public static bool Serialize(CSPlayerInput value, byte[] result, ref int indexStart)
        {
            return IntSerializer.Serialize((int)value.pressed, result, ref indexStart);
        }

        public static CSPlayerInput Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSPlayerInput()
            {
                pressed = (PlayerKey)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
