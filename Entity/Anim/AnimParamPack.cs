/// <summary>
/// EntityAnim 的 Animator 持久参数打包（CharacterType / AttackId / InAir / Moving / Slide）。
/// **trigger 不参与网络传输**：trigger 的作用是驱动状态切换，而状态切换本身已由
/// "状态 hash 变化 → 动画事件" 同步（见《代码架构说明》动画同步节），再单独传 trigger 只会重复且时序不可靠。
/// 因此本包只在**动画事件**（SCEntityAnimInfo）里下发，客户端 ApplyParamPack 还原参数供自身 Controller 使用。
/// </summary>
public struct AnimParamPack
{
    public int characterType;   // CharacterType（动作集分支：Female/Male/Zombie）
    public int attackId;        // AttackId（攻击子状态选择）
    public bool inAir;          // InAir
    public bool moving;         // Moving
    public bool slide;          // Slide

    public static AnimParamPack Default => default;
}

/// <summary>AnimParamPack 网络序列化器。</summary>
public struct AnimParamPackSerializer
{
    public static bool Serialize(AnimParamPack value, byte[] result, ref int indexStart)
    {
        if (!IntSerializer.Serialize(value.characterType, result, ref indexStart)) return false;
        if (!IntSerializer.Serialize(value.attackId, result, ref indexStart)) return false;
        if (!BoolSerializer.Serialize(value.inAir, result, ref indexStart)) return false;
        if (!BoolSerializer.Serialize(value.moving, result, ref indexStart)) return false;
        if (!BoolSerializer.Serialize(value.slide, result, ref indexStart)) return false;
        return true;
    }

    public static AnimParamPack Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        return new AnimParamPack()
        {
            characterType = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            attackId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            inAir = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            moving = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            slide = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
        };
    }
}
