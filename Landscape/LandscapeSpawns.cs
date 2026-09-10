using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Landscape 生成锚点组件（置于地形预制体内，随地形摆进场景，Awake 自动注册 Tool.LandscapeSpawns）。
/// **全项目唯一的地图点位来源**：守护点、防御塔（不复活）、水晶刷新位置、瘟疫树、僵尸出生点、
/// 双方开局出生点与复活备选位置均在此配置（策划案 6.1/7/8.1/17.1）。任何其它组件不得再持有地图点位数据。
/// 注：本组件是战斗逻辑的绝对前提（服务器场景也必须加载），未注册时 Tool.LandscapeSpawns 取用即报错。
/// </summary>
public class LandscapeSpawns : MonoBehaviour
{
    private void Awake()
    {
        Tool.LandscapeSpawns = this;
    }

    [Header("守护点出生点（前 3 个外围 + 最后 1 个中心）")]
    public List<Vector3> beaconSpawnPositions = new();

    [Header("水晶刷新位置列表")]
    public List<Vector3> crystalSpawnPositions = new();

    [Header("防御塔位置列表（防御塔被摧毁后不会复活）")]
    public List<Vector3> towerSpawnPositions = new();

    [Header("瘟疫树位置列表（中立争抢单位，多个候选随机取一个）")]
    public List<Vector3> plagueTreeSpawnPositions = new();

    [Header("僵尸出生点列表（道路/墓地/守护点外围，随机取一个）")]
    public List<Vector3> zombieSpawnPositions = new();

    [Header("进攻方开局出生点列表（按玩家序号轮流分配）")]
    public List<Vector3> attackSpawnPositions = new();

    [Header("防守方开局出生点列表（按玩家序号轮流分配）")]
    public List<Vector3> defenseSpawnPositions = new();

    [Header("进攻方复活备选位置列表（随机取一个）")]
    public List<Vector3> attackRevivePositions = new();

    [Header("防守方复活备选位置列表（随机取一个）")]
    public List<Vector3> defenseRevivePositions = new();

    /// <summary>取列表中的随机备选位置（空列表回退到地图中心）。</summary>
    public static Vector3 RandomOf(List<Vector3> list)
    {
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        return list[Random.Range(0, list.Count)];
    }

    /// <summary>按序号轮流取列表中的位置（开局出生点均摊用；空列表回退到地图中心）。</summary>
    public static Vector3 IndexedOf(List<Vector3> list, int index)
    {
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        return list[Mathf.Abs(index) % list.Count];
    }
}
