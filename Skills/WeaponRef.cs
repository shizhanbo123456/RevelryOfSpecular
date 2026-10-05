public enum WeaponCategory
{
    None = 0,
    Knife = 1,
    Spear = 2,
    Gun = 3,
    MagicOrb = 4,
}

public struct WeaponRef
{
    public WeaponCategory category;
    public int index;

    public WeaponRef(WeaponCategory category, int index)
    {
        this.category = category;
        this.index = index;
    }

    public bool IsValid => category != WeaponCategory.None && index >= 0;

    public static WeaponRef None => new WeaponRef(WeaponCategory.None, -1);

    public override string ToString() => IsValid ? $"{category}[{index}]" : "None";

    public override bool Equals(object obj) => obj is WeaponRef other && other.category == category && other.index == index;

    public override int GetHashCode() => ((int)category * 397) ^ index;

    public static bool operator ==(WeaponRef left, WeaponRef right) => left.Equals(right);

    public static bool operator !=(WeaponRef left, WeaponRef right) => !left.Equals(right);

    // 与全局武器 id（Config.weapon_id_* 区段）互转；区段外直接抛错暴露，不做静默兜底
    public static implicit operator int(WeaponRef weapon)
    {
        switch (weapon.category)
        {
            case WeaponCategory.None: return -1;
            case WeaponCategory.Knife: return Config.weapon_id_melee_min + weapon.index;
            case WeaponCategory.Spear: return Config.weapon_id_spear_min + weapon.index;
            case WeaponCategory.Gun: return Config.weapon_id_gun_min + weapon.index;
            case WeaponCategory.MagicOrb: return Config.weapon_id_magic_min + weapon.index;
            default: throw new System.ArgumentOutOfRangeException(nameof(weapon), weapon.category, "未知武器类别");
        }
    }

    public static implicit operator WeaponRef(int id)
    {
        if (id < 0) return WeaponRef.None;
        if (id >= Config.weapon_id_melee_min && id <= Config.weapon_id_melee_max)
            return new WeaponRef(WeaponCategory.Knife, id - Config.weapon_id_melee_min);
        if (id >= Config.weapon_id_spear_min && id <= Config.weapon_id_spear_max)
            return new WeaponRef(WeaponCategory.Spear, id - Config.weapon_id_spear_min);
        if (id >= Config.weapon_id_gun_min && id <= Config.weapon_id_gun_max)
            return new WeaponRef(WeaponCategory.Gun, id - Config.weapon_id_gun_min);
        if (id >= Config.weapon_id_magic_min && id <= Config.weapon_id_magic_max)
            return new WeaponRef(WeaponCategory.MagicOrb, id - Config.weapon_id_magic_min);
        throw new System.ArgumentOutOfRangeException(nameof(id), id, "超出全部武器 id 区段");
    }
}
