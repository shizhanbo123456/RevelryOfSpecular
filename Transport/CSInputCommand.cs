using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：输入命令（高频，建议不可靠传输）。
    /// 操作方案（双手键盘，无鼠标）：W/S 前后移动 / A/D 左右移动 / 前后+左右同按 = 向前后移动并逐渐转向 /
    /// J 空手攻击（移动=出拳，静止=跃起砸地）/ K 跳跃 / 左 Shift 滑铲 / U I O L H 技能槽 1~5。
    /// 朝向由服务器在渐转中权威推进，客户端不再上报 yaw。
    /// </summary>
    public struct CSInputCommand
    {
        /// <summary>是否在移动。</summary>
        public bool moving;
        /// <summary>原始按键输入（相对角色自身：x = 左右横移 -1~1，z = 前后 -1~1；转向由服务器按此渐转）。</summary>
        public Vector2 moveDir;
        /// <summary>空手攻击按下（J 键：移动=出拳，静止=跃起砸地）。</summary>
        public bool meleePressed;
        /// <summary>跳跃按下（K 键）。</summary>
        public bool jumpPressed;
        /// <summary>滑铲按下（左 Shift）。</summary>
        public bool slidePressed;
        /// <summary>瞄准点（世界坐标：自动索敌最近可见敌人，没有则角色前方）。</summary>
        public Vector3 aimPoint;
    }

    /// <summary>CSInputCommand 网络序列化器。</summary>
    public struct CSInputCommandSerializer
    {
        public static bool Serialize(CSInputCommand value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value.moving, result, ref indexStart)) return false;
            if (!Vector2Serializer.Serialize(value.moveDir, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.meleePressed, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.jumpPressed, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.slidePressed, result, ref indexStart)) return false;
            return Vector3Serializer.Serialize(value.aimPoint, result, ref indexStart);
        }

        public static CSInputCommand Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSInputCommand()
            {
                moving = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                moveDir = Vector2Serializer.Deserialize(data, ref indexStart, invalidIndex),
                meleePressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                jumpPressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                slidePressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                aimPoint = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
