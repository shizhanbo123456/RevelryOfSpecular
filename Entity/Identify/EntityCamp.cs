using System;

[Flags]
public enum EntityCamp
{
    None = 0,
    Attack = 1 << 0,
    Defense = 1 << 1,
    Neutral = 1 << 2,
    Prop = 1 << 3,
    Zombie = 1 << 4,
}

public static class EntityCampUtil
{
    public const EntityCamp All =
        EntityCamp.Attack | EntityCamp.Defense | EntityCamp.Neutral | EntityCamp.Prop | EntityCamp.Zombie;

    public const EntityCamp NonCombat = EntityCamp.Neutral | EntityCamp.Prop;

    public static EntityCamp HostileOf(EntityCamp camp)
    {
        if (camp == EntityCamp.None) return EntityCamp.None;
        if ((camp & EntityCamp.Zombie) != 0) return EntityCamp.Attack; // 僵尸：只敌视进攻方
        EntityCamp hostile = All & ~camp;
        // 僵尸只与进攻方互为敌对，其余阵营一律不视僵尸为敌（否则防守方会开始打自家僵尸）
        if ((camp & EntityCamp.Attack) == 0) hostile &= ~EntityCamp.Zombie;
        if ((camp & NonCombat) != 0) hostile &= ~NonCombat;
        return hostile;
    }

    public static bool IsHostile(EntityCamp camp, EntityCamp target) => (HostileOf(camp) & target) != 0;
}
