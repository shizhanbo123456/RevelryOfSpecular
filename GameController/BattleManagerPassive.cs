using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    public int ZombieSpawnLevel { get; private set; } = Config.zombie_spawn_level;

    public float AttackReviveFactor { get; private set; } = 1f;

    private readonly HashSet<int> defenseCharacters = new();

    #region 开局全局参数（一次结算）
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

    private bool DefenseHas(int defenseValue) => defenseCharacters.Contains(defenseValue);

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
    public void OnHitPassive(EntityData attacker, EntityData target, bool isCrit)
    {
        // PC104 被动「攻击暴击时附加轻微麻痹」：目标已死则不再挂状态
        if (!isCrit || attacker == null || target == null || !target.Alive) return;
        if (attacker.type != EntityType.Defense(Config.defense_index_deer_knight)) return;
        if (target.effectController != null) target.effectController.AddEffect(EffectType.Stun, 1, Config.crit_paralysis_duration,
            negative: true, payload: new EntityEffectController.EffectPayload { sourceId = attacker.id });
    }

    private void OnDayNightFlipped(bool isDay)
    {
        if (!AtServer || !BattleStarted) return;
        if (!DefenseHas(Config.defense_index_masked_pope)) return;
        ApplyPopeGuard(isDay);
    }

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
