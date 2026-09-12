using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 效果类型 V2（按策划案 11.3 总表重构，旧枚举作废）。
/// 类别决定处理轨道：属性重算类走 Add/Remove 重算；控制类施加时动画速度置 0；
/// DoT 固定数值 1s 间隔；标记类无自身效果供读取层数；其余按战斗管线查询。
/// </summary>
public enum EffectType
{
    // ---- 属性修改（重算轨道，无特效；level 不用，value = 修改量）----
    AttrStrength,       // 力量提升/降低（激励法阵等）
    AttrMagic,          // 魔法提升/降低
    AttrCritRate,       // 暴击率提升/降低
    AttrCritDamage,     // 暴击伤害提升/降低
    AttrKnockback,      // 击退抗性提升/降低
    AttrViewDistance,   // 可见距离提升/降低（无限视野 = +99999）
    AttrWeaponSlot,     // 技能槽位提升/降低

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
}

/// <summary>EffectType 分类扩展。</summary>
public static class EffectTypeExt
{
    /// <summary>属性修改类（重算轨道白名单，唯一允许改属性的 Buff）。</summary>
    public static bool IsAttribute(this EffectType type) => type switch
    {
        EffectType.AttrStrength or EffectType.AttrMagic or
        EffectType.AttrCritRate or EffectType.AttrCritDamage or EffectType.AttrKnockback or
        EffectType.AttrViewDistance or EffectType.AttrWeaponSlot => true,
        _ => false,
    };

    /// <summary>强控类（动画速度 0、霸体失效、打断位移与攻击）。</summary>
    public static bool IsControl(this EffectType type) => type is EffectType.Stun or EffectType.Freeze or EffectType.Root;

    /// <summary>DoT 类（固定数值，1s 间隔 tick）。</summary>
    public static bool IsDoT(this EffectType type) => type is EffectType.Burning or EffectType.Poison;
}

/// <summary>
/// 实体效果（Buff）控制器 V2（对应策划案 11.3 总表，三条铁律）：
/// 1. 双轨属性，事件驱动：属性修改类 Buff（Attr* 白名单）只在 Add/Remove 时统一重算
///    运行时属性（base + Σ修改量），其它 Buff 不允许触碰属性；
/// 2. 查询式战斗管线：功能 Buff 不做每帧计算，由战斗代码在明确时机调用查询接口
///    （护盾吸收/减伤/增减伤乘区/反伤/行动限制/霸体）；
/// 3. OnUpdate 只做两件事：到期移除（记录结束时间戳，不每帧刷新剩余时间）、DoT/光环 tick。
/// Buff 数据一律使用 struct；强控施加时直接将动画播放速度置 0（当前攻击随之被打断）。
/// </summary>
public class EntityEffectController
{
    /// <summary>附加数据（按 Buff 类别取用）。</summary>
    public struct EffectPayload
    {
        public float value;        // 属性修改量（Attr* 类）/ 死灵漫步光环半径
        public float damage;       // 固定数值伤害：DoT 每跳 / Reflect 反弹 / 死灵漫步每跳
        public float shieldValue;  // 护盾类：护盾值
        public ushort sourceId;    // 来源实体 id（DoT 归属）
        public float tickInterval; // 生效间隔（DoT/光环，默认 1s）

        public static EffectPayload Default => new EffectPayload() { tickInterval = 1f };
    }

    /// <summary>单个 Buff 的运行时数据（struct：整体写回）。</summary>
    public struct EffectRuntime
    {
        public EffectType type;
        public int level;
        public float value;        // 属性修改量 / 光环半径
        public float damage;       // 每跳固定伤害 / 反弹伤害
        public float shieldValue;  // 护盾剩余值
        public ushort sourceId;
        public float endTime;      // 生效结束时间戳（永久 = float.MaxValue）
        public float tickInterval; // 生效间隔（秒）
        public float nextTickTime; // 下次生效时刻
        public bool isNegative;
    }

    public EntityData owner;

    /// <summary>清空全部效果（实体销毁时调用）。</summary>
    public void Clear() => effects.Clear();

    private readonly Dictionary<EffectType, EffectRuntime> effects = new();
    private static readonly List<EffectType> s_expired = new();
    private static readonly List<EntityData> s_tickTargets = new();

    public void Init(EntityData data)
    {
        owner = data;
        effects.Clear();
    }

    #region//增删查（简易统一入口）
    /// <summary>
    /// 添加 Buff（统一入口，按类别自动分发：属性重算 / 强控施加 / DoT / 护盾 / 标记）。
    /// level：层数或强度；duration：秒（&lt;0 = 永久）；negative：负面标记（净化用）；
    /// payload：按类别填充（属性量/每跳伤害/护盾值/来源/间隔）。
    /// </summary>
    public void AddEffect(EffectType type, int level = 1, float duration = -1f, bool negative = false, EffectPayload payload = default)
    {
        float endTime = duration < 0f ? float.MaxValue : Time.time + duration;
        if (effects.TryGetValue(type, out var rt))
        {
            // 叠层语义（11.3 特征列）：等级累加 + 时间刷新，其余参数以新值为准
            rt.level += level;
            rt.endTime = endTime;
            rt.value = payload.value;
            rt.damage = payload.damage;
            rt.shieldValue = payload.shieldValue;
            rt.sourceId = payload.sourceId;
            effects[type] = rt;
        }
        else
        {
            rt = new EffectRuntime()
            {
                type = type,
                level = level,
                value = payload.value,
                damage = payload.damage,
                shieldValue = payload.shieldValue,
                sourceId = payload.sourceId,
                endTime = endTime,
                tickInterval = payload.tickInterval > 0f ? payload.tickInterval : 1f,
                nextTickTime = Time.time + (payload.tickInterval > 0f ? payload.tickInterval : 1f),
                isNegative = negative,
            };
            effects[type] = rt;
        }

        // 按类别分发
        if (type.IsAttribute()) RecomputeAttributes();
        else if (type.IsControl()) ApplyControl();
    }

    /// <summary>移除 Buff（属性类触发重算；强控类在全部移除后恢复动画速度）。</summary>
    public void RemoveEffect(EffectType type)
    {
        if (!effects.Remove(type)) return;
        if (type.IsAttribute()) RecomputeAttributes();
        if (type.IsControl()) RefreshControlPause();
    }

    /// <summary>移除全部负面 Buff（净化波动）。</summary>
    public void RemoveAllNegative()
    {
        s_expired.Clear();
        foreach (var pair in effects)
        {
            if (pair.Value.isNegative) s_expired.Add(pair.Key);
        }
        foreach (var type in s_expired) RemoveEffect(type);
    }

    public bool HasEffect(EffectType type) => effects.ContainsKey(type);
    public int GetLevel(EffectType type) => effects.TryGetValue(type, out var rt) ? rt.level : 0;
    public float GetRemainTime(EffectType type) => effects.TryGetValue(type, out var rt) ? Mathf.Max(0f, rt.endTime - Time.time) : 0f;
    #endregion

    #region//战斗管线查询（由战斗代码在明确时机调用，非每帧计算）
    /// <summary>护盾吸收总量（护盾/岩石护盾），OnDamaged 先扣盾。</summary>
    public float GetShieldAbsorb()
    {
        float sum = 0f;
        if (effects.TryGetValue(EffectType.Shield, out var s)) sum += s.shieldValue;
        if (effects.TryGetValue(EffectType.TowerShield, out var t)) sum += t.shieldValue;
        return sum;
    }

    /// <summary>扣减护盾值（OnDamaged 内调用；扣完自动移除）。</summary>
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

    /// <summary>总减伤比例 0~1（副守护点减伤每层 25% + 教皇守护）。</summary>
    public float GetDamageReduceRate()
    {
        float rate = 0f;
        if (effects.TryGetValue(EffectType.BeaconReduce, out var b)) rate += b.level * 0.25f;
        if (effects.TryGetValue(EffectType.PopeGuard, out var p)) rate += p.value;
        return Mathf.Clamp01(rate);
    }

    /// <summary>出伤乘区（愈战愈勇：每层 +10% 增伤）。</summary>
    public float GetOutDamageMultiplier()
    {
        return 1f + GetLevel(EffectType.YzCy) * 0.1f;
    }

    /// <summary>受伤乘区（愈战愈勇：每层 +10% 减伤，1/(1+0.1×层) 递减不归零）。</summary>
    public float GetInDamageMultiplier()
    {
        return 1f / (1f + GetLevel(EffectType.YzCy) * 0.1f);
    }

    /// <summary>反弹伤害（Reflect 的固定数值，0 = 无）。</summary>
    public float GetReflectDamage()
    {
        return effects.TryGetValue(EffectType.Reflect, out var rt) ? rt.damage : 0f;
    }

    /// <summary>是否被强控（麻痹/冰冻/定身）：不可移动、不可攻击、动画停止、霸体失效。</summary>
    public bool IsActionBlocked() => HasEffect(EffectType.Stun) || HasEffect(EffectType.Freeze) || HasEffect(EffectType.Root);

    /// <summary>是否被沉默（无法使用技能，可移动/普攻）。</summary>
    public bool IsSilenced() => HasEffect(EffectType.Silence);

    /// <summary>是否可释放技能。</summary>
    public bool CanCastSkill() => !IsActionBlocked() && !IsSilenced();

    /// <summary>是否可移动。</summary>
    public bool CanMove() => !IsActionBlocked();

    /// <summary>是否处于强制霸体（绝对霸体，如「死灵漫步」期间）。</summary>
    public bool HasSuperArmor() => HasEffect(EffectType.DeathStroll);

    /// <summary>
    /// 动画移动状态播放速度倍率（加速/减速/泥沼的载体，多个并存时连乘）。
    /// 服务器与客户端共用：结果经 EntityAnim.SetMoveSpeedScale 应用到 Animator 的 MoveSpeed 参数。
    /// </summary>
    public float GetMoveAnimSpeedMultiplier()
    {
        float m = 1f;
        if (HasEffect(EffectType.AnimSpeedUp)) m *= Config.anim_move_speed_up;
        if (HasEffect(EffectType.AnimSlowDown)) m *= Config.anim_move_speed_down;
        if (HasEffect(EffectType.Mire)) m *= Config.anim_move_speed_mire;
        return m;
    }

    /// <summary>
    /// 客户端：按同步来的 Buff 类型列表计算动画移速倍率（客户端无控制器，直接按类型换算）。
    /// </summary>
    public static float ComputeMoveAnimSpeedMultiplier(IEnumerable<int> buffTypes)
    {
        float m = 1f;
        foreach (var t in buffTypes)
        {
            switch ((EffectType)t)
            {
                case EffectType.AnimSpeedUp: m *= Config.anim_move_speed_up; break;
                case EffectType.AnimSlowDown: m *= Config.anim_move_speed_down; break;
                case EffectType.Mire: m *= Config.anim_move_speed_mire; break;
            }
        }
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
            // DoT/光环 tick（固定数值伤害，1s 默认间隔）
            if (rt.damage > 0f && rt.tickInterval > 0f && Time.time >= rt.nextTickTime)
            {
                rt.nextTickTime = Time.time + rt.tickInterval;
                effects[pair.Key] = rt;
                TickDamage(rt);
            }
        }
        for (int i = 0; i < s_expired.Count; i++) RemoveEffect(s_expired[i]);
    }

    /// <summary>DoT/光环每跳结算（固定数值：吃护盾/减伤，不吃增减伤）。</summary>
    private void TickDamage(EffectRuntime rt)
    {
        switch (rt.type)
        {
            case EffectType.Poison:
            case EffectType.Burning:
                EntityData source = FindEntity(rt.sourceId);
                owner?.OnDamaged(rt.damage, source, fixedDamage: true);
                break;
            case EffectType.DeathStroll:
                if (owner == null) break;
                EntityCamp enemyCamp = owner.camp == EntityCamp.Attack ? EntityCamp.Defense : EntityCamp.Attack;
                s_tickTargets.Clear();
                BattleManager.EntityContainer.GetAllInCamp(owner.transform.position, rt.value, enemyCamp, s_tickTargets);
                for (int i = 0; i < s_tickTargets.Count; i++)
                {
                    s_tickTargets[i]?.OnDamaged(rt.damage, owner, fixedDamage: true);
                }
                break;
        }
    }

    private static EntityData FindEntity(ushort entityId)
    {
        return BattleManager.EntityContainer.Entities.TryGetObject(entityId, out var entity) ? entity : null;
    }
    #endregion

    #region//Local
    /// <summary>属性重算：运行时属性 = 基础属性 + Σ属性修改 Buff（当前生命夹到新上限，不自动回血）。</summary>
    private void RecomputeAttributes()
    {
        if (owner == null || owner.baseAttribute == null) return;
        float currentHealth = owner.floatingAttribute != null ? owner.floatingAttribute.health : 0f;
        var attr = owner.baseAttribute.Clone();
        foreach (var pair in effects)
        {
            if (!pair.Key.IsAttribute()) continue;
            attr.ApplyDelta(new EntityAttributeDelta(FieldFromType(pair.Key), pair.Value.value));
        }
        attr.health = Mathf.Clamp(currentHealth, 0f, attr.maxHealth);
        owner.floatingAttribute = attr;
    }

    /// <summary>强控施加：打断位移（Exit）+ 动画播放速度直接置 0（当前攻击随之被打断）。</summary>
    private void ApplyControl()
    {
        owner?.RemoveMotion();
        owner?.SetAnimPaused(true);
    }

    /// <summary>强控全部移除后恢复动画播放速度。</summary>
    private void RefreshControlPause()
    {
        foreach (var type in effects.Keys)
        {
            if (type.IsControl()) return;
        }
        owner?.SetAnimPaused(false);
    }

    private static EntityAttributeDelta.Field FieldFromType(EffectType type)
    {
        switch (type)
        {
            case EffectType.AttrStrength: return EntityAttributeDelta.Field.Strength;
            case EffectType.AttrMagic: return EntityAttributeDelta.Field.Magic;
            case EffectType.AttrCritRate: return EntityAttributeDelta.Field.CritRate;
            case EffectType.AttrCritDamage: return EntityAttributeDelta.Field.CritDamage;
            case EffectType.AttrKnockback: return EntityAttributeDelta.Field.KnockbackResistance;
            case EffectType.AttrViewDistance: return EntityAttributeDelta.Field.ViewDistance;
            case EffectType.AttrWeaponSlot: return EntityAttributeDelta.Field.WeaponSlotCount;
            default: return EntityAttributeDelta.Field.Strength;
        }
    }

    /// <summary>组装服务器→客户端的 Buff 摘要（type/level/剩余时间，客户端按映射表挂特效与判断表现）。</summary>
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
