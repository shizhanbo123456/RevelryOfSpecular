using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public partial class BattleManager
{
    private ushort[] coralAtPoint = System.Array.Empty<ushort>();
    private float coralSpawnAccumulator;
    private static readonly List<Vector3> s_playerPosScratch = new();

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

        // 珊瑚：不再开局全量生成（按玩家邻近动态生成，见 TickCoralSpawn）；
        // 这里只按刷新点数量重建"哪一点有珊瑚"的占用记录
        ResetCoralSpawnState(spawns.coralSpawnPositions.Count);

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
        var list = Tool.LandscapeSpawns.zombieSpawnPositions;
        if (list == null || list.Count == 0) return; // 未配置僵尸出生点则不刷新
        int variant = Random.Range(0, Config.zombie_variant_count);
        SpawnEntity(EntityType.Zombie(variant), GetZombieSpawnLevel(), LandscapeSpawns.RandomOf(list), EntityCamp.Zombie);
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
    private void ResetCoralSpawnState(int pointCount)
    {
        if (coralAtPoint == null || coralAtPoint.Length != pointCount)
            coralAtPoint = pointCount > 0 ? new ushort[pointCount] : System.Array.Empty<ushort>();
        else
            System.Array.Clear(coralAtPoint, 0, pointCount);
        coralSpawnAccumulator = 0f;
    }

    public void NotifyCoralDestroyed(CoralEntityData coral)
    {
        if (coral == null || coralAtPoint == null) return;
        int idx = coral.spawnPointIndex;
        if (idx >= 0 && idx < coralAtPoint.Length && coralAtPoint[idx] == coral.id) coralAtPoint[idx] = 0;
    }

    private void TickCoralSpawn(float dt)
    {
        int count = coralAtPoint != null ? coralAtPoint.Length : 0;
        if (count == 0) return;

        float rate = Config.coral_spawn_checks_per_second;
        // 累计检测次数；上限压到"1 秒的量"，避免掉帧后单帧爆发式生成
        coralSpawnAccumulator = Mathf.Min(coralSpawnAccumulator + dt * rate, rate);
        while (coralSpawnAccumulator >= 1f)
        {
            coralSpawnAccumulator -= 1f;
            TrySpawnOneCoral(count);
        }
    }

    private void TrySpawnOneCoral(int count)
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

        var points = Tool.LandscapeSpawns.coralSpawnPositions;
        Vector3 center = s_playerPosScratch[Random.Range(0, s_playerPosScratch.Count)];

        float minDist = Config.coral_spawn_min_dist;
        float minSq = minDist * minDist;
        float maxSq = Config.coral_spawn_max_dist * Config.coral_spawn_max_dist;

        // 在"位于所选玩家的环带内、且 minDist 内无任何玩家"的候选点中均匀随机取一个（蓄水池抽样，零分配）
        int picked = -1;
        int hits = 0;
        int scan = Mathf.Min(count, points.Count); // 两者理论一致，取小值防御越界
        for (int i = 0; i < scan; i++)
        {
            Vector3 p = points[i];
            float dx = p.x - center.x;
            float dz = p.z - center.z;
            float d = dx * dx + dz * dz;
            if (d < minSq || d > maxSq) continue;        // 不在所选玩家的环带内
            if (IsAnyPlayerWithin(p, minDist)) continue; // 该点近处还有别的玩家
            if (++hits == 1 || Random.Range(0, hits) == 0) picked = i;
        }
        if (picked < 0) return;

        // 该点已有珊瑚（且实体仍在）→ 不处理
        ushort existing = coralAtPoint[picked];
        if (existing != 0 && EntityContainer.Entities.TryGetObject(existing, out _)) return;

        Vector3 pos = points[picked];
        EntityType type = Random.value < ConfigManager.GlobalVariableConfig.coral_spawn_as_large_chance ? EntityType.CoralLarge : EntityType.CoralSmall;
        ushort id = SpawnEntity(type, 1, pos, EntityCamp.Prop);
        if (id == 0) return;
        coralAtPoint[picked] = id;
        if (GetEntity(id) is CoralEntityData coral) coral.spawnPointIndex = picked;
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
