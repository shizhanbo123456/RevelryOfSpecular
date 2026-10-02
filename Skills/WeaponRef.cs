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
}
