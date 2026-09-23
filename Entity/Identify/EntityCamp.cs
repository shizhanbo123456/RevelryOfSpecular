using System;

/// <summary>
/// 阵营（[Flags]）：实体自身只占其中一位；「敌对阵营」是这些位的组合掩码，供攻击判定与索敌过滤使用。
/// 敌对关系一律由 <see cref="EntityCampUtil.HostileOf"/> 计算，不要在别处再写阵营对立判断。
/// </summary>
[Flags]
public enum EntityCamp
{
    /// <summary>无阵营（未定）。</summary>
    None = 0,
    /// <summary>进攻方。</summary>
    Attack = 1 << 0,
    /// <summary>防守方（含守护点 / 防御塔 / 僵尸）。</summary>
    Defense = 1 << 1,
    /// <summary>中立（瘟疫树）。</summary>
    Neutral = 1 << 2,
    /// <summary>可采集物件（水晶；蘑菇是它的感染形态，同一实体）。</summary>
    Prop = 1 << 3,
}

/// <summary>
/// 阵营敌对关系。
/// 规则：敌人 = 除自身阵营外的全部阵营；若自身是中立或 Prop，再从结果中剔除中立与 Prop
/// （即中立与 Prop 互不敌对，且中立不会去打 Prop）。攻击判定、索敌、范围效果统一走这里。
/// </summary>
public static class EntityCampUtil
{
    /// <summary>全部阵营位。</summary>
    public const EntityCamp All = EntityCamp.Attack | EntityCamp.Defense | EntityCamp.Neutral | EntityCamp.Prop;

    /// <summary>非参战阵营（中立 + 可采集物件）。</summary>
    public const EntityCamp NonCombat = EntityCamp.Neutral | EntityCamp.Prop;

    /// <summary>阵营的敌对阵营掩码（可含多位）。</summary>
    public static EntityCamp HostileOf(EntityCamp camp)
    {
        if (camp == EntityCamp.None) return EntityCamp.None;
        EntityCamp hostile = All & ~camp;
        if ((camp & NonCombat) != 0) hostile &= ~NonCombat;
        return hostile;
    }

    /// <summary>目标阵营是否属于该阵营的敌对阵营（取代「非同阵营即命中」，避免误伤友方）。</summary>
    public static bool IsHostile(EntityCamp camp, EntityCamp target) => (HostileOf(camp) & target) != 0;
}
