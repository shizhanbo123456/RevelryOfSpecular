using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 客户端 → 服务器：输入命令（高频，建议不可靠传输）。
    /// 操作方案见策划案 10.2：WASD 移动 / 左键近战 / 右键技能 / 滚轮选技能 / Shift 滑铲。
    /// </summary>
    public struct CSInputCommand
    {
        /// <summary>是否在移动。</summary>
        public bool moving;
        /// <summary>朝向（欧拉角 Y，弧度？度，统一用度）。</summary>
        public float yaw;
        /// <summary>移动方向（相对相机，x=横向 z=纵向）。</summary>
        public Vector2 moveDir;
        /// <summary>左键近身攻击按下（移动=连段动作，静止=跃起砸地）。</summary>
        public bool meleePressed;
        /// <summary>右键技能触发按下（仅选中远程/施法类技能时有效）。</summary>
        public bool skillPressed;
        /// <summary>Shift 滑铲按下。</summary>
        public bool slidePressed;
        /// <summary>滚轮增量（>0 下一技能，<0 上一技能）。</summary>
        public int skillScrollDelta;
        /// <summary>当前选中技能 id（-1 表示未选中）。</summary>
        public int selectedSkillId = -1;
        /// <summary>瞄准点（世界坐标）。</summary>
        public Vector3 aimPoint;

        public CSInputCommand(bool moving, float yaw)
        {
            this.moving = moving;
            this.yaw = yaw;
            moveDir = Vector2.zero;
            meleePressed = false;
            skillPressed = false;
            slidePressed = false;
            skillScrollDelta = 0;
            selectedSkillId = -1;
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
            if (!BoolSerializer.Serialize(value.skillPressed, result, ref indexStart)) return false;
            if (!BoolSerializer.Serialize(value.slidePressed, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.skillScrollDelta, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.selectedSkillId, result, ref indexStart)) return false;
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
                skillPressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                slidePressed = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                skillScrollDelta = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                selectedSkillId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                aimPoint = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
