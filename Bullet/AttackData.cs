using System;
using static EntityEffectController;

/// <summary>
/// 攻击数据结构：一次攻击的全部伤害相关信息，近战与子弹共用。
/// 子弹（Bullet）持有本结构；近战判定同样通过它走同一条伤害计算链路。
/// 伤害公式（策划案 14 章）：damage = rate × (力量 或 魔法)，施放瞬间结算并快照
/// （暴击在命中时按攻击者暴击率/暴击伤害掷出，见 GetDamage）。
/// </summary>
public class AttackData
{
    /// <summary>攻击者实体 id。</summary>
    public ushort shooter;
    /// <summary>攻击者阵营（生成时快照，命中筛选用：不打同阵营）。</summary>
    public EntityCamp shooterCamp;
    /// <summary>伤害倍率。</summary>
    public float rate;
    /// <summary>命中判定半径。</summary>
    public float radius;
    /// <summary>是否魔法伤害（true=×魔法，远程/法术；false=×力量，近战）。</summary>
    public bool useMagic;
    /// <summary>破霸体：命中时可让霸体等级 1（Common）的目标进入受击状态（见策划案 12.1）。</summary>
    public bool breakEndure;
    /// <summary>目标伤害接口（可命中目标筛选与伤害窗口）。</summary>
    public Damageable.IDamageable damageable;
    /// <summary>命中附加效果（如给目标添加 Buff）。</summary>
    public Action<Action<EffectType, int, float>> addEffectEvent;
    /// <summary>伤害计算属性快照（攻击者命中时的运行时属性）。</summary>
    public EntityAttribute attribute;
    /// <summary>武器经验点数（伤害 = 基础 × (1 + 10% × 经验)，策划案 14 章）。</summary>
    public int weaponExp;

    /// <summary>由施放者便捷构建（属性快照取施放者当前运行时属性；武器技能传该武器经验点数）。</summary>
    public static AttackData Create(EntityData shooter, float rate, float radius, bool breakEndure,
        bool useMagic = false, Damageable.IDamageable damageable = null,
        Action<Action<EffectType, int, float>> addEffectEvent = null, int weaponExp = 0)
    {
        return new AttackData()
        {
            shooter = shooter != null ? shooter.id : (ushort)0,
            shooterCamp = shooter != null ? shooter.camp : EntityCamp.Neutral,
            rate = rate,
            radius = radius,
            useMagic = useMagic,
            breakEndure = breakEndure,
            damageable = damageable,
            addEffectEvent = addEffectEvent,
            attribute = shooter != null ? shooter.floatingAttribute : null,
            weaponExp = weaponExp,
        };
    }

    /// <summary>
    /// 结算本次攻击的最终伤害：基础 = rate × (魔法或力量) × (1 + 10% × 武器经验)，
    /// 按攻击者暴击率掷暴击（× 暴击伤害倍率）。出伤乘区（愈战愈勇等）在目标侧管线统一应用。
    /// </summary>
    public float GetDamage()
    {
        if (attribute == null) return 0f;
        float final = rate * (useMagic ? attribute.magic : attribute.strength);
        final *= 1f + Config.skill_exp_damage_bonus * weaponExp; // 武器经验加伤（策划案 14 章）
        if (attribute.critRate > 0f && UnityEngine.Random.Range(0f, 100f) < attribute.critRate)
        {
            final *= attribute.critDamage; // 暴击伤害为倍率（默认 1.5 = 150%）
        }
        return final;
    }
}
