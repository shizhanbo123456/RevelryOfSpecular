using System;

namespace Ros.Transport
{
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
        KRelease = 1 << 17, LShiftRelease = 1 << 18,                     // 长按抬起边沿（飞行高度控制预留）

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
            if (!EnsInstance.enableBitTransform) return SerializeBytes(value, result, ref indexStart);
            if (result.Length - indexStart < 3) return false;

            // 位级：PlayerKey 位标志最高 1<<18，19 位
            BitField.SetBuffer(result, indexStart);
            BitField.WriteInt((int)value.pressed, 0, 524287);
            indexStart += BitField.Flush();
            return true;
        }

        public static CSPlayerInput Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!EnsInstance.enableBitTransform) return DeserializeBytes(data, ref indexStart, invalidIndex);
            if (invalidIndex - indexStart < 3) throw new Exception("反序列化 CSPlayerInput 失败：剩余数据不足");

            BitField.SetBuffer(data, indexStart);
            var r = new CSPlayerInput();
            r.pressed = (PlayerKey)BitField.ReadInt(0, 524287);
            indexStart += 3;
            return r;
        }

        private static bool SerializeBytes(CSPlayerInput value, byte[] result, ref int indexStart)
        {
            return IntSerializer.Serialize((int)value.pressed, result, ref indexStart);
        }

        private static CSPlayerInput DeserializeBytes(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSPlayerInput()
            {
                pressed = (PlayerKey)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
