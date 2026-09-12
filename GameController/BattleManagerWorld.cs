using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 战斗世界生成（partial BattleManager）：
/// 守护点/水晶/防御塔/瘟疫树开局生成、水晶被摧毁后的定时重生、夜间僵尸刷新落地。
/// 位置唯一来源：地形组件 LandscapeSpawns（策划案 6.1/7/8.1），不再回退其它组件。
/// 瘟疫树/防御塔/僵尸的攻击与行动逻辑按策划暂留空。
/// </summary>
public partial class BattleManager
{
    /// <summary>水晶重生计划（时间戳驱动，不做每帧状态推进）。</summary>
    private struct CrystalRespawn
    {
        public float time;
        public Vector3 pos;
        public int type;
    }
    private readonly List<CrystalRespawn> crystalRespawns = new();

    /// <summary>开局生成对局世界（守护点×4 / 水晶 / 防御塔 / 瘟疫树）。</summary>
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

        // 水晶：外观下标按序循环（0~11，类别 = 下标 % 4 对应 4 类武器；被摧毁后 30~60s 随机重生）
        for (int i = 0; i < spawns.crystalSpawnPositions.Count; i++)
        {
            SpawnEntity(EntityType.Crystal(i % Config.crystal_graphics_count), 1, spawns.crystalSpawnPositions[i], EntityCamp.Neutral);
        }

        // 防御塔（瘟疫孢子，不复活；锚点未赋值的槽位跳过）
        for (int i = 0; i < spawns.towerSpawnPositions.Count; i++)
        {
            var towerAnchor = spawns.towerSpawnPositions[i];
            if (towerAnchor == null) continue;
            SpawnEntity(EntityType.Tower(i), 1, towerAnchor.position, EntityCamp.Defense);
        }

        // 瘟疫树（中立争抢单位）：多个候选位置随机取一个
        SpawnEntity(EntityType.PlagueTree0, 1, LandscapeSpawns.RandomOf(spawns.plagueTreeSpawnPositions), EntityCamp.Neutral);
    }

    /// <summary>夜间刷新一只普通僵尸：出生点从地形组件随机取，外观变体随机（攻击/行动逻辑暂留空）。</summary>
    private void SpawnZombie()
    {
        var list = Tool.LandscapeSpawns.zombieSpawnPositions;
        if (list == null || list.Count == 0) return; // 未配置僵尸出生点则不刷新
        int variant = Random.Range(0, Config.zombie_variant_count);
        SpawnEntity(EntityType.Zombie(variant), 1, LandscapeSpawns.RandomOf(list), EntityCamp.Defense);
    }

    /// <summary>瘟疫树被攻占（树交互玩法实现后调用）：广播攻占事件（UI 飘字 / CD 加速表现）。</summary>
    public void NotifyPlagueTreeCaptured(ushort treeId)
    {
        Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.PlagueTreeCaptured, treeId);
    }

    /// <summary>水晶被摧毁：按原类型与原位置排 30~60s 随机重生（策划案第七章）。</summary>
    private void ScheduleCrystalRespawn(EntityData crystal)
    {
        crystalRespawns.Add(new CrystalRespawn()
        {
            time = Time.time + Random.Range(Config.crystal_respawn_min, Config.crystal_respawn_max),
            pos = crystal.transform.position,
            type = Mathf.Clamp(crystal.type.value, 0, Config.crystal_graphics_count - 1), // 重生保持同一外观/类别
        });
    }

    /// <summary>水晶重生推进（时间戳到期检查，无每帧状态计算）。</summary>
    private void TickWorldRespawn()
    {
        for (int i = crystalRespawns.Count - 1; i >= 0; i--)
        {
            if (Time.time < crystalRespawns[i].time) continue;
            var r = crystalRespawns[i];
            crystalRespawns.RemoveAt(i);
            SpawnEntity(EntityType.Crystal(r.type), 1, r.pos, EntityCamp.Neutral);
        }
    }
}
