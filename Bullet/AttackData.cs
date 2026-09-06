using System;
using static EntityEffectController;

/// <summary>
/// 攻击数据结构：一次攻击的全部伤害相关信息，近战与子弹共用。
/// 子弹（Bullet）持有本结构；近战判定（策划案：攻击统一由子弹处理）同样通过它走同一条伤害计算链路。
/// </summary>
public class AttackData
{
    /// <summary>攻击者实体 id。</summary>
    public ushort shooter;
    /// <summary>伤害倍率。</summary>
    public float rate;
    /// <summary>命中判定半径。</summary>
    public float radius;
    /// <summary>破霸体：命中时可让霸体等级 1（Common）的目标进入受击状态（见策划案 12.1）。</summary>
    public bool breakEndure;
    /// <summary>目标伤害接口（可命中目标筛选与伤害窗口）。</summary>
    public Damageable.IDamageable damageable;
    /// <summary>命中附加效果（如给目标添加 Buff）。</summary>
    public Action<Action<EffectType, int, float>> addEffectEvent;
    /// <summary>伤害计算属性快照（攻击者命中时的运行时属性）。</summary>
    public EntityAttribute attribute;

    /// <summary>由施放者便捷构建（属性快照取施放者当前运行时属性）。</summary>
    public static AttackData Create(EntityData shooter, float rate, float radius, bool breakEndure,
        Damageable.IDamageable damageable = null, Action<Action<EffectType, int, float>> addEffectEvent = null)
    {
        return new AttackData()
        {
            shooter = shooter != null ? shooter.id : (ushort)0,
            rate = rate,
            radius = radius,
            breakEndure = breakEndure,
            damageable = damageable,
            addEffectEvent = addEffectEvent,
            attribute = shooter != null ? shooter.floatingAttribute : null,
        };
    }
}
