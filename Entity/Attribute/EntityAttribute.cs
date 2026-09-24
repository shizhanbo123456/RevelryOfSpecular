/// <summary>
/// 实体属性（V0.8）。
/// 属性清单见策划案 10.1：生命值 / 力量（近战伤害）/ 魔法（远程伤害）/ 暴击率 / 暴击伤害 / 被击飞抗性 / 可见距离 / 武器槽位数量。
/// **本类只承载"属性"，不含运行时状态**：这里的 health 是**生命值上限**（配置项），
/// **当前生命值由 EntityData.currentHealth 单独承担** —— 它是随受击随时变化的运行时状态，
/// 不属于属性配置，因此配置 SO 里只会出现一个"生命值"项。
/// 全局参数被动（复活速度、僵尸刷新等级等）开战一次性计算，不进本类。
/// </summary>
[System.Serializable]
public class EntityAttribute
{
    /// <summary>生命值上限（配置项）。当前生命值见 EntityData.currentHealth。</summary>
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

    /// <summary>按字段应用增量（用于升级成长、愈战愈勇叠层等）。**只改属性本身，不碰当前生命值。**</summary>
    public void ApplyDelta(EntityAttributeDelta delta)
    {
        switch (delta.field)
        {
            case EntityAttributeDelta.Field.Health: health += delta.value; break; // 生命值上限
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
        /// <summary>生命值上限（注意：不是当前生命值）。</summary>
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
