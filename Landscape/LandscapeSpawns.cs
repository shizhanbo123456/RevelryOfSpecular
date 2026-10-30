using System.Collections.Generic;
using UnityEngine;

public class LandscapeSpawns : MonoBehaviour
{
    [Header("地形")]
    [Tooltip("点位贴合表面用；地形被 8×8 拆分后仍指向原始地形")]
    [SerializeField] private Terrain terrain;

    [Header("地形块")]
    [Tooltip("拆分后的 64 个子地形物体；供 TerrainChunkCuller 按相机方形可见距离显隐")]
    public List<GameObject> terrainChunks = new();

    private void Awake()
    {
        Tool.LandscapeSpawns = this;
    }

    #region 运行时点位贴合（吸附到 Terrain 表面上方 0.1m）

    private void Start()
    {
        if (terrain == null)
        {
            Debug.LogError("[LandscapeSpawns] 未指定 Terrain：点位无法贴合表面，珊瑚/僵尸将无法生成。");
            return;
        }
        SnapCombatPointsToTerrain();
    }

    private void SnapCombatPointsToTerrain()
    {
        // 锚点类点位（Transform）：直接平移场景对象的 Y，X/Z 不变
        SnapTransformsToSurface(subGuardStatueSpawnPositions);
        SnapTransformToSurface(mainGuardStatueSpawnPosition);
        SnapTransformsToSurface(crystalStatueSpawnPositions);
        SnapTransformsToSurface(neutralStatueSpawnPositions);
        SnapTransformsToSurface(attackPositions);
        SnapTransformsToSurface(defensePositions);
    }

    private void SnapTransformsToSurface(List<Transform> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++) SnapTransformToSurface(list[i]);
    }

    private void SnapTransformToSurface(Transform t)
    {
        if (t == null) return;
        Vector3 origin = terrain.transform.position;
        float y = origin.y + terrain.SampleHeight(t.position) + SurfaceOffset;
        t.position = new Vector3(t.position.x, y, t.position.z);
    }

    #endregion

    [Header("副守护点")]
    [Tooltip("贤者雕像 ×3，下标即 GuardStatue.value 0~2")]
    public List<Transform> subGuardStatueSpawnPositions = new();

    [Header("主守护点")]
    [Tooltip("国王雕像 ×1")]
    public Transform mainGuardStatueSpawnPosition;

    [Header("水晶雕像位置")]
    [Tooltip("被摧毁后不会复活")]
    public List<Transform> crystalStatueSpawnPositions = new();

    [Header("中立雕像位置")]
    [Tooltip("中立争抢单位，多个候选随机取一个")]
    public List<Transform> neutralStatueSpawnPositions = new();

    [Header("进攻方出生/复活位置")]
    public List<Transform> attackPositions = new();

    [Header("防守方出生/复活位置")]
    public List<Transform> defensePositions = new();

    [Header("初始界面预览")]
    [Tooltip("进攻方角色站位；仅客户端使用，锚点必须属于地形预制体")]
    public Transform attackerPreviewPos;
    [Tooltip("防守方角色站位")]
    public Transform defenserPreviewPos;
    [Tooltip("展示相机机位")]
    public Transform cameraPreviewPos;

    [Header("Gizmos 半径")]
    [Tooltip("Transform 锚点的球半径，仅编辑期可视化")]
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

    #endregion

    #region 刷新区采样（MapSpace 合法区 + Terrain 表面）

    private const int MaxSampleAttempts = 32;

    private const float SurfaceOffset = 0.1f;

    private const float GizmoPinFactor = 2f;

    // 全图均匀取一个合法刷新点（僵尸用）
    public bool TrySampleSpawnPoint(out Vector3 result)
    {
        result = default;
        if (!CanSample) return false;

        for (int i = 0; i < MaxSampleAttempts; i++)
        {
            float x = Random.value * Landscape.MapSize;
            float z = Random.value * Landscape.MapSize;
            if (!Tool.AssetsManager.IsLegalSpawnXZ(x, z)) continue;
            result = SnapToSurface(x, z);
            return true;
        }
        return false;
    }

    // 在 center 的 [minRadius, maxRadius] 环带内均匀取一个合法刷新点（珊瑚用）
    public bool TrySampleSpawnPointInRing(Vector3 center, float minRadius, float maxRadius, out Vector3 result)
    {
        result = default;
        if (!CanSample) return false;

        float minSq = minRadius * minRadius;
        float maxSq = maxRadius * maxRadius;
        for (int i = 0; i < MaxSampleAttempts; i++)
        {
            // 半径按面积均匀（sqrt 修正），否则会明显堆在环带内侧
            float angle = Random.value * (Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Mathf.Lerp(minSq, maxSq, Random.value));
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            if (!Tool.AssetsManager.IsLegalSpawnXZ(x, z)) continue;
            result = SnapToSurface(x, z);
            return true;
        }
        return false;
    }

    // 缺地形或缺 AssetsManager 时启动自检/Start 已报过错，这里不再逐次刷日志
    private bool CanSample => terrain != null && Tool.AssetsManager != null;

    private Vector3 SnapToSurface(float x, float z)
    {
        float y = terrain.transform.position.y + terrain.SampleHeight(new Vector3(x, 0f, z)) + SurfaceOffset;
        return new Vector3(x, y, z);
    }

    #endregion

    private void OnDrawGizmos()
    {
        // Transform 锚点——共用 otherGizmoRadius
        DrawPoints(subGuardStatueSpawnPositions, new Color(0.25f, 0.85f, 0.35f), otherGizmoRadius);  // 副守护点：绿
        if (mainGuardStatueSpawnPosition != null) { Gizmos.color = new Color(1.00f, 0.85f, 0.25f); DrawPoint(mainGuardStatueSpawnPosition.position, otherGizmoRadius); } // 主守护点：金
        DrawPoints(crystalStatueSpawnPositions, new Color(1.00f, 0.35f, 0.30f), otherGizmoRadius);      // 水晶雕像：红
        DrawPoints(neutralStatueSpawnPositions, new Color(0.70f, 0.40f, 1.00f), otherGizmoRadius); // 中立雕像：紫
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

    private static void DrawPoint(Vector3 pos, float radius)
    {
        Gizmos.DrawSphere(pos, radius);
        Gizmos.DrawLine(pos, pos + Vector3.up * (radius * GizmoPinFactor));
    }
}
