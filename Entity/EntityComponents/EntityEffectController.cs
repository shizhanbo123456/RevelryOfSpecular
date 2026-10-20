using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public enum EffectType
{
    // ---- 属性修改（重算轨道，无特效；Boost = 提升 / Decrease = 削弱成对，幅度内部定死，见 RecomputeAttributes）----
    AttrStrengthBoost,          // 力量提升（激励法阵等）
    AttrStrengthDecrease,       // 力量降低
    AttrMagicBoost,             // 魔法提升
    AttrMagicDecrease,          // 魔法降低
    AttrCritRateBoost,          // 暴击率提升
    AttrCritRateDecrease,       // 暴击率降低
    AttrCritDamageBoost,        // 暴击伤害提升
    AttrCritDamageDecrease,     // 暴击伤害降低
    AttrKnockbackBoost,         // 击退抗性提升
    AttrKnockbackDecrease,      // 击退抗性降低
    AttrViewDistanceBoost,      // 可见距离提升（按 20% 规则）
    AttrViewDistanceDecrease,   // 可见距离降低
    InfiniteView,               // 无限视野（特例：固定值 Config.infinite_view_distance，不走 20% 规则）

    // ---- 控制类（强控：动画速度 0、霸体失效、打断位移与攻击）----
    Stun,               // 麻痹（鹿铠被动/麻痹弹/苍白之雷）
    Freeze,             // 冰冻（冻结弹/冰锥术/冰霜新星/苍白之冰）
    Root,               // 定身（捕获投掷/束缚钉）
    Silence,            // 沉默（教皇主动1：仅禁技能，非强控）

    // ---- DoT（固定数值，1s 间隔，吃护盾/减伤、不吃增减伤）----
    Burning,            // 燃烧（燃烧弹/苍白之火）
    Poison,             // 中毒（毒珠/瘟疫引爆，叠层）

    // ---- 护盾/减伤/反伤 ----
    Shield,             // 护盾（光盾）
    TowerShield,        // 岩石护盾（鹿铠主动1，塔身）
    BeaconReduce,       // 副守护点减伤（每存活外围点 25%，叠层）
    PopeGuard,          // 教皇守护（教皇被动，入夜守护点减伤）
    Reflect,            // 反伤（教皇大招，守护点）

    // ---- 标记（无自身效果，供读取层数/客户端表现）----
    PlagueMark,         // 瘟疫标记（叠层，引爆用）
    PaleLight,          // 苍白之光（叠层，满 10 转化冰）
    PaleDark,           // 苍白之暗（叠层，满 10 转化雷）
    EyeMark,            // 白眼标记（小地图常显）

    // ---- 视野/信息（客户端表现逻辑）----
    Fog,                // 迷雾（苍白舞者大招）
    MinimapLost,        // 小地图失联（夜间进攻方）

    // ---- 动画移速（载体 = 动画状态机移动状态播放速度，移速属性已删除，见策划案 11.3）----
    AnimSpeedUp,        // 加速（激励弹）
    AnimSlowDown,       // 减速（通用，破甲重弹）
    Mire,               // 泥沼（教皇主动2，全场敌方）

    // ---- 特殊 ----
    YzCy,               // 愈战愈勇（增伤/减伤乘区，按层数）
    TowerBlaze,         // 灵火（塔攻击附加爆炸：攻击生成时查询）
    DeathStroll,        // 死灵漫步（强制霸体 + 光环 DoT）
    MushroomInfect,     // 蘑菇感染（鹿铠主动2：水晶上的 Buff，服务器只存剩余时间；客户端按同步 Buff 显隐换模，存在时被进攻方摧毁无产出）
    PlagueBless,        // 瘟疫祝福（攻占瘟疫树奖励：+75% 出伤 / −25% 受伤，见 Config.plague_bless_*）

    // ---- GM（仅调试指令用）----
    GmSpeedUp,          // GM「极速」：动画移速倍率 = Config.gm_speed_multiplier
}

public static class EffectTypeExt
{
    public static bool IsAttribute(this EffectType type) => type switch
    {
        EffectType.AttrStrengthBoost or EffectType.AttrMagicBoost or
        EffectType.AttrCritRateBoost or EffectType.AttrCritDamageBoost or EffectType.AttrKnockbackBoost or
        EffectType.AttrViewDistanceBoost or EffectType.InfiniteView or
        EffectType.AttrStrengthDecrease or EffectType.AttrMagicDecrease or
        EffectType.AttrCritRateDecrease or EffectType.AttrCritDamageDecrease or EffectType.AttrKnockbackDecrease or
        EffectType.AttrViewDistanceDecrease => true,
        _ => false,
    };

    public static bool IsControl(this EffectType type) => type is EffectType.Stun or EffectType.Freeze or EffectType.Root;

    public static bool IsDoT(this EffectType type) => type is EffectType.Burning or EffectType.Poison;

    //固定间隔生效的类型（统一 1s 一跳，见 OnUpdate）
    public static bool IsTicking(this EffectType type) => type is EffectType.Burning or EffectType.Poison or EffectType.DeathStroll;

    public static bool IsShield(this EffectType type) => type is EffectType.Shield or EffectType.TowerShield;

    //极性固定标注：负面白名单 + 全部属性削弱；其余一律正面（新增 Buff 按实际效果维护此表）
    public static bool IsNegative(this EffectType type) => type switch
    {
        EffectType.AttrStrengthDecrease or EffectType.AttrMagicDecrease or
        EffectType.AttrCritRateDecrease or EffectType.AttrCritDamageDecrease or
        EffectType.AttrKnockbackDecrease or EffectType.AttrViewDistanceDecrease or
        EffectType.Stun or EffectType.Freeze or EffectType.Root or EffectType.Silence or
        EffectType.Burning or EffectType.Poison or
        EffectType.Mire or EffectType.AnimSlowDown or EffectType.MinimapLost or
        EffectType.PlagueMark or EffectType.PaleLight or EffectType.PaleDark or
        EffectType.EyeMark or EffectType.Fog or EffectType.MushroomInfect => true,
        _ => false,
    };
}

public class EntityEffectController
{
    private const float TickInterval = 1f; // 固定间隔生效统一 1s 一跳

    public struct EffectRuntime
    {
        public EffectType type;
        public int level;
        public float shieldValue;              // 护盾剩余值（仅护盾类）
        public ushort sourceId;                // 施加者 id（DoT 击杀归属）
        public EntityAttribute sourceAttribute; // 施加者属性快照（施加时的独立克隆，不随后续变化）
        public float endTime;                  // 生效结束时间戳（永久 = float.MaxValue）
        public float nextTickTime;             // 下次生效时刻
        public bool isNegative;
    }

    public EntityData owner;

    public void Clear()
    {
        effects.Clear();
        if (owner != null) owner.buffDirty = true;
    }

    private readonly Dictionary<EffectType, EffectRuntime> effects = new();
    private static readonly List<EffectType> s_expired = new();
    private static readonly List<EntityData> s_tickTargets = new();

    public void Init(EntityData data)
    {
        owner = data;
        effects.Clear();
    }

    #region//增删查（简易统一入口）
    public void AddEffect(EffectType type, int level = 1, float duration = -1f, EntityData source = null)
    {
        if (owner != null) owner.buffDirty = true;
        float endTime = duration < 0f ? float.MaxValue : Time.time + duration;
        if (effects.TryGetValue(type, out var rt))
        {
            // 叠层语义（11.3 特征列）：等级累加 + 时间刷新；施加者与快照以最新一次为准
            rt.level += level;
            rt.endTime = endTime;
            rt.sourceId = source != null ? source.id : (ushort)0;
            rt.sourceAttribute = source != null && source.floatingAttribute != null ? source.floatingAttribute.Clone() : null;
            if (type.IsShield()) rt.shieldValue = ShieldValueOf(rt.level);
            effects[type] = rt;
        }
        else
        {
            rt = new EffectRuntime()
            {
                type = type,
                level = level,
                shieldValue = type.IsShield() ? ShieldValueOf(level) : 0f,
                sourceId = source != null ? source.id : (ushort)0,
                sourceAttribute = source != null && source.floatingAttribute != null ? source.floatingAttribute.Clone() : null,
                endTime = endTime,
                nextTickTime = Time.time + TickInterval,
                isNegative = type.IsNegative(),
            };
            effects[type] = rt;
        }

        // 按类别分发
        if (type.IsAttribute()) RecomputeAttributes();
        else if (type.IsControl()) ApplyControl();
        else if (IsMoveSpeedEffect(type)) ApplyAnimSpeedScale();

        // 苍白之光/暗叠层后判定转化（PC103 被动：满层转冰/雷、双高转火，见 BattleManagerPassive）；转化出的控制/燃烧归属原施加者
        if (type == EffectType.PaleLight || type == EffectType.PaleDark)
        {
            if (Tool.BattleManager != null) Tool.BattleManager.CheckPaleConversion(owner, FindEntity(rt.sourceId));
        }
    }

    public void RemoveEffect(EffectType type)
    {
        if (!effects.Remove(type)) return;
        if (owner != null) owner.buffDirty = true;
        if (type.IsAttribute()) RecomputeAttributes();
        if (type.IsControl()) RefreshControlPause();
        if (IsMoveSpeedEffect(type)) ApplyAnimSpeedScale();
    }

    public void RemoveAllNegative()
    {
        s_expired.Clear();
        foreach (var pair in effects)
        {
            if (pair.Value.isNegative) s_expired.Add(pair.Key);
        }
        foreach (var type in s_expired) RemoveEffect(type);
    }

    // GM：移除全部 Buff（逐个走 RemoveEffect，保证属性/控制/移速按类重算）
    public void RemoveAll()
    {
        s_expired.Clear();
        foreach (var pair in effects) s_expired.Add(pair.Key);
        foreach (var type in s_expired) RemoveEffect(type);
    }

    public bool HasEffect(EffectType type) => effects.ContainsKey(type);
    public int GetLevel(EffectType type) => effects.TryGetValue(type, out var rt) ? rt.level : 0;
    public float GetRemainTime(EffectType type) => effects.TryGetValue(type, out var rt) ? Mathf.Max(0f, rt.endTime - Time.time) : 0f;
    #endregion

    #region//战斗管线查询（由战斗代码在明确时机调用，非每帧计算）
    public float GetShieldAbsorb()
    {
        float sum = 0f;
        if (effects.TryGetValue(EffectType.Shield, out var s)) sum += s.shieldValue;
        if (effects.TryGetValue(EffectType.TowerShield, out var t)) sum += t.shieldValue;
        return sum;
    }

    public void ConsumeShield(float amount)
    {
        foreach (var type in new[] { EffectType.Shield, EffectType.TowerShield })
        {
            if (amount <= 0f) break;
            if (!effects.TryGetValue(type, out var rt)) continue;
            float used = Mathf.Min(rt.shieldValue, amount);
            rt.shieldValue -= used;
            amount -= used;
            if (rt.shieldValue <= 0f) effects.Remove(type);
            else effects[type] = rt;
        }
    }

    public float GetDamageReduceRate()
    {
        float rate = 0f;
        if (effects.TryGetValue(EffectType.BeaconReduce, out var b)) rate += b.level * 0.25f;
        if (HasEffect(EffectType.PopeGuard)) rate += Config.pope_guard_reduce_rate;
        return Mathf.Clamp01(rate);
    }

    public float GetOutDamageMultiplier()
    {
        float m = 1f + GetLevel(EffectType.YzCy) * 0.1f;
        if (HasEffect(EffectType.PlagueBless)) m *= 1f + Config.plague_bless_damage_up;
        return m;
    }

    public float GetInDamageMultiplier()
    {
        float m = 1f / (1f + GetLevel(EffectType.YzCy) * 0.1f);
        if (HasEffect(EffectType.PlagueBless)) m *= 1f - Config.plague_bless_damage_reduce;
        return m;
    }

    // 反弹量 = 实际受到伤害 × 等级 × 系数，向下取整且至少 1（有反伤才会被调用）
    public float GetReflectDamage(float receivedDamage)
    {
        if (!effects.TryGetValue(EffectType.Reflect, out var rt)) return 0f;
        return Mathf.Max(1, Mathf.FloorToInt(receivedDamage * rt.level * Config.reflect_rate));
    }

    public bool IsActionBlocked() => HasEffect(EffectType.Stun) || HasEffect(EffectType.Freeze) || HasEffect(EffectType.Root);

    public bool IsSilenced() => HasEffect(EffectType.Silence);

    public bool CanCastSkill() => !IsActionBlocked() && !IsSilenced();

    public bool CanMove() => !IsActionBlocked();

    public bool HasSuperArmor() => HasEffect(EffectType.DeathStroll);

    public float GetMoveAnimSpeedMultiplier()
    {
        float m = 1f;
        if (HasEffect(EffectType.AnimSpeedUp)) m *= Config.anim_move_speed_up;
        if (HasEffect(EffectType.AnimSlowDown)) m *= Config.anim_move_speed_down;
        if (HasEffect(EffectType.Mire)) m *= Config.anim_move_speed_mire;
        if (HasEffect(EffectType.DeathStroll)) m *= Config.death_stroll_speed_up;
        if (HasEffect(EffectType.GmSpeedUp)) m *= Config.gm_speed_multiplier;
        return m;
    }
    #endregion

    #region//每帧推进（只做两件事：到期移除、DoT/光环 tick）
    public void OnUpdate()
    {
        if (effects.Count == 0) return;
        s_expired.Clear();
        foreach (var pair in effects)
        {
            var rt = pair.Value;
            if (Time.time >= rt.endTime)
            {
                s_expired.Add(pair.Key);
                continue;
            }
            // 固定间隔生效（燃烧/中毒/死灵漫步，1s 一跳），是否 tick 由类型决定
            if (rt.type.IsTicking() && Time.time >= rt.nextTickTime)
            {
                rt.nextTickTime = Time.time + TickInterval;
                effects[pair.Key] = rt;
                TickDamage(rt);
            }
        }
        for (int i = 0; i < s_expired.Count; i++) RemoveEffect(s_expired[i]);
    }

    private void TickDamage(EffectRuntime rt)
    {
        switch (rt.type)
        {
            case EffectType.Burning:
                if (owner != null) owner.OnDamaged(Config.burning_damage_per_tick * rt.level, FindEntity(rt.sourceId), fixedDamage: true);
                break;
            case EffectType.Poison:
            {
                if (owner == null) break;
                float stat = SourcePower(rt);
                owner.OnDamaged(Config.poison_damage_ratio * stat * rt.level, FindEntity(rt.sourceId), fixedDamage: true);
                break;
            }
            case EffectType.DeathStroll:
            {
                if (owner == null) break;
                float stat = SourcePower(rt);
                EntityCamp enemyCamp = EntityCampUtil.HostileOf(owner.camp);
                s_tickTargets.Clear();
                BattleManager.EntityContainer.GetAllInCamp(owner.transform.position, Config.death_stroll_radius, enemyCamp, s_tickTargets);
                for (int i = 0; i < s_tickTargets.Count; i++)
                {
                    if (s_tickTargets[i] != null) s_tickTargets[i].OnDamaged(Config.death_stroll_dot_ratio * stat * rt.level, owner, fixedDamage: true);
                }
                break;
            }
        }
    }

    // 施加者快照里的 max(力量, 魔力)；无施加者时为 0
    private static float SourcePower(EffectRuntime rt)
    {
        return rt.sourceAttribute != null ? Mathf.Max(rt.sourceAttribute.strength, rt.sourceAttribute.magic) : 0f;
    }

    private static EntityData FindEntity(ushort entityId)
    {
        return BattleManager.EntityContainer.Entities.TryGetObject(entityId, out var entity) ? entity : null;
    }
    #endregion

    #region//Local
    // Health / WeaponSlotCount 不允许被 Buff 修改，走到这里属效果配置错误
    private static float UnsupportedAttrField(EntityAttributeDelta.Field field)
    {
        Debug.LogError($"[Buff] 属性通道不支持 {field}（该属性不可被 Buff 修改）");
        return 0f;
    }

    private static bool IsMoveSpeedEffect(EffectType type) =>
        type is EffectType.AnimSpeedUp or EffectType.AnimSlowDown or EffectType.Mire or EffectType.DeathStroll or EffectType.GmSpeedUp;

    private void ApplyAnimSpeedScale()
    {
        if (owner == null || owner.anim == null) return;
        owner.anim.SetMoveSpeedScale(GetMoveAnimSpeedMultiplier());
    }

    private void RecomputeAttributes()
    {
        if (owner == null) return;
        float currentHealth = owner.floatingAttribute.health;
        var attr = owner.baseAttribute.Clone(); // 此刻 attr.health 是"生命值上限"
        foreach (var pair in effects)
        {
            if (!pair.Key.IsAttribute()) continue;
            attr.ApplyDelta(new EntityAttributeDelta(FieldFromType(pair.Key), AttributeDeltaOf(pair.Key, pair.Value.level)));
        }
        // 夹取上限读 base：生命值上限只由 base 承担（升级时定一次）。当前也无可达的生命类 Buff（不存在 AttrHealth）
        attr.health = Mathf.Clamp(currentHealth, 0f, owner.baseAttribute.health);
        owner.floatingAttribute = attr;
    }

    // 属性增减幅度内部定死：无限视野 = Config 固定值；其余 = 对应属性初始值 × 20% × 等级（方向由 Boost/Decrease 表达）
    private float AttributeDeltaOf(EffectType type, int level)
    {
        if (type == EffectType.InfiniteView) return Config.infinite_view_distance;
        var field = FieldFromType(type);
        float baseValue = field switch
        {
            EntityAttributeDelta.Field.Strength => owner.baseAttribute.strength,
            EntityAttributeDelta.Field.Magic => owner.baseAttribute.magic,
            EntityAttributeDelta.Field.CritRate => owner.baseAttribute.critRate,
            EntityAttributeDelta.Field.CritDamage => owner.baseAttribute.critDamage,
            EntityAttributeDelta.Field.KnockbackResistance => owner.baseAttribute.knockbackResistance,
            EntityAttributeDelta.Field.ViewDistance => owner.baseAttribute.viewDistance,
            _ => UnsupportedAttrField(field),
        };
        float amount = baseValue * Config.buff_attr_ratio * level;
        return type.IsNegative() ? -amount : amount;
    }

    // 护盾初始值 = 被施加者生命值上限 × 系数 × 等级
    private float ShieldValueOf(int level)
    {
        return owner != null ? owner.baseAttribute.health * Config.shield_health_ratio * level : 0f;
    }

    //强控施加
    private void ApplyControl()
    {
        owner.SetAnimPaused(true);
    }

    private void RefreshControlPause()
    {
        foreach (var type in effects.Keys)
        {
            if (type.IsControl()) return;
        }
        owner.SetAnimPaused(false);
    }

    private static EntityAttributeDelta.Field FieldFromType(EffectType type)
    {
        switch (type)
        {
            case EffectType.AttrStrengthBoost:
            case EffectType.AttrStrengthDecrease: return EntityAttributeDelta.Field.Strength;
            case EffectType.AttrMagicBoost:
            case EffectType.AttrMagicDecrease: return EntityAttributeDelta.Field.Magic;
            case EffectType.AttrCritRateBoost:
            case EffectType.AttrCritRateDecrease: return EntityAttributeDelta.Field.CritRate;
            case EffectType.AttrCritDamageBoost:
            case EffectType.AttrCritDamageDecrease: return EntityAttributeDelta.Field.CritDamage;
            case EffectType.AttrKnockbackBoost:
            case EffectType.AttrKnockbackDecrease: return EntityAttributeDelta.Field.KnockbackResistance;
            case EffectType.AttrViewDistanceBoost:
            case EffectType.AttrViewDistanceDecrease:
            case EffectType.InfiniteView: return EntityAttributeDelta.Field.ViewDistance;
            default:
                Debug.LogError($"[Buff] 未处理的属性 Buff 类型：{type}，已按力量兜底");
                return EntityAttributeDelta.Field.Strength; // 兜底不可达；新增 Attr* 时必须同时加进 IsAttribute 与本映射，否则会静默加错属性
        }
    }

    public void FillDisplayInfo(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        foreach (var pair in effects)
        {
            info.buffs.Add(new SCEntityDisplayInfo.BuffRuntime()
            {
                type = (int)pair.Value.type,
                level = pair.Value.level,
            });
        }
    }
    #endregion
}
