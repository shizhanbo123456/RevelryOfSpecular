using UnityEngine;

public static class EntityTypeExt
{
    public static bool IsCharacter(this EntityType type) => type.category == EntityCategory.Character;
    public static bool IsNpc(this EntityType type) => type.category == EntityCategory.NPC;
    public static bool IsPlant(this EntityType type) => type.category == EntityCategory.Plant;
    public static bool IsInfectedPlant(this EntityType type) => type.category == EntityCategory.InfectedPlant;
    public static bool IsOre(this EntityType type) => type.category == EntityCategory.Ore;
    public static bool IsInfectedOre(this EntityType type) => type.category == EntityCategory.InfectedOre;
    public static bool IsInfection(this EntityType type) => type.category == EntityCategory.Infection;
    public static bool AnyInfected(this EntityType type) => type.IsInfection() || type.IsInfectedPlant() || type.IsInfectedOre();
    public static bool AnyPlant(this EntityType type) => type.IsInfectedPlant() || type.IsPlant();
    public static bool AnyOre(this EntityType type) => type.IsInfectedOre() || type.IsOre();

    public static EntityType RandomNpc => EntityType.Npc(Random.Range(0, Config.npc_count));
    public static EntityType RandomPlant => EntityType.Plant(Random.Range(0, Config.plant_count));
    public static EntityType RandomOre => EntityType.Ore(Random.Range(0, Config.ore_count));
    public static EntityType RandomInfection => EntityType.Infection(Random.Range(0, Config.infection_count));
}
