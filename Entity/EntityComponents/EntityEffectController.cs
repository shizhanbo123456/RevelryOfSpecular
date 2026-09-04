using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实体效果（Buff/状态）控制器 API。
/// 状态分类见策划案 9.2：护盾类 / 属性调整类 / 机制调整类。
/// 【TODO】具体效果实现（时长推进、DoT、属性影响、特效绑定）待后续完善，本类先提供注册/移除/查询接口。
/// </summary>
public class EntityEffectController
{
    /// <summary>效果类型（按策划案 9.2 分类，特效编号见《特效清单与分配表.md》）。</summary>
    public enum EffectType
    {
        // ---- 护盾类（Shield 系）----
        Shield,             // 玩家护盾（护盾特效 1~13 按类型）
        BeaconDamageReduce, // 守护点减伤（75/50/25/0%，护盾叠层 3/2/1/0）
        BeaconReflect,      // 守护点反伤（教皇大招，Buff 特效 4 血色缠绕）
        BeaconSlowDestroy,  // 夜间拆守护点减速（闲置效果：守护点被拆速度变慢）
        BeaconReduceAura,   // 守护点减伤光环（教皇主动，魔法阵 1 黄色力场）

        // ---- 属性调整类 ----
        SpeedUp,            // 加速（Buff 28 环绕风）
        SlowDown,           // 减速（Buff 26/27 水花/雪，可叠层）
        AttackBoost,        // 攻击增益（魔法阵 10 蓝色攻击增益）
        DefenseBoost,       // 防御增益（魔法阵 9 红色防御增益）
        BlessLife,          // 祝福·生命（Buff 29 绿色祝福）
        BlessAttack,        // 祝福·攻击（Buff 30 橙色祝福）
        BlessSpeed,         // 祝福·速度（Buff 31 紫色祝福）
        Heal,               // 治疗（Buff 14/15 绿色/蓝色治愈）
        TowerShield,        // 塔护盾（鹿铠主动 1，护盾特效 1~13 按类型；2026-09-04 替换原塔回血 TowerHeal）

        // ---- 机制调整类 ----
        Mark,               // 暴露标记（Buff 16 黄色周身泛光；白眼伯爵专属增强=被标记者受暴击率增加，2026-09-04）
        Darkness,           // 黑暗：视野缩小（范围魔法 3 + Buff 6 黑雾喷发）
        Fog,                // 迷雾遮蔽/隐身（Buff 1/7 紫雾）
        Invincible,         // 无敌（Buff 2 舞台灯）
        FakeTarget,         // 虚假目标/幻象（Buff 18 黑绿喷发）
        Stun,               // 眩晕（Buff 8~10 迪斯科舞台）
        Freeze,             // 冻结（Buff 13 + 27 雪）
        ParalysisRange,     // 麻痹·封远程：禁止使用远程技能（Buff 20 麻痹蓝）
        ParalysisFreeze,    // 麻痹·定身（Buff 21 麻痹黄）
        Hypnosis,           // 催眠（Buff 22/23 催眠黄/蓝）
        Burning,            // 燃烧（Buff 12 火焰）
        Poison,             // 中毒（护盾 9 绿毒）
        Mushroomize,        // 蘑菇化：施加在水晶上的封锁（表现=变成蘑菇，无法采集，可破坏恢复）
        ViewDistanceReduce, // 视野减小（夜间不对称视野·2026-09-03 后暂无角色采用·效果池候选，复用黑暗特效）
    }

    /// <summary>单个效果的运行时数据。</summary>
    public class EffectRuntime
    {
        public EffectType type;
        /// <summary>等级/叠层（守护点减伤叠层等）。</summary>
        public int level = 1;
        /// <summary>剩余时长（秒，&lt;0 = 永久）。</summary>
        public float remainTime = -1f;
        /// <summary>总时长（秒）。</summary>
        public float totalTime = -1f;
        /// <summary>触发间隔（DoT/逐跳效果用，0=无间隔）。</summary>
        public float tickInterval;
        /// <summary>距下次触发时间。</summary>
        public float nextTick;
    }

    public EntityData owner;

    private readonly Dictionary<EffectType, EffectRuntime> effects = new();

    public void Init(EntityData data)
    {
        owner = data;
        effects.Clear();
    }

    /// <summary>每帧推进：过期移除、DoT 触发（具体结算 TODO）。</summary>
    public void OnUpdate()
    {
        if (effects.Count == 0) return;
        var expired = new List<EffectType>();
        foreach (var pair in effects)
        {
            var runtime = pair.Value;
            if (runtime.remainTime >= 0f)
            {
                runtime.remainTime -= Time.deltaTime;
                if (runtime.remainTime <= 0f)
                {
                    expired.Add(pair.Key);
                    continue;
                }
            }
            // TODO: DoT 触发（Burning/Poison 等每 tickInterval 结算一次伤害）
        }
        foreach (var type in expired)
        {
            RemoveEffect(type);
        }
    }

    /// <summary>添加/刷新效果（duration &lt; 0 = 永久）。</summary>
    public void AddEffect(EffectType type, int level = 1, float duration = -1f, float tickInterval = 0f)
    {
        if (effects.TryGetValue(type, out var runtime))
        {
            runtime.level = level;
            if (duration >= 0f) runtime.remainTime = duration;
            return;
        }
        effects[type] = new EffectRuntime()
        {
            type = type,
            level = level,
            remainTime = duration,
            totalTime = duration,
            tickInterval = tickInterval,
        };
    }

    /// <summary>移除效果。</summary>
    public void RemoveEffect(EffectType type)
    {
        effects.Remove(type);
        // TODO: 移除绑定特效
    }

    /// <summary>是否拥有指定效果。</summary>
    public bool HasEffect(EffectType type)
    {
        return effects.ContainsKey(type);
    }

    /// <summary>获取效果等级/叠层（无则 0）。</summary>
    public int GetEffectLevel(EffectType type)
    {
        return effects.TryGetValue(type, out var runtime) ? runtime.level : 0;
    }

    /// <summary>清空全部效果。</summary>
    public void Clear()
    {
        effects.Clear();
    }

    /// <summary>剩余时长（秒，&lt;0 = 永久，无效果返回 0）。</summary>
    public float GetRemainTime(EffectType type)
    {
        return effects.TryGetValue(type, out var runtime) ? runtime.remainTime : 0f;
    }

    #region//Local 查询接口（具体计算 TODO）
    /// <summary>护盾吸收量（TODO：按 Shield 效果等级计算）。</summary>
    public float GetShieldAbsorb() => 0f;

    /// <summary>总伤害减免比例 0~1（守护点减伤/防御增益等，TODO）。</summary>
    public float GetDamageReduceRate() => 0f;

    /// <summary>移速倍率（加速/减速综合，TODO）。</summary>
    public float GetMoveSpeedMultiplier() => 1f;

    /// <summary>可见距离倍率（黑暗/视野减小，TODO）。</summary>
    public float GetViewDistanceMultiplier() => 1f;

    /// <summary>是否禁止远程技能（麻痹·封远程，TODO 判定）。</summary>
    public bool IsRangedBlocked() => HasEffect(EffectType.ParalysisRange);

    /// <summary>是否无法行动（眩晕/冻结/麻痹·定身/催眠，TODO 判定）。</summary>
    public bool IsActionBlocked() =>
        HasEffect(EffectType.Stun) || HasEffect(EffectType.Freeze) ||
        HasEffect(EffectType.ParalysisFreeze) || HasEffect(EffectType.Hypnosis);
    #endregion
}
