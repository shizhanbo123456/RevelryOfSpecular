using System;

/// <summary>
/// 实体类型：类别 + 编号。（值语义，可网络传输，配套 EntityTypeSerializer 同文件）
/// </summary>
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
    public static EntityType Beacon(int value) => new EntityType(EntityCategory.Beacon, value);
    public static EntityType Crystal(int value) => new EntityType(EntityCategory.Crystal, value);
    public static EntityType Tower(int value) => new EntityType(EntityCategory.Tower, value);
    public static EntityType PlagueTree(int value) => new EntityType(EntityCategory.PlagueTree, value);
    public static EntityType Mushroom(int value) => new EntityType(EntityCategory.Mushroom, value);
    public static EntityType Prop(int value) => new EntityType(EntityCategory.Prop, value);
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

    // 守护点：0/1/2 外围，3 中心
    public static EntityType OuterBeacon0 => Beacon(0);
    public static EntityType OuterBeacon1 => Beacon(1);
    public static EntityType OuterBeacon2 => Beacon(2);
    public static EntityType CoreBeacon => Beacon(Config.outer_beacon_count);

    // 水晶（value = 类型 0~3，对应刀/长枪/枪械/魔法球 4 类武器，见第七章）
    public static EntityType Crystal0 => Crystal(0);
    public static EntityType CrystalFirst => Crystal(0);
    public static EntityType CrystalLast => Crystal(Config.crystal_type_count - 1);

    // 防御塔（0~3）
    public static EntityType Tower0 => Tower(0);
    public static EntityType TowerFirst => Tower(0);
    public static EntityType TowerLast => Tower(Config.tower_count - 1);

    // 瘟疫树
    public static EntityType PlagueTree0 => PlagueTree(0);

    // 普通僵尸（0~20，暂用 21 种）
    public static EntityType ZombieFirst => Zombie(0);
    public static EntityType ZombieLast => Zombie(20);

    // 精英僵尸（0~13，14 种）
    public static EntityType EliteZombieFirst => EliteZombie(0);
    public static EntityType EliteZombieLast => EliteZombie(13);

    // 感染蘑菇（value = 被感染水晶的类型 0~3；外观多种由客户端随机选用）
    public static EntityType Mushroom0 => Mushroom(0);
    #endregion

    #region common class func
    public bool Equals(EntityType other) => category == other.category && value == other.value;

    public override bool Equals(object obj) => obj is EntityType other && Equals(other);

    public override int GetHashCode() => ((int)category * 397) ^ value;

    public override string ToString() => $"{category}{value}";

    public static bool operator ==(EntityType left, EntityType right) => left.Equals(right);

    public static bool operator !=(EntityType left, EntityType right) => !left.Equals(right);

    /// <summary>该实体类型对应的默认阵营（守护点/防御塔/蘑菇/普通僵尸/精英僵尸归防守，瘟疫树中立；策划案第九章：僵尸本体归防守方阵营）。</summary>
    public EntityCamp DefaultCamp()
    {
        switch (category)
        {
            case EntityCategory.Character_Attack: return EntityCamp.Attack;
            case EntityCategory.Character_Defense:
            case EntityCategory.Beacon:
            case EntityCategory.Tower:
            case EntityCategory.Mushroom:
            case EntityCategory.Zombie:
            case EntityCategory.EliteZombie:
                return EntityCamp.Defense;
            case EntityCategory.PlagueTree:
                return EntityCamp.Neutral;
            default:
                return EntityCamp.Neutral;
        }
    }
    #endregion
}

/// <summary>
/// EntityType 网络序列化器（EnsNetcode 要求，同文件）。
/// </summary>
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
