using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    private bool zombieSpawnLevelBoosted;

    private readonly HashSet<int> defenseCharacters = new();

    // 瘟疫使者「致命伤害拦截」的冷却：按玩家角色记（复活后仍占用），无归属实体按 id 记
    private readonly Dictionary<int, float> plagueGuardReadyTime = new();

    #region 开局全局参数（一次结算）
    private void ApplyGlobalPassives()
    {
        defenseCharacters.Clear();
        foreach (var e in EntityContainer.Entities)
        {
            if (e != null && e.Alive && e.type.category == EntityCategory.Character_Defense) defenseCharacters.Add(e.type.value);
        }
        GmOnBattleStart(); // GM：把组队期启用的被动并进本次结算，并清掉上一局的无敌记录

        zombieSpawnLevelBoosted = DefenseHas(Config.defense_index_death_stroller);

        // 入夜才施加教皇守护，这里按当前昼夜补一次，避免开战即夜晚时漏掉
        if (DefenseHas(Config.defense_index_masked_pope)) ApplyPopeGuard(EnvironmentManager.IsDay);
    }

    private bool DefenseHas(int defenseValue) => defenseCharacters.Contains(defenseValue);
    #endregion

    #region 受击被动（白眼伯爵 / 瘟疫使者）
    // 白眼伯爵：单次受到伤害不超过生命上限的固定比例，在护盾与减伤之前就裁到上限
    public float AdjustIncomingDamage(EntityData victim, float damage)
    {
        if (victim == null || damage <= 0f) return damage;
        if (victim.type != EntityType.Defense(Config.defense_index_count_eye)) return damage;
        float cap = victim.baseAttribute.health * Config.count_eye_damage_cap_ratio;
        return damage > cap ? cap : damage;
    }

    // 瘟疫使者：会致死的伤害改成「剩余血量 -1」，同时挂护盾；每个角色每冷却只触发一次
    public float BlockLethalDamage(EntityData victim, float damage)
    {
        if (victim == null || damage <= 0f) return damage;
        if (victim.type != EntityType.Defense(Config.defense_index_plague_bringer)) return damage;

        float health = victim.floatingAttribute.health;
        if (damage < health) return damage; // 不致死
        if (health <= 1f) return damage;    // 已只剩 1 血，留不出 1 血，正常死亡

        int key = EntityOwnerClient.TryGetValue(victim.id, out short owner) ? owner : 100000 + victim.id;
        if (plagueGuardReadyTime.TryGetValue(key, out float readyTime) && Time.time < readyTime) return damage;

        plagueGuardReadyTime[key] = Time.time + Config.plague_shield_cooldown;
        if (victim.effectController != null)
            victim.effectController.AddEffect(EffectType.Shield, Config.plague_shield_level, Config.plague_shield_duration);
        return health - 1f;
    }
    #endregion

    #region 僵尸刷新等级（按战斗剩余时间实时计算，普通/精英共用）
    public int GetZombieSpawnLevel()
    {
        float elapsed = Mathf.Clamp01((Config.battle_duration - BattleRemainTime) / Config.battle_duration);
        int level = Config.zombie_spawn_level + Mathf.FloorToInt(elapsed / Config.zombie_level_step_ratio);
        if (zombieSpawnLevelBoosted) level += Config.zombie_spawn_level_passive_boost;
        return level;
    }
    #endregion

    #region 事件驱动被动
    public void OnHitPassive(EntityData attacker, EntityData target, DamageType type)
    {
        // 鹿铠怪人 被动「攻击暴击时附加轻微麻痹」：目标已死则不再挂状态
        if (type != DamageType.Crit || attacker == null || target == null || !target.Alive) return;
        if (attacker.type != EntityType.Defense(Config.defense_index_deer_knight)) return;
        if (target.effectController != null) target.effectController.AddEffect(EffectType.Stun, 1, Config.crit_paralysis_duration, attacker);
    }

    private void OnDayNightFlipped(bool isDay)
    {
        if (!AtServer || !BattleStarted) return;
        if (!DefenseHas(Config.defense_index_masked_pope)) return;
        ApplyPopeGuard(isDay);
    }

    private void ApplyPopeGuard(bool isDay)
    {
        foreach (var guardStatue in EntityContainer.GuardStatues)
        {
            if (guardStatue == null || guardStatue.effectController == null) continue;
            if (isDay)
            {
                guardStatue.effectController.RemoveEffect(EffectType.PopeGuard);
            }
            else
            {
                guardStatue.effectController.AddEffect(EffectType.PopeGuard, 1, float.MaxValue);
            }
        }
    }

    // source = 刚叠层的苍白标记的施加者，转化出的苍白之冰/雷/火归属它
    public void CheckPaleConversion(EntityData owner, EntityData source)
    {
        if (!AtServer || owner == null || owner.effectController == null) return;
        var effect = owner.effectController;
        int light = effect.GetLevel(EffectType.PaleLight);
        int dark = effect.GetLevel(EffectType.PaleDark);

        if (light >= Config.pale_full_stacks)
        {
            effect.RemoveEffect(EffectType.PaleLight);
            effect.AddEffect(EffectType.PaleIce, 1, Config.buff_duration_control, source);
        }
        else if (dark >= Config.pale_full_stacks)
        {
            effect.RemoveEffect(EffectType.PaleDark);
            effect.AddEffect(EffectType.PaleThunder, 1, Config.buff_duration_control, source);
        }
        else if (light >= Config.pale_mixed_stacks && dark >= Config.pale_mixed_stacks)
        {
            effect.RemoveEffect(EffectType.PaleLight);
            effect.RemoveEffect(EffectType.PaleDark);
            effect.AddEffect(EffectType.PaleFire, 1, Config.buff_duration_debuff, source);
        }
    }
    #endregion
}
