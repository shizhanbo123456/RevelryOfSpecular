using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public partial class BattleManager
{
    private float coralSpawnAccumulator;
    private static readonly List<Vector3> s_playerPosScratch = new();
    private static readonly List<Vector3> s_coralPosScratch = new();

    private float neutralStatueRespawnTime = -1f;

    private void SpawnBattleWorld()
    {
        var spawns = Tool.LandscapeSpawns;

        // 副守护点：index 即 value 0~2（贤者雕像；锚点未赋值的槽位跳过）
        var sagePositions = spawns.subGuardStatueSpawnPositions;
        for (int i = 0; i < sagePositions.Count; i++)
        {
            var sageAnchor = sagePositions[i];
            if (sageAnchor == null) continue;
            SpawnEntity(EntityType.GuardStatue(i), 1, sageAnchor.position, EntityCamp.Defense);
        }

        // 主守护点：国王雕像（未赋值则不生成，启动自检会报）
        var kingAnchor = spawns.mainGuardStatueSpawnPosition;
        if (kingAnchor != null) SpawnEntity(EntityType.KingStatue, 1, kingAnchor.position, EntityCamp.Defense);

        // 珊瑚：开局不生成，按玩家邻近采样 MapSpace 合法位置动态生成（见 TickCoralSpawn）

        // 水晶雕像（瘟疫孢子，不复活；锚点未赋值的槽位跳过）
        for (int i = 0; i < spawns.crystalStatueSpawnPositions.Count; i++)
        {
            var crystalStatueAnchor = spawns.crystalStatueSpawnPositions[i];
            if (crystalStatueAnchor == null) continue;
            SpawnEntity(EntityType.CrystalStatue(i), 1, crystalStatueAnchor.position, EntityCamp.DefenseTower);
        }

        // 中立雕像（中立争抢单位）：开局不刷，开战后按延迟计时刷新；之后由被打死的时机重排
        neutralStatueRespawnTime = Time.time + Config.neutral_statue_first_spawn_delay;
    }

    private void SpawnZombie()
    {
        // 出生点在 MapSpace 合法区均匀采样；采样失败（合法区太小/地形缺失）本次就不刷
        if (!Tool.LandscapeSpawns.TrySampleSpawnPoint(out Vector3 pos)) return;
        int variant = Random.Range(0, Config.zombie_variant_count);
        SpawnEntity(EntityType.Zombie(variant), GetZombieSpawnLevel(), pos, EntityCamp.Zombie);
    }

    private void SpawnNeutralStatue()
    {
        var list = Tool.LandscapeSpawns.neutralStatueSpawnPositions;
        if (list == null || list.Count == 0) return; // 未配置候选点则不刷新
        // 白天刷天使雕像、夜晚刷恶魔雕像（两者行为一致，只有外观与 value 不同）
        EntityType statue = EnvironmentManager.IsDay ? EntityType.AngelStatue : EntityType.DemonStatue;
        SpawnEntity(statue, 1, LandscapeSpawns.RandomOf(list), EntityCamp.Neutral);

        // 中立雕像刷新播报（首次出生与重生都算刷新）
        Tool.NetworkManager.SendBattleEvent(new SCBattleEvent()
        {
            type = SCBattleEvent.Type.NeutralStatueRespawn,
            textId = 20,
        });
    }

    public void ScheduleNeutralStatueRespawn()
    {
        neutralStatueRespawnTime = Time.time + Config.neutral_statue_respawn_delay;
    }

    public void NotifyNeutralStatueCaptured(ushort treeId)
    {
        Tool.NetworkManager.SendBattleEvent(new SCBattleEvent()
        {
            type = SCBattleEvent.Type.NeutralStatueCaptured,
            textId = 23,
        });
    }

    #region 珊瑚（按玩家邻近动态生成）
    private const int CoralSampleAttempts = 8;

    private void TickCoralSpawn(float dt)
    {
        float rate = Config.coral_spawn_checks_per_second;
        // 累计检测次数；上限压到"1 秒的量"，避免掉帧后单帧爆发式生成
        coralSpawnAccumulator = Mathf.Min(coralSpawnAccumulator + dt * rate, rate);
        while (coralSpawnAccumulator >= 1f)
        {
            coralSpawnAccumulator -= 1f;
            TrySpawnOneCoral();
        }
    }

    private void TrySpawnOneCoral()
    {
        // 收集全部存活玩家位置（玩家数很少，一次收集供本次检测复用；阵亡/复活等待中返回 null）
        s_playerPosScratch.Clear();
        foreach (var clientId in PlayerInfoList.Keys)
        {
            var e = GetEntityOfClient(clientId);
            if (e == null || !e.Alive) continue;
            s_playerPosScratch.Add(e.transform.position);
        }
        if (s_playerPosScratch.Count == 0) return;

        // 场上已有珊瑚位置，用于保持最小间距
        s_coralPosScratch.Clear();
        foreach (var entity in EntityContainer.Entities)
        {
            if (entity == null || !entity.Alive || entity.type.category != EntityCategory.Coral) continue;
            s_coralPosScratch.Add(entity.transform.position);
        }

        Vector3 center = s_playerPosScratch[Random.Range(0, s_playerPosScratch.Count)];
        float minDist = Config.coral_spawn_min_dist;

        for (int attempt = 0; attempt < CoralSampleAttempts; attempt++)
        {
            // 采样：所选玩家的环带内 + MapSpace 合法区
            if (!Tool.LandscapeSpawns.TrySampleSpawnPointInRing(center, minDist, Config.coral_spawn_max_dist, out Vector3 pos)) continue;
            if (IsAnyPlayerWithin(pos, minDist)) continue;              // 该点近处还有别的玩家
            if (IsAnyCoralWithin(pos, Config.coral_min_separation)) continue; // 与已有珊瑚距离必须 > 1m
            if (CoralDensityAt(pos) >= Config.coral_chunk_max_density) continue; // 所在区块珊瑚密度已达上限

            EntityType type = Random.value < ConfigManager.GlobalVariableConfig.coral_spawn_as_large_chance ? EntityType.CoralLarge : EntityType.CoralSmall;
            SpawnEntity(type, 1, pos, EntityCamp.Prop);
            return;
        }
    }

    // 候选点所在区块的珊瑚平均密度（个/㎡）：区块面积按整格 chunkSize² 算，不剔除区块内的非法区
    private static float CoralDensityAt(Vector3 pos)
    {
        int count = EntityContainer.Corals.GetChunkCount(ChunkSearcher<EntityData>.GetChunkIndex(pos));
        return count / (Landscape.chunkSize * Landscape.chunkSize);
    }

    private static bool IsAnyPlayerWithin(Vector3 pos, float radius)
    {
        float rSq = radius * radius;
        for (int i = 0; i < s_playerPosScratch.Count; i++)
        {
            Vector3 p = s_playerPosScratch[i];
            float dx = p.x - pos.x;
            float dz = p.z - pos.z;
            if (dx * dx + dz * dz <= rSq) return true;
        }
        return false;
    }

    private static bool IsAnyCoralWithin(Vector3 pos, float radius)
    {
        float rSq = radius * radius;
        for (int i = 0; i < s_coralPosScratch.Count; i++)
        {
            Vector3 p = s_coralPosScratch[i];
            float dx = p.x - pos.x;
            float dz = p.z - pos.z;
            if (dx * dx + dz * dz <= rSq) return true;
        }
        return false;
    }
    #endregion

    private void TickWorldRespawn()
    {
        // 中立雕像：倒计时到期刷新一棵（同时只有一棵，故用单值时刻而不进列表）
        if (neutralStatueRespawnTime >= 0f && Time.time >= neutralStatueRespawnTime)
        {
            neutralStatueRespawnTime = -1f;
            SpawnNeutralStatue();
        }
    }
}
