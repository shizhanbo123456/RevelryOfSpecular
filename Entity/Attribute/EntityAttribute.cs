/// <summary>
/// 实体属性（V0.8）。双轨属性的载体：`EntityData.baseAttribute`（基础属性，实体生成后不再变化）
/// 与 `EntityData.floatingAttribute`（运行时属性 = 基础 + Buff 叠加，会变化）。
/// 属性清单见策划案 10.1：生命值 / 力量（近战伤害）/ 魔法（远程伤害）/ 暴击率 / 暴击伤害 / 被击飞抗性 / 可见距离 / 武器槽位数量。
/// **`health` 一个字段承载两种语义**：
/// 在 `baseAttribute` 里 = **生命值上限**（配置项，所以配置 SO 里只会出现一个"生命值"项）；
/// 在 `floatingAttribute` 里 = **当前生命值**（会变的运行时值）。
/// → **读上限一律读 `baseAttribute.health`，读当前生命一律读 `floatingAttribute.health`，不要混用。**
/// 全局参数被动（复活速度、僵尸刷新等级等）开战一次性计算，不进本类。
/// </summary>
[System.Serializable]
public class EntityAttribute
{
    /// <summary>生命值：base 侧 = 生命值上限（配置项）；floating 侧 = 当前生命值。</summary>
    public float health = 1000f;
    /// <summary>力量：近战物理伤害。</summary>
    public int strength = 100;
    /// <summary>魔法：所有远程攻击与投射物伤害。</summary>
    public int magic = 100;
    /// <summary>暴击率（0~100，力量/魔法共用）。</summary>
    public int critRate = 5;
    /// <summary>暴击伤害倍率（1.5 = 150%）。</summary>
    public float critDamage = 1.5f;
    /// <summary>被击飞抗性（与攻击力度同量纲；命中时 击飞速度 = 力度 − 本值）。</summary>
    public float knockbackResistance = 0f;
    /// <summary>可见距离（米）。</summary>
    public float viewDistance = 20f;
    /// <summary>武器槽位数量（技能列表可容纳武器数，双方角色均为此属性）。</summary>
    public int weaponSlotCount = 3;

    /// <summary>整表拷贝。注意 `health` 的语义随去向翻转：base 的上限拷进 floating 即成为当前生命值
    /// （因此生成时克隆出来就是满血）。</summary>
    public EntityAttribute Clone()
    {
        return new EntityAttribute()
        {
            health = health,
            strength = strength,
            magic = magic,
            critRate = critRate,
            critDamage = critDamage,
            knockbackResistance = knockbackResistance,
            viewDistance = viewDistance,
            weaponSlotCount = weaponSlotCount,
        };
    }

    /// <summary>按字段应用增量（用于升级成长、愈战愈勇叠层等）。由调用方施加在"基础属性的克隆"上，不直接改实体。</summary>
    public void ApplyDelta(EntityAttributeDelta delta)
    {
        switch (delta.field)
        {
            case EntityAttributeDelta.Field.Health: health += delta.value; break; // 此刻 health 还是"上限"语义
            case EntityAttributeDelta.Field.Strength: strength += (int)delta.value; break;
            case EntityAttributeDelta.Field.Magic: magic += (int)delta.value; break;
            case EntityAttributeDelta.Field.CritRate: critRate += (int)delta.value; break;
            case EntityAttributeDelta.Field.CritDamage: critDamage += delta.value; break;
            case EntityAttributeDelta.Field.KnockbackResistance: knockbackResistance += delta.value; break;
            case EntityAttributeDelta.Field.ViewDistance: viewDistance += delta.value; break;
            case EntityAttributeDelta.Field.WeaponSlotCount: weaponSlotCount += (int)delta.value; break;
        }
    }
}

/// <summary>
/// 属性增量描述（用于升级成长路线与 Buff 属性调整）。
/// </summary>
[System.Serializable]
public class EntityAttributeDelta
{
    public enum Field
    {
        /// <summary>生命值上限（不是当前生命值）。**当前不可达**：`EffectType` 里没有生命类，
        /// 且它加到的克隆会被重算末尾的夹取覆盖 —— 真要做"生命上限 ±X"的 Buff，须先改重算逻辑。</summary>
        Health,
        Strength, Magic, CritRate, CritDamage,
        KnockbackResistance, ViewDistance, WeaponSlotCount,
    }

    public Field field;
    public float value;

    public EntityAttributeDelta() { }

    public EntityAttributeDelta(Field field, float value)
    {
        this.field = field;
        this.value = value;
    }
}
