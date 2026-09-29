/// <summary>
/// EntityAnim 的 Animator 参数打包（方案 B：参数随表现摘要逐包同步）。
/// 持久参数：CharacterType / AttackId / InAir / Moving / Slide，随包全量下发；
/// Trigger（一次性）：仅"服务器本帧内被设置"时为 true（EntityAnim 按 Time.frameCount 打戳），
/// 客户端 ApplyParamPack 原样 SetTrigger 交由 Controller 自动转换；
/// 若客户端错过该帧包，由后续同步的状态 hash（animId）兜底对齐。
/// </summary>
public struct AnimParamPack
{
    public int characterType;   // CharacterType（动作集分支：Female/Male/Zombie）
    public int attackId;        // AttackId（攻击子状态选择）
    public bool inAir;          // InAir
    public bool moving;         // Moving
    public bool slide;          // Slide

    public bool trigSpawn;      // Spawn trigger（本帧被设置）
    public bool trigJump;       // Jump trigger
    public bool trigSlideEnd;   // SlideEnd trigger
    public bool trigAttack;     // Attack trigger
    public bool trigHit;        // Hit trigger
    public bool trigDie;        // Died trigger

    public static AnimParamPack Default => default;
}

/// <summary>AnimParamPack 网络序列化器（trigger 压缩为一个字节的位标志）。</summary>
public struct AnimParamPackSerializer
{
    public static bool Serialize(AnimParamPack value, byte[] result, ref int indexStart)
    {
        if (!IntSerializer.Serialize(value.characterType, result, ref indexStart)) return false;
        if (!IntSerializer.Serialize(value.attackId, result, ref indexStart)) return false;
        if (!BoolSerializer.Serialize(value.inAir, result, ref indexStart)) return false;
        if (!BoolSerializer.Serialize(value.moving, result, ref indexStart)) return false;
        if (!BoolSerializer.Serialize(value.slide, result, ref indexStart)) return false;

        byte trigBits = (byte)(
            (value.trigSpawn ? 1 : 0) |
            (value.trigJump ? 2 : 0) |
            (value.trigSlideEnd ? 4 : 0) |
            (value.trigAttack ? 8 : 0) |
            (value.trigHit ? 16 : 0) |
            (value.trigDie ? 32 : 0));
        return ByteSerializer.Serialize(trigBits, result, ref indexStart);
    }

    public static AnimParamPack Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        var pack = new AnimParamPack()
        {
            characterType = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            attackId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            inAir = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            moving = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            slide = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
        };
        byte trigBits = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
        pack.trigSpawn = (trigBits & 1) != 0;
        pack.trigJump = (trigBits & 2) != 0;
        pack.trigSlideEnd = (trigBits & 4) != 0;
        pack.trigAttack = (trigBits & 8) != 0;
        pack.trigHit = (trigBits & 16) != 0;
        pack.trigDie = (trigBits & 32) != 0;
        return pack;
    }
}
