[System.Serializable]
public class EntityAttribute
{
    public float health = 1000f;
    public int strength = 100;
    public int magic = 100;
    public int critRate = 5;
    public float critDamage = 1.5f;
    public float knockbackResistance = 0f;
    public float viewDistance = 80f;
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

[System.Serializable]
public class EntityAttributeDelta
{
    public enum Field
    {
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
