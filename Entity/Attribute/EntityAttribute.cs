using System.Collections.Generic;

/// <summary>
/// 运行时角色/实体属性（V0.8）。
/// 属性清单见策划案 8.2：生命 / 力量（近战伤害）/ 魔法（远程伤害）/ 移速 / 暴击率 / 暴击伤害 / 击退抗性 / 可见距离 / 武器槽位数量。
/// 全局参数被动（复活速度、僵尸刷新等级等）开战一次性计算，不进本类。
/// </summary>
[System.Serializable]
public class EntityAttribute
{
    /// <summary>当前生命值。</summary>
    public float health = 1000f;
    /// <summary>最大生命值。</summary>
    public float maxHealth = 1000f;
    /// <summary>力量：近战物理伤害。</summary>
    public int strength = 100;
    /// <summary>魔法：所有远程攻击与投射物伤害。</summary>
    public int magic = 100;
    /// <summary>移动速度（米/秒）。</summary>
    public float moveSpeed = 5f;
    /// <summary>暴击率（0~100，力量/魔法共用）。</summary>
    public int critRate = 5;
    /// <summary>暴击伤害倍率（1.5 = 150%）。</summary>
    public float critDamage = 1.5f;
    /// <summary>击退抗性（0~1，1 完全免疫击退）。</summary>
    public float knockbackResistance = 0f;
    /// <summary>可见距离（米）。</summary>
    public float viewDistance = 20f;
    /// <summary>武器槽位数量（技能列表可容纳武器数，双方角色均为此属性）。</summary>
    public int weaponSlotCount = 3;
    /// <summary>角色等级。</summary>
    public int level = 1;

    /// <summary>是否存活。</summary>
    public bool Alive => health > 0f;

    public EntityAttribute Clone()
    {
        return new EntityAttribute()
        {
            health = health,
            maxHealth = maxHealth,
            strength = strength,
            magic = magic,
            moveSpeed = moveSpeed,
            critRate = critRate,
            critDamage = critDamage,
            knockbackResistance = knockbackResistance,
            viewDistance = viewDistance,
            weaponSlotCount = weaponSlotCount,
            level = level,
        };
    }

    /// <summary>按字段应用增量（用于升级成长、愈战愈勇叠层等）。</summary>
    public void ApplyDelta(EntityAttributeDelta delta)
    {
        switch (delta.field)
        {
            case EntityAttributeDelta.Field.Health: maxHealth += delta.value; break;
            case EntityAttributeDelta.Field.Strength: strength += (int)delta.value; break;
            case EntityAttributeDelta.Field.Magic: magic += (int)delta.value; break;
            case EntityAttributeDelta.Field.MoveSpeed: moveSpeed += delta.value; break;
            case EntityAttributeDelta.Field.CritRate: critRate += (int)delta.value; break;
            case EntityAttributeDelta.Field.CritDamage: critDamage += delta.value; break;
            case EntityAttributeDelta.Field.KnockbackResistance: knockbackResistance += delta.value; break;
            case EntityAttributeDelta.Field.ViewDistance: viewDistance += delta.value; break;
            case EntityAttributeDelta.Field.WeaponSlotCount: weaponSlotCount += (int)delta.value; break;
        }
        // 升级加最大生命时同步补满当前生命（复活/出生时全满，战斗中升级留空 TODO）
        if (delta.field == EntityAttributeDelta.Field.Health && health > 0f)
        {
            health = maxHealth;
        }
    }

    public List<float> GetValueList()
    {
        return new List<float>()
        {
            health, maxHealth, strength, magic, moveSpeed,
            critRate, critDamage, knockbackResistance, viewDistance, weaponSlotCount, level
        };
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
        Health, Strength, Magic, MoveSpeed, CritRate, CritDamage,
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
