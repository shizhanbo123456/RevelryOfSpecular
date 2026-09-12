/// <summary>
/// 武器类别（与 AssetsManager 的 4 个武器模型列表一一对应）。
/// </summary>
public enum WeaponCategory
{
    /// <summary>无武器（空手）。</summary>
    None = 0,
    /// <summary>近战刀 → AssetsManager.MeleeWeaponPrefabs。</summary>
    Knife = 1,
    /// <summary>长枪 → AssetsManager.SpearWeaponPrefabs。</summary>
    Spear = 2,
    /// <summary>枪械 → AssetsManager.GunWeaponPrefabs。</summary>
    Gun = 3,
    /// <summary>魔法球 → AssetsManager.MagicOrbPrefabs。</summary>
    MagicOrb = 4,
}

/// <summary>
/// 技能对应的武器引用：<b>武器类别 + 在该类别列表中的下标</b>。
/// 由技能通过 <c>SkillBase.Weapon</c> 声明（悬浮武器模型，客户端表现用；服务器模板无图形）。
/// 之所以不用"技能 id 直接当预制体下标"：两者是独立的编号体系，靠 id 推算会随资源增减而错位。
/// </summary>
public struct WeaponRef
{
    /// <summary>武器类别。</summary>
    public WeaponCategory category;
    /// <summary>在该类别列表中的下标（0 起）。</summary>
    public int index;

    public WeaponRef(WeaponCategory category, int index)
    {
        this.category = category;
        this.index = index;
    }

    /// <summary>是否是一把有效武器（= 该技能是否有悬浮武器显示）。</summary>
    public bool IsValid => category != WeaponCategory.None && index >= 0;

    /// <summary>无武器（空手）。</summary>
    public static WeaponRef None => new WeaponRef(WeaponCategory.None, -1);

    public override string ToString() => IsValid ? $"{category}[{index}]" : "None";

    public override bool Equals(object obj) => obj is WeaponRef other && other.category == category && other.index == index;

    public override int GetHashCode() => ((int)category * 397) ^ index;

    public static bool operator ==(WeaponRef left, WeaponRef right) => left.Equals(right);

    public static bool operator !=(WeaponRef left, WeaponRef right) => !left.Equals(right);
}
