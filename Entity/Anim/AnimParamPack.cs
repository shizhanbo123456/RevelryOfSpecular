public struct AnimParamPack
{
    public int characterType;   // CharacterType（动作集分支：Female/Male/Zombie）
    public int attackId;        // AttackId（攻击子状态选择）
    public bool inAir;          // InAir
    public bool moving;         // Moving
    public bool slide;          // Slide

    public static AnimParamPack Default => default;
}

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
