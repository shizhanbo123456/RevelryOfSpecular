using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 防守方角色被动（partial BattleManager）。
/// 策划案 21.5：被动**不是技能** —— 不占技能 id、不注册技能类，按 EntityType 判定、直接实现在战斗逻辑里。
/// 分两类：
///   ① 全局参数（僵尸刷新等级 / 夜晚延长 / 进攻方复活减速）：开战扫一次阵营后结算（策划案二十章原则 5）。
///      用缓存标记而非实时查询：角色死亡期间被动不失效，实时查询会在该角色阵亡的瞬间漏掉。
///   ② 事件驱动（暴击麻痹 / 教皇守护 / 光暗转化）：挂在既有钩子上 —— 命中入口 ProcessHit、昼夜翻转、Buff 叠层。
/// 判定依据统一为"防守方阵营里有没有该角色"，角色下标见 Config.defense_index_*；数值全为占位初值。
/// </summary>
public partial class BattleManager
{
    /// <summary>夜刷普通僵尸等级（PC106 被动可提升，见策划案九/二十章）。</summary>
    public int ZombieSpawnLevel { get; private set; } = Config.zombie_spawn_level;

    /// <summary>进攻方复活进度倍率（PC102 被动减慢；仅进攻方，防守方不受影响）。</summary>
    public float AttackReviveFactor { get; private set; } = 1f;

    /// <summary>本局防守方持有的角色下标（type.value），开战缓存。</summary>
    private readonly HashSet<int> defenseCharacters = new();

    #region 开局全局参数（一次结算）
    /// <summary>结算全局参数类被动。须在双方实体都已生成之后调用（要扫阵营），且在下发昼夜快照之前。</summary>
    private void ApplyGlobalPassives()
    {
        defenseCharacters.Clear();
        foreach (var e in EntityContainer.Entities)
        {
            if (e != null && e.Alive && e.type.category == EntityCategory.Character_Defense) defenseCharacters.Add(e.type.value);
        }

        ZombieSpawnLevel = DefenseHas(Config.defense_index_death_stroller)
            ? Config.zombie_spawn_level_boosted
            : Config.zombie_spawn_level;
        AttackReviveFactor = DefenseHas(Config.defense_index_plague_bringer)
            ? Config.attack_revive_slow_factor
            : 1f;
        if (DefenseHas(Config.defense_index_count_eye)) ExtendNight();

        // 入夜才施加教皇守护，这里按当前昼夜补一次，避免开战即夜晚时漏掉
        if (DefenseHas(Config.defense_index_masked_pope)) ApplyPopeGuard(EnvironmentManager.IsDay);
    }

    /// <summary>防守方阵营是否持有该角色（defenseValue 见 Config.defense_index_*）。</summary>
    private bool DefenseHas(int defenseValue) => defenseCharacters.Contains(defenseValue);

    /// <summary>NP114 被动「夜间时间延长」：延长夜晚、按同量压缩白天，一个完整昼夜周期总长不变。</summary>
    private void ExtendNight()
    {
        var env = Tool.EnvironmentManager;
        if (env == null) return;
        float cycle = env.dayDuration + env.nightDuration;
        float night = Mathf.Min(cycle - 0.01f, env.nightDuration * Config.night_extend_factor);
        env.nightDuration = night;
        env.dayDuration = Mathf.Max(0.01f, cycle - night);
    }
    #endregion

    #region 事件驱动被动
    /// <summary>
    /// 命中时结算攻击方被动（由 EntityData.ProcessHit 调用；子弹与近战球都经该入口，无需在命中点各写一次）。
    /// </summary>
    public void OnHitPassive(EntityData attacker, EntityData target, bool isCrit)
    {
        // PC104 被动「攻击暴击时附加轻微麻痹」：目标已死则不再挂状态
        if (!isCrit || attacker == null || target == null || !target.Alive) return;
        if (attacker.type != EntityType.Defense(Config.defense_index_deer_knight)) return;
        target.effectController?.AddEffect(EffectType.Stun, 1, Config.crit_paralysis_duration,
            negative: true, payload: new EntityEffectController.EffectPayload { sourceId = attacker.id });
    }

    /// <summary>昼夜翻转（订阅 EnvironmentManager.DayNightFlipped）：NP134 被动「教皇守护」入夜施加、天亮移除。</summary>
    private void OnDayNightFlipped(bool isDay)
    {
        if (!AtServer || !BattleStarted) return;
        if (!DefenseHas(Config.defense_index_masked_pope)) return;
        ApplyPopeGuard(isDay);
    }

    /// <summary>给全部守护点施加 / 移除教皇守护（减伤走 effectController 既有的伤害减免查询）。</summary>
    private void ApplyPopeGuard(bool isDay)
    {
        foreach (var beacon in EntityContainer.Beacons)
        {
            if (beacon == null || beacon.effectController == null) continue;
            if (isDay)
            {
                beacon.effectController.RemoveEffect(EffectType.PopeGuard);
            }
            else
            {
                beacon.effectController.AddEffect(EffectType.PopeGuard, 1, float.MaxValue,
                    payload: new EntityEffectController.EffectPayload { value = Config.pope_guard_reduce_rate });
            }
        }
    }

    /// <summary>
    /// PC103 被动「光暗转化」（由 EntityEffectController.AddEffect 在苍白之光 / 苍白之暗叠层后调用）：
    /// 光满 10 层 → 苍白之冰、暗满 10 层 → 苍白之雷、光暗均 ≥8 且都未满 → 苍白之火（同时移除两个标记）。
    /// 两个标记只由 PC103 的技能施加，故直接按层数判定，不吃"PC103 是否在场"。
    /// </summary>
    public void CheckPaleConversion(EntityData owner)
    {
        if (!AtServer || owner == null || owner.effectController == null) return;
        var effect = owner.effectController;
        int light = effect.GetLevel(EffectType.PaleLight);
        int dark = effect.GetLevel(EffectType.PaleDark);

        if (light >= Config.pale_full_stacks)
        {
            effect.RemoveEffect(EffectType.PaleLight);
            effect.AddEffect(EffectType.Freeze, 1, Config.buff_duration_control, negative: true);
        }
        else if (dark >= Config.pale_full_stacks)
        {
            effect.RemoveEffect(EffectType.PaleDark);
            effect.AddEffect(EffectType.Stun, 1, Config.buff_duration_control, negative: true);
        }
        else if (light >= Config.pale_mixed_stacks && dark >= Config.pale_mixed_stacks)
        {
            effect.RemoveEffect(EffectType.PaleLight);
            effect.RemoveEffect(EffectType.PaleDark);
            effect.AddEffect(EffectType.Burning, 1, Config.buff_duration_debuff, negative: true,
                payload: new EntityEffectController.EffectPayload { damage = Config.buff_dot_damage });
        }
    }
    #endregion
}
