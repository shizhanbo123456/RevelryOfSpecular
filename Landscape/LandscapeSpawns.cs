using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[AddComponentMenu("Landscape/LandscapeSpawns")]
#endif
public class LandscapeSpawns : MonoBehaviour
{
    [Header("地形（在 Inspector 中指定；右键菜单生成水晶/僵尸点位时用于贴合表面）")]
    [SerializeField] private Terrain terrain;

    [Header("地形块列表（拆分后的 64 个子地形物体；供 TerrainChunkCuller 按相机方形可见距离显隐）")]
    public List<GameObject> terrainChunks = new();

    private void Awake()
    {
        Tool.LandscapeSpawns = this;
    }

    #region 运行时点位贴合（吸附到 Terrain 表面上方 0.1m）

    private void Start()
    {
        SnapCombatPointsToTerrain();
    }

    private void SnapCombatPointsToTerrain()
    {
        if (terrain == null)
        {
            Debug.LogWarning("[LandscapeSpawns] 未指定 Terrain，跳过战斗点位运行时贴合（保持原 Y）。");
            return;
        }
        // 坐标类点位（Vector3）
        SnapListToSurface(crystalSpawnPositions);
        SnapListToSurface(zombieSpawnPositions);
        // 锚点类点位（Transform）：直接平移场景对象的 Y，X/Z 不变
        SnapTransformsToSurface(beaconSpawnPositions);
        SnapTransformsToSurface(towerSpawnPositions);
        SnapTransformsToSurface(plagueTreeSpawnPositions);
        SnapTransformsToSurface(attackPositions);
        SnapTransformsToSurface(defensePositions);
    }

    private void SnapListToSurface(List<Vector3> list)
    {
        if (list == null || list.Count == 0) return;
        Vector3 origin = terrain.transform.position;
        for (int i = 0; i < list.Count; i++)
        {
            Vector3 p = list[i];
            float y = origin.y + terrain.SampleHeight(new Vector3(p.x, 0f, p.z)) + SurfaceOffset;
            list[i] = new Vector3(p.x, y, p.z);
        }
    }

    private void SnapTransformsToSurface(List<Transform> list)
    {
        if (list == null) return;
        Vector3 origin = terrain.transform.position;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;
            float y = origin.y + terrain.SampleHeight(t.position) + SurfaceOffset;
            t.position = new Vector3(t.position.x, y, t.position.z);
        }
    }

    #endregion

    [Header("守护点出生点（前 3 个外围 + 最后 1 个中心）")]
    public List<Transform> beaconSpawnPositions = new();

    [Header("水晶刷新位置列表（Vector3，由右键菜单生成）")]
    public List<Vector3> crystalSpawnPositions = new();

    [Tooltip("右键菜单「随机生成水晶刷新点」单次生成的数量；生成时会先清空 crystalSpawnPositions")]
    [Min(1)] public int crystalGenerateCount = 100;

    [Header("防御塔位置列表（防御塔被摧毁后不会复活）")]
    public List<Transform> towerSpawnPositions = new();

    [Header("瘟疫树位置列表（中立争抢单位，多个候选随机取一个）")]
    public List<Transform> plagueTreeSpawnPositions = new();

    [Header("僵尸出生点列表（Vector3，由右键菜单生成；道路/墓地/守护点外围，随机取一个）")]
    public List<Vector3> zombieSpawnPositions = new();

    [Tooltip("右键菜单「随机生成僵尸出生点」单次生成的数量；生成时会先清空 zombieSpawnPositions")]
    [Min(1)] public int zombieGenerateCount = 100;

    [Header("进攻方出生/复活位置列表（同一个列表，出生与复活均随机取一个）")]
    public List<Transform> attackPositions = new();

    [Header("防守方出生/复活位置列表（同一个列表，出生与复活均随机取一个）")]
    public List<Transform> defensePositions = new();

    [Header("初始界面预览（仅客户端使用）：角色站位与展示相机机位，锚点必须属于地形预制体")]
    public Transform attackerPreviewPos;
    public Transform defenserPreviewPos;
    public Transform cameraPreviewPos;

    [Header("Gizmos 半径（仅编辑期可视化，不影响运行时逻辑）")]
    [Tooltip("由菜单生成的坐标点位（水晶刷新点 + 僵尸出生点，两个 Vector3 列表共用）的 Gizmos 球半径")]
    [Min(0.1f)] public float crystalGizmoRadius = 1f;

    [Tooltip("其余 Transform 锚点（守护点/防御塔/瘟疫树/双方出生-复活位置）的 Gizmos 球半径")]
    [Min(0.1f)] public float otherGizmoRadius = 1f;

    #region 点位读取（锚点列表允许留空位，读取时自动跳过）

    public static Vector3 RandomOf(List<Transform> list)
    {
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[Random.Range(0, list.Count)];
            if (t != null) return t.position;
        }
        return Landscape.MapCenter; // 全部是空位
    }

    public static Vector3 RandomOf(List<Vector3> list)
    {
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        return list[Random.Range(0, list.Count)];
    }

    #endregion

    #region 点位生成（编辑器工具）

    private const float ClearanceRadius = 0.5f;

    private const float SurfaceOffset = 0.1f;

    private const int MaxAttemptsPerPoint = 500;

    private const float GizmoPinFactor = 2f;

    private static readonly Collider[] s_overlapBuffer = new Collider[32];

    private enum DensityBias
    {
        Center,

        Boundary,
    }

    [ContextMenu("随机生成水晶刷新点（清空后重建）")]
    public void GenerateCrystalSpawnPositions()
    {
        GeneratePointsOnTerrain(crystalSpawnPositions, crystalGenerateCount, DensityBias.Center, "水晶刷新点");
    }

    [ContextMenu("随机生成僵尸出生点（清空后重建）")]
    public void GenerateZombieSpawnPositions()
    {
        GeneratePointsOnTerrain(zombieSpawnPositions, zombieGenerateCount, DensityBias.Boundary, "僵尸出生点");
    }

    private void GeneratePointsOnTerrain(List<Vector3> target, int count, DensityBias bias, string label)
    {
        if (terrain == null)
        {
            Debug.LogError($"[LandscapeSpawns] 未在主控面板的 Inspector 中指定 Terrain，无法生成{label}。");
            return;
        }

        target.Clear();

        int failed = 0;
        for (int i = 0; i < count; i++)
        {
            if (TryFindFreePointOnTerrain(terrain, bias, out Vector3 pos)) target.Add(pos);
            else failed++;
        }

        if (failed > 0)
        {
            Debug.LogWarning($"[LandscapeSpawns] {label}：已生成 {target.Count} 个，{failed} 个因找不到净空位置而放弃（Terrain = {terrain.name}）。");
        }
        else
        {
            Debug.Log($"[LandscapeSpawns] {label}：已生成 {target.Count} 个（Terrain = {terrain.name}）。");
        }
    }

    private bool TryFindFreePointOnTerrain(Terrain terrain, DensityBias bias, out Vector3 result)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        for (int i = 0; i < MaxAttemptsPerPoint; i++)
        {
            float x = origin.x + Random.value * size.x;
            float z = origin.z + Random.value * size.z;

            if (Random.value > AcceptProbability(x, z, origin, size, bias)) continue;

            // 高度贴 Terrain 表面（SampleHeight 返回相对 Terrain 原点的高度，需加回 transform.position.y）
            float y = origin.y + terrain.SampleHeight(new Vector3(x, 0f, z));
            var pos = new Vector3(x, y, z);

            if (!IsClear(pos, terrain)) continue;

            result = pos;
            return true;
        }

        result = default;
        return false;
    }

    private static float AcceptProbability(float x, float z, Vector3 origin, Vector3 size, DensityBias bias)
    {
        float minX = origin.x, maxX = origin.x + size.x;
        float minZ = origin.z, maxZ = origin.z + size.z;

        if (bias == DensityBias.Center)
        {
            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            float radius = new Vector2(size.x * 0.5f, size.z * 0.5f).magnitude;
            return 1f - new Vector2(x - centerX, z - centerZ).magnitude / radius;
        }

        // 矩形中心处「到最近边的距离」最大，等于短边的一半，用它做归一化上限
        float halfShort = Mathf.Min(size.x, size.z) * 0.5f;
        float toEdge = Mathf.Min(Mathf.Min(x - minX, maxX - x), Mathf.Min(z - minZ, maxZ - z));
        return 1f - toEdge / halfShort;
    }

    private bool IsClear(Vector3 pos, Terrain terrain)
    {
        // 先做便宜的检查：与已配置点位的距离
        if (IsNearExistingPoint(pos)) return false;

        int count = Physics.OverlapSphereNonAlloc(pos, ClearanceRadius, s_overlapBuffer, ~0, QueryTriggerInteraction.Collide);
        if (count >= s_overlapBuffer.Length) return false; // 缓冲塞满，视为拥挤

        for (int i = 0; i < count; i++)
        {
            var col = s_overlapBuffer[i];
            if (col == null) continue;
            // Terrain 自身（含其子物体）的 collider 就是地面，不算阻挡
            if (col.transform == terrain.transform || col.transform.IsChildOf(terrain.transform)) continue;
            return false;
        }
        return true;
    }

    private bool IsNearExistingPoint(Vector3 pos)
    {
        return NearIn(beaconSpawnPositions, pos)
            || NearIn(crystalSpawnPositions, pos)
            || NearIn(towerSpawnPositions, pos)
            || NearIn(plagueTreeSpawnPositions, pos)
            || NearIn(zombieSpawnPositions, pos)
            || NearIn(attackPositions, pos)
            || NearIn(defensePositions, pos);
    }

    private static bool NearIn(List<Transform> list, Vector3 pos)
    {
        if (list == null) return false;
        float sqr = ClearanceRadius * ClearanceRadius;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;
            if ((t.position - pos).sqrMagnitude < sqr) return true;
        }
        return false;
    }

    private static bool NearIn(List<Vector3> list, Vector3 pos)
    {
        if (list == null) return false;
        float sqr = ClearanceRadius * ClearanceRadius;
        for (int i = 0; i < list.Count; i++)
        {
            if ((list[i] - pos).sqrMagnitude < sqr) return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        // 菜单生成的坐标点位（Vector3 列表）——与水晶共用 crystalGizmoRadius
        DrawPoints(crystalSpawnPositions, new Color(0.25f, 0.80f, 1.00f), crystalGizmoRadius); // 水晶：青
        DrawPoints(zombieSpawnPositions, new Color(1.00f, 0.65f, 0.20f), crystalGizmoRadius);  // 僵尸：橙

        // Transform 锚点——共用 otherGizmoRadius
        DrawPoints(beaconSpawnPositions, new Color(0.25f, 0.85f, 0.35f), otherGizmoRadius);     // 守护点：绿
        DrawPoints(towerSpawnPositions, new Color(1.00f, 0.35f, 0.30f), otherGizmoRadius);      // 防御塔：红
        DrawPoints(plagueTreeSpawnPositions, new Color(0.70f, 0.40f, 1.00f), otherGizmoRadius); // 瘟疫树：紫
        DrawPoints(attackPositions, new Color(0.35f, 0.55f, 1.00f), otherGizmoRadius);          // 进攻方出生/复活：蓝
        DrawPoints(defensePositions, new Color(0.10f, 0.90f, 0.90f), otherGizmoRadius);         // 防守方出生/复活：青绿

        // 初始界面预览锚点（单点）
        if (attackerPreviewPos != null) { Gizmos.color = new Color(1.00f, 0.45f, 0.40f); DrawPoint(attackerPreviewPos.position, otherGizmoRadius); } // 进攻预览：红
        if (defenserPreviewPos != null) { Gizmos.color = new Color(0.40f, 0.72f, 1.00f); DrawPoint(defenserPreviewPos.position, otherGizmoRadius); } // 防守预览：蓝
        if (cameraPreviewPos != null) { Gizmos.color = new Color(1.00f, 0.82f, 0.40f); DrawPoint(cameraPreviewPos.position, otherGizmoRadius); }     // 预览相机：黄
    }

    private static void DrawPoints(List<Transform> list, Color color, float radius)
    {
        if (list == null || list.Count == 0) return;
        Gizmos.color = color;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;
            DrawPoint(t.position, radius);
        }
    }

    private static void DrawPoints(List<Vector3> list, Color color, float radius)
    {
        if (list == null || list.Count == 0) return;
        Gizmos.color = color;
        for (int i = 0; i < list.Count; i++) DrawPoint(list[i], radius);
    }

    private static void DrawPoint(Vector3 pos, float radius)
    {
        Gizmos.DrawSphere(pos, radius);
        Gizmos.DrawLine(pos, pos + Vector3.up * (radius * GizmoPinFactor));
    }

    #endregion
}
