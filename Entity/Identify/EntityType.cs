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

    public static EntityType Character(int value) => new EntityType(EntityCategory.Character, value);
    public static EntityType Npc(int value) => new EntityType(EntityCategory.NPC, value);
    public static EntityType Plant(int value) => new EntityType(EntityCategory.Plant, value);

    public static EntityType InfectedPlant => new EntityType(EntityCategory.InfectedPlant, 0);
    public static EntityType Ore(int value) => new EntityType(EntityCategory.Ore, value);
    public static EntityType InfectedOre => new EntityType(EntityCategory.InfectedOre, 0);
    public static EntityType Infection(int value) => new EntityType(EntityCategory.Infection, value);

    #region//common class func
    public bool Equals(EntityType other) => category == other.category && value == other.value;

    public override bool Equals(object obj) => obj is EntityType other && Equals(other);

    public override int GetHashCode() => ((int)category * 397) ^ value;

    public override string ToString() => $"{category}{value}";

    public static bool operator ==(EntityType left, EntityType right) => left.Equals(right);

    public static bool operator !=(EntityType left, EntityType right) => !left.Equals(right);
    #endregion

    #region//presets
    public static EntityType Char0 => Character(0);
    public static EntityType Char1 => Character(1);
    public static EntityType Char2 => Character(2);
    public static EntityType Char3 => Character(3);
    public static EntityType Char4 => Character(4);
    public static EntityType Char5 => Character(5);
    public static EntityType Char6 => Character(6);
    public static EntityType Char7 => Character(7);
    public static EntityType Char8 => Character(8);
    public static EntityType Char9 => Character(9);
    public static EntityType Char10 => Character(10);
    public static EntityType Char11 => Character(11);
    public static EntityType Char12 => Character(12);
    public static EntityType Char13 => Character(13);

    public static EntityType InfectionTree => Infection(0);
    public static EntityType InfectionBeacon0 => Infection(1);
    public static EntityType InfectionBeacon1 => Infection(2);
    public static EntityType InfectionCore0 => Infection(3);
    public static EntityType InfectionCore1 => Infection(4);
    public static EntityType InfectionCore2 => Infection(5);
    public static EntityType InfectionCore3 => Infection(6);


    public static EntityType CharacterFirst => Char0;
    public static EntityType CharacterLast => Char13;

    public static EntityType NpcFirst => Npc(0);
    public static EntityType NpcLast => Npc(Config.npc_count - 1);

    public static EntityType PlantFirst => Plant(0);
    public static EntityType PlantLast => Plant(Config.plant_count - 1);

    public static EntityType OreFirst => Ore(0);
    public static EntityType OreLast => Ore(Config.ore_count - 1);

    public static EntityType InfectionFirst => InfectionTree;
    public static EntityType InfectionLast => Infection(Config.infection_count - 1);
    #endregion
}