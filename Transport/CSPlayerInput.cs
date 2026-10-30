using System;

namespace Ros.Transport
{
    // 移动与通用动作键；技能键不走这个包，按下时直接上行技能 id（见 CSSkillCast）
    [Flags]
    public enum PlayerKey : uint
    {
        None = 0,
        WPress = 1 << 0, WRelease = 1 << 1,
        SPress = 1 << 2, SRelease = 1 << 3,
        APress = 1 << 4, ARelease = 1 << 5,
        DPress = 1 << 6, DRelease = 1 << 7,  // 移动（按下/抬起边沿）
        K = 1 << 8, LShift = 1 << 9,        // 跳跃 / 滑铲
        KRelease = 1 << 10, LShiftRelease = 1 << 11, // 长按抬起边沿（飞行高度控制预留）

        MovePressMask = WPress | SPress | APress | DPress,
        MoveReleaseMask = WRelease | SRelease | ARelease | DRelease,
    }

    public struct CSPlayerInput
    {
        public PlayerKey pressed;
    }

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
