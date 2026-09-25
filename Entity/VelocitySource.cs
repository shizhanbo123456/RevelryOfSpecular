/// <summary>
/// 速度来源（三类写入者）：EntityData 的速度写入必须携带，内部按来源分桶存储并统一混合。
/// 后续按来源做规则判断（如 Motion 锁输入期间阻断 Animation 来源的速度）落在本枚举的分支上。
/// </summary>
public enum VelocitySource
{
    /// <summary>动画声明（EntityAnim 统一接口写入；玩家主动操控速度的唯一来源）。</summary>
    Animation = 0,
    /// <summary>MotionBase 位移效果（由 EntityData.TickVelocity 每帧取用；有位移时无视摩擦，不吃速度系数）。</summary>
    Motion = 1,
    /// <summary>击飞（命中瞬间的一次性覆盖，写入时清除 Animation 来源的声明与保留值）。</summary>
    Knockback = 2,
}
