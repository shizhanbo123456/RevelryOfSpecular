using System.Collections.Generic;
using Ros.Gm;
using Ros.Transport;
using UnityEngine;

// GM 指令的服务器执行体：NetworkManager.ServerReceiveGmLocal → ExecuteGm。
// 只处理"联网项"；纯客户端项（雾气/地形/存档/调试/收集-面数/敌方对话/结算演示）在 GmPanel 里本地做。
// 无权限校验（开发工具，按用户口径所有客户端都能发）。
public partial class BattleManager
{
    // 无敌实体（EntityData.OnDamaged 命中即按 0 伤害）
    private static readonly HashSet<ushort> gmInvincible = new();
    // 组队期 GM 启用的防守方被动（下标 = Config.defense_index_*）
    private static readonly HashSet<int> gmPassives = new();

    public static bool IsGmInvincible(ushort entityId) => gmInvincible.Contains(entityId);

    // 开战钩子（由 ApplyGlobalPassives 调用）：注入 GM 被动；清掉上一局的无敌记录（实体 id 会被复用）
    private void GmOnBattleStart()
    {
        foreach (var index in gmPassives) defenseCharacters.Add(index);
        gmInvincible.Clear();
    }

    public void ExecuteGm(short clientId, CSGm gm)
    {
        if (!AtServer) return;
        Debug.Log($"[GM] {gm.type}(int1={gm.int1},int2={gm.int2},f1={gm.float1},f2={gm.float2},f3={gm.float3}) client={clientId}");

        switch (gm.type)
        {
            // ===== 对局 =====
            case GmEnum.Battle_RemainTime_Add: GmSetRemainTime(BattleRemainTime + gm.float1); break;
            case GmEnum.Battle_RemainTime_Reduce: GmSetRemainTime(BattleRemainTime - gm.float1); break;
            case GmEnum.Battle_RemainTime_Set: GmSetRemainTime(gm.float1); break;
            case GmEnum.Battle_Settle:
                if (!BattleStarted) break;
                if (gm.int1 == (int)GmSettleType.Annihilate) CheckAttackWiped();
                else EndBattle(AttackScore >= DefenseScore() ? 1 : 2); // 时间结束：按分数
                break;
            case GmEnum.Battle_DayNight_SetTimePoint:
                // 滑条 = 0~1 全天进度 → 周期值 [0,2)
                if (Tool.EnvironmentManager != null)
                    Tool.EnvironmentManager.SetCycleTime(gm.float1 * EnvironmentManager.CycleLength);
                break;
            case GmEnum.Battle_DayNight_SetDayDuration:
                if (Tool.EnvironmentManager != null)
                    Tool.EnvironmentManager.dayDuration = Mathf.Max(0.01f, gm.float1);
                break;
            case GmEnum.Battle_DayNight_SetNightDuration:
                // 允许覆盖防御方「夜晚延长」被动算出的值
                if (Tool.EnvironmentManager != null)
                    Tool.EnvironmentManager.nightDuration = Mathf.Max(0.01f, gm.float1);
                break;

            // ===== 实体 =====
            case GmEnum.Entity_Spawn: GmSpawn((GmEntityType)gm.int1, gm.int2); break;
            case GmEnum.Entity_Kill: GmKill(clientId, (GmEntityType)gm.int1, (GmFilterType)gm.int2, gm.float1); break;
            case GmEnum.Entity_Knockback: GmKnockback(clientId, gm.float1, gm.float2, (GmFilterType)gm.int1, gm.float3); break;

            // ===== 玩家 =====
            case GmEnum.Player_Teleport_ToCoordinate:
                GmTeleport(clientId, new Vector3(gm.float1, Config.gm_teleport_height, gm.float2));
                break;
            case GmEnum.Player_Teleport_ToGuardPoint: GmTeleportToBeacon(clientId, gm.int1); break;
            case GmEnum.Player_Revive: GmRevive(clientId); break;
            case GmEnum.Player_Skill_GetSkill: GmAddSkill(clientId, gm.int1); break;
            case GmEnum.Player_Skill_DirectUseSkill: GmForceUseSkill(clientId, gm.int1); break;
            case GmEnum.Player_Skill_ResetCd: GmResetAllCd(clientId); break;
            case GmEnum.Player_Skill_ClearSkill: GmClearSkill(clientId); break;
            case GmEnum.Player_Skill_PassiveEnable:
                if (gm.int2 != 0) gmPassives.Add(gm.int1); else gmPassives.Remove(gm.int1);
                break;
            case GmEnum.Player_Buff_AddBuff: GmAddBuff(clientId, (EffectType)gm.int1); break;
            case GmEnum.Player_Buff_RemoveBuff: GmRemoveBuff(clientId, (EffectType)gm.int1); break;
            case GmEnum.Player_Buff_RemoveAllBuff: GmRemoveAllBuff(clientId); break;
            case GmEnum.Player_Cheat_Flight:
                if (GetGmPlayerEntity(clientId) is PlayerEntityData flight) flight.SetFlightMode(gm.int1 != 0);
                break;
            case GmEnum.Player_Cheat_Invincible: GmSetInvincible(clientId, gm.int1 != 0); break;
            case GmEnum.Player_Cheat_Speed:
                {
                    var effect = GetGmEffectController(clientId);
                    if (effect == null) break;
                    if (gm.int1 != 0) effect.AddEffect(EffectType.GmSpeedUp);
                    else effect.RemoveEffect(EffectType.GmSpeedUp);
                    break;
                }
            case GmEnum.Player_Cheat_IgnoreCd:
                {
                    var skills = GetGmSkillController(clientId);
                    if (skills != null) skills.GmIgnoreCd = gm.int1 != 0;
                    break;
                }

            // ===== 其它（收集）=====
            case GmEnum.Other_Collect_ServerFps:
            case GmEnum.Other_Collect_ServerTime:
                Tool.NetworkManager.SendGm(clientId, new SCGm()
                {
                    type = gm.type,
                    int1 = EnsInstance.frameRate,
                    float1 = UnityEngine.Time.time,
                });
                break;
        }
    }

    #region//Local
    // 剩余时间：计时分支只在 >0 时推进，所以下限压到 0.01，让它在下一帧自然结算
    private void GmSetRemainTime(float seconds)
    {
        if (!BattleStarted) return;
        BattleRemainTime = Mathf.Max(0.01f, seconds);
    }

    private EntityData GetGmPlayerEntity(short clientId) =>
        PlayerEntityId.TryGetValue(clientId, out var id) ? GetEntity(id) : null;

    private EntitySkillController GetGmSkillController(short clientId)
    {
        var owner = GetGmPlayerEntity(clientId);
        return owner != null ? owner.skillController : null;
    }

    private EntityEffectController GetGmEffectController(short clientId)
    {
        var owner = GetGmPlayerEntity(clientId);
        return owner != null ? owner.effectController : null;
    }

    // 技能 / buff：GM 指令作用于发送者自己，取不到实体就静默跳过
    private void GmAddSkill(short clientId, int skillId)
    {
        var skills = GetGmSkillController(clientId);
        if (skills != null) skills.AddSkill(skillId);
    }

    private void GmForceUseSkill(short clientId, int skillId)
    {
        var skills = GetGmSkillController(clientId);
        if (skills != null) skills.GmForceUseSkill(skillId);
    }

    private void GmResetAllCd(short clientId)
    {
        var skills = GetGmSkillController(clientId);
        if (skills != null) skills.GmResetAllCd();
    }

    private void GmClearSkill(short clientId)
    {
        var skills = GetGmSkillController(clientId);
        if (skills != null) skills.SetSkillList(null);
    }

    private void GmAddBuff(short clientId, EffectType type)
    {
        var effect = GetGmEffectController(clientId);
        if (effect != null) effect.AddEffect(type);
    }

    private void GmRemoveBuff(short clientId, EffectType type)
    {
        var effect = GetGmEffectController(clientId);
        if (effect != null) effect.RemoveEffect(type);
    }

    private void GmRemoveAllBuff(short clientId)
    {
        var effect = GetGmEffectController(clientId);
        if (effect != null) effect.RemoveAll();
    }

    private void GmSetInvincible(short clientId, bool on)
    {
        var target = GetGmPlayerEntity(clientId);
        if (target == null) return;
        if (on) gmInvincible.Add(target.id);
        else gmInvincible.Remove(target.id);
    }

    // 传送：直接落点（y = Config.gm_teleport_height），并清速度与位移，避免刚体/位移效果把角色拉回
    private void GmTeleport(short clientId, Vector3 pos)
    {
        var target = GetGmPlayerEntity(clientId);
        if (target == null) return;
        pos.y = Config.gm_teleport_height;
        target.transform.position = pos;
        if (target.rb != null)
        {
            target.rb.position = pos;
            target.rb.velocity = Vector3.zero;
        }
        target.motion = null;
    }

    private void GmTeleportToBeacon(short clientId, int beaconIndex)
    {
        foreach (var beacon in EntityContainer.Beacons)
        {
            if (beacon == null) continue;
            bool match = beaconIndex >= Config.outer_beacon_count
                ? beacon.type == EntityType.CoreBeacon
                : beacon.type.category == EntityCategory.Beacon && beacon.type.value == beaconIndex;
            if (!match) continue;
            GmTeleport(clientId, beacon.transform.position);
            return;
        }
        Debug.LogWarning($"[GM] 场上没有可用的守护点（{beaconIndex}），传送取消");
    }

    private void GmRevive(short clientId)
    {
        if (!BattleStarted) return;
        if (PlayerEntityId.ContainsKey(clientId)) return; // 还活着
        if (!reviveStates.TryGetValue(clientId, out var rs))
        {
            rs = new ReviveState();
            reviveStates[clientId] = rs;
        }
        RespawnPlayer(clientId, rs);
    }

    private void GmSpawn(GmEntityType type, int count)
    {
        if (!BattleStarted || count <= 0) return;
        var spawns = Tool.LandscapeSpawns;
        switch (type)
        {
            case GmEntityType.Zombie:
                for (int i = 0; i < count; i++)
                    SpawnEntity(EntityType.Zombie(Random.Range(0, Config.zombie_variant_count)), GetZombieSpawnLevel(),
                        LandscapeSpawns.RandomOf(spawns.zombieSpawnPositions), EntityCamp.Zombie);
                break;
            case GmEntityType.EliteZombie:
                for (int i = 0; i < count; i++)
                    SpawnEntity(EntityType.EliteZombie(Random.Range(0, Config.elite_zombie_variant_count)), GetZombieSpawnLevel(),
                        LandscapeSpawns.RandomOf(spawns.zombieSpawnPositions), EntityCamp.Zombie);
                break;
            case GmEntityType.Crystal: GmSpawnCrystals(count); break;
            case GmEntityType.Neutral:
                // 中立单位（瘟疫树）场上最多 1 个，已有就不再刷
                foreach (var e in EntityContainer.Entities)
                    if (e != null && e.Alive && e.type.category == EntityCategory.PlagueTree) return;
                SpawnEntity(EntityType.PlagueTree0, 1, LandscapeSpawns.RandomOf(spawns.plagueTreeSpawnPositions), EntityCamp.Neutral);
                break;
            case GmEntityType.Tower:
                for (int i = 0; i < count; i++)
                    SpawnEntity(EntityType.Tower(Random.Range(0, Config.tower_count)), 1,
                        LandscapeSpawns.RandomOf(spawns.towerSpawnPositions), EntityCamp.Defense);
                break;
        }
    }

    // 水晶：与 TickCrystalSpawn 共用 crystalAtPoint 占用簿记，只刷当前空着的刷新点
    private void GmSpawnCrystals(int count)
    {
        if (crystalAtPoint == null || crystalAtPoint.Length == 0) return;
        var points = Tool.LandscapeSpawns.crystalSpawnPositions;
        int spawned = 0;
        for (int i = 0; i < crystalAtPoint.Length && spawned < count; i++)
        {
            ushort existing = crystalAtPoint[i];
            if (existing != 0 && EntityContainer.Entities.TryGetObject(existing, out _)) continue; // 该点已有水晶
            Vector3 pos = i < points.Count ? points[i] : Landscape.MapCenter;
            ushort id = SpawnEntity(EntityType.Crystal(i % Config.crystal_graphics_count), 1, pos, EntityCamp.Prop);
            if (id == 0) continue;
            crystalAtPoint[i] = id;
            if (GetEntity(id) is CrystalEntityData crystal) crystal.spawnPointIndex = i;
            spawned++;
        }
    }

    private void GmKill(short clientId, GmEntityType type, GmFilterType filter, float amount)
    {
        if (!BattleStarted) return;
        var targets = CollectGmTargets(clientId, GmCategoryOf(type), filter, amount);
        foreach (var e in targets) e.Kill();
    }

    // 击飞：从 GM 自己实体向外；不判霸体；飞行（FlightMode）与无刚体单位跳过
    private void GmKnockback(short clientId, float horizontal, float vertical, GmFilterType filter, float radius)
    {
        if (!BattleStarted) return;
        var source = GetGmPlayerEntity(clientId);
        if (source == null) return;
        var targets = CollectGmTargets(clientId, null, filter, radius);
        foreach (var e in targets)
        {
            if (e.FlightMode || e.rb == null) continue;
            Vector3 away = e.transform.position - source.transform.position;
            away.y = 0f;
            Vector3 dir = away.sqrMagnitude > 0.0001f ? away.normalized : e.transform.forward;
            if (horizontal != 0f)
                e.SetVelocityHorizontal(new Vector2(dir.x * horizontal, dir.z * horizontal), VelocitySource.Knockback);
            if (vertical != 0f) e.SetVelocityVertical(vertical, VelocitySource.Knockback);
        }
    }

    private static EntityCategory GmCategoryOf(GmEntityType type) => type switch
    {
        GmEntityType.EliteZombie => EntityCategory.EliteZombie,
        GmEntityType.Crystal => EntityCategory.Crystal,
        GmEntityType.Neutral => EntityCategory.PlagueTree,
        GmEntityType.Tower => EntityCategory.Tower,
        _ => EntityCategory.Zombie,
    };

    private readonly List<EntityData> gmTargetScratch = new();

    // 目标筛选（都以 GM 自己实体为参照）：All 全部 / Nearest 最近 1 个 / Count 就近 amount 个 / Radius amount 米内
    private List<EntityData> CollectGmTargets(short clientId, EntityCategory? category, GmFilterType filter, float amount)
    {
        gmTargetScratch.Clear();
        var source = GetGmPlayerEntity(clientId);
        Vector3 origin = source != null ? source.transform.position : Landscape.MapCenter;
        foreach (var e in EntityContainer.Entities)
        {
            if (e == null || !e.Alive) continue;
            if (source != null && e.id == source.id) continue; // 排除自己
            if (category.HasValue && e.type.category != category.Value) continue;
            gmTargetScratch.Add(e);
        }

        switch (filter)
        {
            case GmFilterType.Nearest:
            case GmFilterType.Count:
                gmTargetScratch.Sort((a, b) => (a.transform.position - origin).sqrMagnitude
                    .CompareTo((b.transform.position - origin).sqrMagnitude));
                int keep = filter == GmFilterType.Nearest ? 1 : Mathf.Max(1, Mathf.RoundToInt(amount));
                if (gmTargetScratch.Count > keep) gmTargetScratch.RemoveRange(keep, gmTargetScratch.Count - keep);
                break;
            case GmFilterType.Radius:
                float r = Mathf.Max(0f, amount);
                gmTargetScratch.RemoveAll(e => (e.transform.position - origin).sqrMagnitude > r * r);
                break;
        }
        return gmTargetScratch;
    }
    #endregion
}
