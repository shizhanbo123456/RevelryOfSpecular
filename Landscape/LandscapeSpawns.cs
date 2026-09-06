using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形组件（挂载在场景地形物体上）：记录各类单位出生/复活位置列表。
/// 策划案 6.1/7/8.1/17.1：守护点、防御塔（不复活）、水晶刷新位置与双方复活备选位置均在此配置。
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

    [Header("瘟疫树位置列表（中立争抢单位）")]
    public List<Vector3> plagueTreeSpawnPositions = new();

    [Header("进攻方复活备选位置列表")]
    public List<Vector3> attackRevivePositions = new();

    [Header("防守方复活备选位置列表")]
    public List<Vector3> defenseRevivePositions = new();

    /// <summary>取列表中的随机备选位置（空列表回退到地图中心）。</summary>
    public static Vector3 RandomOf(List<Vector3> list)
    {
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        return list[Random.Range(0, list.Count)];
    }
}
