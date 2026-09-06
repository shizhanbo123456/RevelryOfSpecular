using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：输入命令（高频，建议不可靠传输）。
    /// 操作方案见策划案 12 章：WASD 移动 / J 空手攻击（移动=出拳，静止=跃起砸地）/
    /// K 跳跃 / U I O L H 技能槽 1~5 触发（技能释放走 CSUseSkillRequest 单独发送）。
    /// </summary>
    public struct CSInputCommand
    {
        /// <summary>是否在移动。</summary>
        public bool moving;
        /// <summary>朝向（欧拉角 Y，度）。</summary>
        public float yaw;
        /// <summary>移动方向（相对相机，x=横向 z=纵向）。</summary>
        public Vector2 moveDir;
        /// <summary>空手攻击按下（J 键：移动=出拳，静止=跃起砸地）。</summary>
        public bool meleePressed;
        /// <summary>跳跃按下（K 键）。</summary>
        public bool jumpPressed;
        /// <summary>滑铲按下（触发键待定，暂保留字段）。</summary>
        public bool slidePressed;
        /// <summary>瞄准点（世界坐标：自动索敌最近可见敌人，没有则向前方）。</summary>
        public Vector3 aimPoint;

        public CSInputCommand(bool moving, float yaw)
        {
            this.moving = moving;
            this.yaw = yaw;
            moveDir = Vector2.zero;
            meleePressed = false;
            jumpPressed = false;
            slidePressed = false;
            aimPoint = Vector3.zero;
        }
    }

    /// <summary>CSInputCommand 网络序列化器。</summary>
    public struct CSInputCommandSerializer
    {
        public static bool Serialize(CSInputCommand value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value.moving, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.yaw, result, ref indexStart)) return false;
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
                yaw = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                moveDir = Vector2Serializer.Deserialize(data, ref indexStart, invalidIndex),
                meleePressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                jumpPressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                slidePressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                aimPoint = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
