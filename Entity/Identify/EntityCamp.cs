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
    DefenseTower = 1 << 5, // 防御塔（水晶雕像）：防守方设施，只敌视进攻方
}

public static class EntityCampUtil
{
    public const EntityCamp All =
        EntityCamp.Attack | EntityCamp.Defense | EntityCamp.Neutral | EntityCamp.Prop | EntityCamp.Zombie | EntityCamp.DefenseTower;

    // 敌对集合由实体类别决定：camp 只表达阵营归属，行为差异（僵尸/水晶雕像只打进攻方、珊瑚不主动索敌）走类别
    public static EntityCamp HostileOf(EntityCategory category) => category switch
    {
        EntityCategory.Character_Attack => All & ~EntityCamp.Attack,
        EntityCategory.Character_Defense => EntityCamp.Attack | EntityCamp.Neutral | EntityCamp.Prop,
        EntityCategory.GuardStatue => EntityCamp.Attack | EntityCamp.Neutral | EntityCamp.Prop, // 防守方设施：自身没有索敌 AI
        EntityCategory.NeutralStatue => EntityCamp.Attack | EntityCamp.Defense,                 // 中立争抢单位：谁靠近打谁
        EntityCategory.Coral => EntityCamp.Attack | EntityCamp.Defense,                         // 纯物件，本身不索敌
        EntityCategory.Zombie or EntityCategory.EliteZombie or EntityCategory.CrystalStatue => EntityCamp.Attack,
        _ => throw new System.ArgumentException($"未定义敌对关系的实体类别：{category}", nameof(category)),
    };

    public static bool IsHostile(EntityCategory category, EntityCamp target) => (HostileOf(category) & target) != 0;

    // 显示归属：僵尸与防御塔同属防守方阵营，头顶血条的阵营图标与敌我判定都按防守方处理
    public static EntityCamp DisplayCamp(EntityCamp camp) =>
        (camp & (EntityCamp.Zombie | EntityCamp.DefenseTower)) != 0 ? EntityCamp.Defense : camp;
}
