using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 战斗世界生成（partial BattleManager）：
/// 守护点/防御塔开局生成、瘟疫树延时刷新与攻占后重生、夜间僵尸刷新落地、
/// **水晶按玩家邻近动态生成**（不再开局全量生成、被摧毁后也不再有固定重生计时，见 TickCrystalSpawn）。
/// 位置唯一来源：地形组件 LandscapeSpawns（策划案 6.1/7/8.1），不再回退其它组件。
/// 各实体的行为见其 EntityData 子类：僵尸 ZombieEntityData（游荡/追击/攻击）、防御塔与瘟疫树 AutoCastEntityData（周期施法）。
/// </summary>
public partial class BattleManager
{
    /// <summary>各水晶刷新点当前的水晶实体 id（下标 = 刷新点下标；0 = 该点没有水晶）。</summary>
    private ushort[] crystalAtPoint = System.Array.Empty<ushort>();
    /// <summary>水晶生成检测累计器（按 Config.crystal_spawn_checks_per_second 每秒消耗若干次）。</summary>
    private float crystalSpawnAccumulator;
    /// <summary>一次检测内复用的存活玩家位置（避免对每个候选点重复查表）。</summary>
    private static readonly List<Vector3> s_playerPosScratch = new();

    /// <summary>瘟疫树重生时刻（&lt; 0 = 无计划；场上有树时同样为 -1，保证同时只有一棵）。</summary>
    private float plagueTreeRespawnTime = -1f;

    /// <summary>开局生成对局世界（守护点×4 / 防御塔；水晶改由 TickCrystalSpawn 动态生成、瘟疫树延时刷新）。</summary>
    private void SpawnBattleWorld()
    {
        var spawns = Tool.LandscapeSpawns;

        // 守护点：列表前 3 个外围（value 0~2）+ 最后 1 个中心（锚点未赋值的槽位跳过）
        for (int i = 0; i < spawns.beaconSpawnPositions.Count; i++)
        {
            var beaconAnchor = spawns.beaconSpawnPositions[i];
            if (beaconAnchor == null) continue;
            bool core = i >= Config.outer_beacon_count;
            SpawnEntity(core ? EntityType.CoreBeacon : EntityType.Beacon(i), 1, beaconAnchor.position, EntityCamp.Defense);
        }

        // 水晶：不再开局全量生成（策划案第七章改为按玩家邻近动态生成，见 TickCrystalSpawn）；
        // 这里只按刷新点数量重建"哪一点有水晶"的占用记录
        ResetCrystalSpawnState(spawns.crystalSpawnPositions.Count);

        // 防御塔（瘟疫孢子，不复活；锚点未赋值的槽位跳过）
        for (int i = 0; i < spawns.towerSpawnPositions.Count; i++)
        {
            var towerAnchor = spawns.towerSpawnPositions[i];
            if (towerAnchor == null) continue;
            SpawnEntity(EntityType.Tower(i), 1, towerAnchor.position, EntityCamp.Defense);
        }

        // 瘟疫树（中立争抢单位）：开局不刷，开战后按延迟计时刷新；之后由被打死的时机重排
        plagueTreeRespawnTime = Time.time + Config.plague_tree_first_spawn_delay;
    }

    /// <summary>夜间刷新一只普通僵尸：出生点从地形组件随机取，外观变体随机；等级取全局参数（PC106 被动可提升）。</summary>
    private void SpawnZombie()
    {
        var list = Tool.LandscapeSpawns.zombieSpawnPositions;
        if (list == null || list.Count == 0) return; // 未配置僵尸出生点则不刷新
        int variant = Random.Range(0, Config.zombie_variant_count);
        SpawnEntity(EntityType.Zombie(variant), ZombieSpawnLevel, LandscapeSpawns.RandomOf(list), EntityCamp.Zombie);
    }

    /// <summary>刷新一棵瘟疫树：候选点随机取一。</summary>
    private void SpawnPlagueTree()
    {
        var list = Tool.LandscapeSpawns.plagueTreeSpawnPositions;
        if (list == null || list.Count == 0) return; // 未配置候选点则不刷新
        SpawnEntity(EntityType.PlagueTree0, 1, LandscapeSpawns.RandomOf(list), EntityCamp.Neutral);
    }

    /// <summary>瘟疫树被打死：排下一次刷新倒计时。</summary>
    public void SchedulePlagueTreeRespawn()
    {
        plagueTreeRespawnTime = Time.time + Config.plague_tree_respawn_delay;
    }

    /// <summary>瘟疫树被攻占（树被打死时由 PlagueTreeEntityData 调用）：广播攻占事件供客户端做表现。</summary>
    public void NotifyPlagueTreeCaptured(ushort treeId)
    {
        Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.PlagueTreeCaptured);
    }

    #region 水晶（按玩家邻近动态生成）
    /// <summary>开战重置水晶生成状态：按当前刷新点数量重建占用记录、清零累计器。</summary>
    private void ResetCrystalSpawnState(int pointCount)
    {
        if (crystalAtPoint == null || crystalAtPoint.Length != pointCount)
            crystalAtPoint = pointCount > 0 ? new ushort[pointCount] : System.Array.Empty<ushort>();
        else
            System.Array.Clear(crystalAtPoint, 0, pointCount);
        crystalSpawnAccumulator = 0f;
    }

    /// <summary>水晶被摧毁（CrystalEntityData.OnKilled 调用）：清空其刷新点的占用记录，使其可被再次生成。</summary>
    public void NotifyCrystalDestroyed(CrystalEntityData crystal)
    {
        if (crystal == null || crystalAtPoint == null) return;
        int idx = crystal.spawnPointIndex;
        if (idx >= 0 && idx < crystalAtPoint.Length && crystalAtPoint[idx] == crystal.id) crystalAtPoint[idx] = 0;
    }

    /// <summary>
    /// 水晶邻近生成（每帧调用）：按 Config.crystal_spawn_checks_per_second 累计检测次数（每秒若干次）。
    /// 每次检测：随机取一个存活玩家 → 在其 [minDist, maxDist] 距离环带内随机取一个刷新点 →
    /// 全局确认该点 minDist 内没有任何存活玩家 → 该点没有水晶则生成一个（已有则跳过）。
    /// 注意：不销毁水晶——已生成的水晶只会在被玩家打碎时消失（见 NotifyCrystalDestroyed）。
    /// </summary>
    private void TickCrystalSpawn(float dt)
    {
        int count = crystalAtPoint != null ? crystalAtPoint.Length : 0;
        if (count == 0) return;

        float rate = Config.crystal_spawn_checks_per_second;
        // 累计检测次数；上限压到"1 秒的量"，避免掉帧后单帧爆发式生成
        crystalSpawnAccumulator = Mathf.Min(crystalSpawnAccumulator + dt * rate, rate);
        while (crystalSpawnAccumulator >= 1f)
        {
            crystalSpawnAccumulator -= 1f;
            TrySpawnOneCrystal(count);
        }
    }

    /// <summary>一次生成检测（详见 TickCrystalSpawn）。</summary>
    private void TrySpawnOneCrystal(int count)
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

        var points = Tool.LandscapeSpawns.crystalSpawnPositions;
        Vector3 center = s_playerPosScratch[Random.Range(0, s_playerPosScratch.Count)];

        float minDist = Config.crystal_spawn_min_dist;
        float minSq = minDist * minDist;
        float maxSq = Config.crystal_spawn_max_dist * Config.crystal_spawn_max_dist;

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

        // 该点已有水晶（且实体仍在）→ 不处理
        ushort existing = crystalAtPoint[picked];
        if (existing != 0 && EntityContainer.Entities.TryGetObject(existing, out _)) return;

        Vector3 pos = points[picked];
        ushort id = SpawnEntity(EntityType.Crystal(picked % Config.crystal_graphics_count), 1, pos, EntityCamp.Prop);
        if (id == 0) return;
        crystalAtPoint[picked] = id;
        if (GetEntity(id) is CrystalEntityData crystal) crystal.spawnPointIndex = picked;
    }

    /// <summary>该位置 minDist 内是否有任何存活玩家（用本次检测收集到的玩家位置）。</summary>
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

    /// <summary>世界重生推进（时间戳到期检查，无每帧状态计算）。</summary>
    private void TickWorldRespawn()
    {
        // 瘟疫树：倒计时到期刷新一棵（同时只有一棵，故用单值时刻而不进列表）
        if (plagueTreeRespawnTime >= 0f && Time.time >= plagueTreeRespawnTime)
        {
            plagueTreeRespawnTime = -1f;
            SpawnPlagueTree();
        }
    }
}
