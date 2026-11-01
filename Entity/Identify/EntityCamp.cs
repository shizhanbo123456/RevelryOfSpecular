using System;

[Flags]
public enum EntityCamp
{
    None = 0,
    Attack = 1 << 0,
    Defense = 1 << 1,
    Neutral = 1 << 2,
}

public static class EntityCampUtil
{
    // 敌对集合由实体类别决定：camp 只表达阵营归属，行为差异（僵尸/水晶雕像只打进攻方、珊瑚不主动索敌）走类别
    public static EntityCamp HostileOf(EntityCategory category) => category switch
    {
        EntityCategory.Character_Attack => EntityCamp.Defense | EntityCamp.Neutral,
        EntityCategory.Character_Defense => EntityCamp.Attack | EntityCamp.Neutral,
        EntityCategory.GuardStatue => EntityCamp.Attack | EntityCamp.Neutral,          // 防守方设施：自身没有索敌 AI
        EntityCategory.NeutralStatue => EntityCamp.Attack | EntityCamp.Defense,        // 中立争抢单位：谁靠近打谁
        EntityCategory.Coral => EntityCamp.Attack | EntityCamp.Defense,                // 纯物件，本身不索敌
        EntityCategory.Zombie or EntityCategory.EliteZombie or EntityCategory.CrystalStatue => EntityCamp.Attack,
        _ => throw new System.ArgumentException($"未定义敌对关系的实体类别：{category}", nameof(category)),
    };

    public static bool IsHostile(EntityCategory category, EntityCamp target) => (HostileOf(category) & target) != 0;
}
