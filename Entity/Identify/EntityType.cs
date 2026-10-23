using System;

[Serializable]
public struct EntityType : IEquatable<EntityType>
{
    public EntityCategory category;
    public int value;

    public EntityType(EntityCategory category, int value)
    {
        this.category = category;
        this.value = value;
    }

    #region 构造器
    public static EntityType Attack(int value) => new EntityType(EntityCategory.Character_Attack, value);
    public static EntityType Defense(int value) => new EntityType(EntityCategory.Character_Defense, value);
    public static EntityType Zombie(int value) => new EntityType(EntityCategory.Zombie, value);
    public static EntityType EliteZombie(int value) => new EntityType(EntityCategory.EliteZombie, value);
    public static EntityType GuardStatue(int value) => new EntityType(EntityCategory.GuardStatue, value);
    public static EntityType Coral(int size) => new EntityType(EntityCategory.Coral, size);
    public static EntityType CrystalStatue(int value) => new EntityType(EntityCategory.CrystalStatue, value);
    public static EntityType NeutralStatue(int value) => new EntityType(EntityCategory.NeutralStatue, value);
    #endregion

    #region 预设
    // 进攻方角色（18 人，具体角色池待定）
    public static EntityType Attack0 => Attack(0);
    public static EntityType Attack1 => Attack(1);
    public static EntityType Attack2 => Attack(2);
    public static EntityType Attack3 => Attack(3);
    public static EntityType Attack4 => Attack(4);
    public static EntityType Attack5 => Attack(5);
    public static EntityType Attack6 => Attack(6);
    public static EntityType Attack7 => Attack(7);
    public static EntityType Attack8 => Attack(8);
    public static EntityType Attack9 => Attack(9);
    public static EntityType Attack10 => Attack(10);
    public static EntityType Attack11 => Attack(11);
    public static EntityType Attack12 => Attack(12);
    public static EntityType Attack13 => Attack(13);
    public static EntityType Attack14 => Attack(14);
    public static EntityType Attack15 => Attack(15);
    public static EntityType Attack16 => Attack(16);
    public static EntityType Attack17 => Attack(17);
    public static EntityType AttackFirst => Attack0;
    public static EntityType AttackLast => Attack(Config.attack_character_count - 1);

    // 防守方角色（6 人，基于实际模型资源）
    public static EntityType Defense0 => Defense(0);
    public static EntityType Defense1 => Defense(1);
    public static EntityType Defense2 => Defense(2);
    public static EntityType Defense3 => Defense(3);
    public static EntityType Defense4 => Defense(4);
    public static EntityType Defense5 => Defense(5);
    public static EntityType DefenseFirst => Defense0;
    public static EntityType DefenseLast => Defense(Config.defense_character_count - 1);

    // 守护雕像：0/1/2 贤者雕像（外围），3 国王雕像（中心）
    public static EntityType SageStatue0 => GuardStatue(0);
    public static EntityType SageStatue1 => GuardStatue(1);
    public static EntityType SageStatue2 => GuardStatue(2);
    public static EntityType KingStatue => GuardStatue(Config.sage_statue_count);

    // 珊瑚（value = 尺寸：0 小 / 1 大；同尺寸共用一套属性、外观列表与掉落率）
    public static EntityType CoralSmall => Coral(0);
    public static EntityType CoralLarge => Coral(1);

    // 水晶雕像（0~2）
    public static EntityType CrystalStatue0 => CrystalStatue(0);
    public static EntityType CrystalStatueFirst => CrystalStatue(0);
    public static EntityType CrystalStatueLast => CrystalStatue(Config.crystal_statue_count - 1);

    // 中立雕像：0 天使雕像 / 1 恶魔雕像（行为一致，仅外观不同）
    public static EntityType AngelStatue => NeutralStatue(0);
    public static EntityType DemonStatue => NeutralStatue(1);

    // 普通僵尸（0~20，暂用 21 种）
    public static EntityType ZombieFirst => Zombie(0);
    public static EntityType ZombieLast => Zombie(Config.zombie_variant_count - 1);

    // 精英僵尸（14 种，type.value 0~13；种类数取 Config 常量，避免与召唤逻辑脱节）
    public static EntityType EliteZombieFirst => EliteZombie(0);
    public static EntityType EliteZombieLast => EliteZombie(Config.elite_zombie_variant_count - 1);
    #endregion

    #region common class func
    public bool Equals(EntityType other) => category == other.category && value == other.value;

    public override bool Equals(object obj) => obj is EntityType other && Equals(other);

    public override int GetHashCode() => ((int)category * 397) ^ value;

    public override string ToString() => $"{category}{value}";

    public static bool operator ==(EntityType left, EntityType right) => left.Equals(right);

    public static bool operator !=(EntityType left, EntityType right) => !left.Equals(right);
    #endregion
}

public struct EntityTypeSerializer
{
    public static bool Serialize(EntityType value, byte[] result, ref int indexStart)
    {
        if (!IntSerializer.Serialize((int)value.category, result, ref indexStart)) return false;
        return IntSerializer.Serialize(value.value, result, ref indexStart);
    }

    public static EntityType Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        var category = (EntityCategory)IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
        int value = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
        return new EntityType(category, value);
    }
}
