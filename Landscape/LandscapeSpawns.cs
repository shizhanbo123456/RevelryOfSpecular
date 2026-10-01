using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Landscape 生成锚点组件（置于地形预制体内，随地形摆进场景，Awake 自动注册 Tool.LandscapeSpawns）。
/// **全项目唯一的地图点位来源**：守护点、防御塔（不复活）、水晶刷新位置、瘟疫树、僵尸出生点、
/// 双方出生/复活位置均在此配置（策划案 6.1/7/8.1/17.1）。任何其它组件不得再持有地图点位数据。
/// 存储形态：
/// ① **锚点（Transform）**——守护点、防御塔、瘟疫树、双方出生/复活位置：在场景摆空物体后拖进列表，
///    之后直接拖动对象即可调整，运行时读取其世界坐标；
/// ② **坐标（Vector3）**——水晶刷新点、僵尸出生点：由组件右键菜单按 Terrain 表面批量生成。
/// 出生与复活：双方各自的「出生/复活位置列表」是**同一个列表**，开局出生与复活都从中随机取点（不做去重）。
/// 部署形态：**服务器与客户端场景各挂一份**，使用同一套锚点——服务器侧只提供点位（不含贴图与图形表现），
/// 因此锚点对象必须是地形预制体的一部分，否则服务器那份引用会丢失。
/// 点位列表允许暂时为空（为空时回退地图中心 Landscape.MapCenter，不影响编译与加载）。
/// 注：本组件是战斗逻辑的绝对前提，未注册时 Tool.LandscapeSpawns 取用即报错。
/// </summary>
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

    /// <summary>
    /// 运行时将所有战斗相关点位吸附到 Terrain 表面上方 SurfaceOffset：仅调整 Y，X/Z 不变。
    /// 覆盖：水晶刷新点/僵尸出生点（Vector3 列表）与 守护点/防御塔/瘟疫树/进攻方出生-复活/防守方出生-复活（Transform 锚点）。
    /// 初始界面预览锚点（attackerPreviewPos / defenserPreviewPos / cameraPreviewPos）非战斗刷新点，不在此处理。
    /// </summary>
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

    /// <summary>将坐标列表每个点的 Y 吸附到 Terrain 表面上方 SurfaceOffset，X/Z 保持原值。</summary>
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

    /// <summary>将锚点列表中每个 Transform 的 Y 吸附到 Terrain 表面上方 SurfaceOffset，X/Z 保持原值（跳过空位）。</summary>
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

    /// <summary>取锚点列表中的随机位置（跳过未赋值的空位；空列表回退到地图中心）。</summary>
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

    /// <summary>取坐标列表中的随机位置（空列表回退到地图中心）。</summary>
    public static Vector3 RandomOf(List<Vector3> list)
    {
        if (list == null || list.Count == 0) return Landscape.MapCenter;
        return list[Random.Range(0, list.Count)];
    }

    #endregion

    #region 点位生成（编辑器工具）

    /// <summary>点位净空半径（米）：与其它 collider、与其它已配置点位的最短距离下限。</summary>
    private const float ClearanceRadius = 0.5f;

    /// <summary>战斗相关坐标点位（水晶刷新点 + 僵尸出生点）在运行时吸附到 Terrain 表面上方时的额外抬升高度（米），避免陷入地表。仅影响 Y，不改变 X/Z。</summary>
    private const float SurfaceOffset = 0.1f;

    /// <summary>单个点位的最大尝试次数，超过则放弃该点。</summary>
    private const int MaxAttemptsPerPoint = 500;

    /// <summary>Gizmos 向上立柱长度 = 该类型的球半径 × 该系数（半径 1 时为 2m），便于远景/斜视定位。</summary>
    private const float GizmoPinFactor = 2f;

    /// <summary>物理检测缓冲（半径 0.5m 内重叠的 collider 数，超出即视为拥挤）。</summary>
    private static readonly Collider[] s_overlapBuffer = new Collider[32];

    /// <summary>随机取点的密度偏向。</summary>
    private enum DensityBias
    {
        /// <summary>越靠 Terrain 中心越密（水晶刷新点）。</summary>
        Center,

        /// <summary>越靠 Terrain 边界越密（僵尸出生点）。</summary>
        Boundary,
    }

    /// <summary>组件右键菜单：清空后按「越靠 Terrain 中心密度越高」重建水晶刷新点。</summary>
    [ContextMenu("随机生成水晶刷新点（清空后重建）")]
    public void GenerateCrystalSpawnPositions()
    {
        GeneratePointsOnTerrain(crystalSpawnPositions, crystalGenerateCount, DensityBias.Center, "水晶刷新点");
    }

    /// <summary>组件右键菜单：清空后按「越靠 Terrain 边界密度越高」重建僵尸出生点。</summary>
    [ContextMenu("随机生成僵尸出生点（清空后重建）")]
    public void GenerateZombieSpawnPositions()
    {
        GeneratePointsOnTerrain(zombieSpawnPositions, zombieGenerateCount, DensityBias.Boundary, "僵尸出生点");
    }

    /// <summary>清空目标列表后，在 Terrain 表面按指定密度偏向重新生成 count 个净空点位。</summary>
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

    /// <summary>在 Terrain 上找一个净空点：随机取 XZ → 按密度偏向决定是否接受 → 采样高度 → 净空校验。</summary>
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

    /// <summary>
    /// 密度偏向给出的接受概率（0~1）：
    /// Center 越靠 Terrain 中心越接近 1（衰减半径 = 中心到四角的最远距离）；
    /// Boundary 越靠 Terrain 边界越接近 1（到最近边的距离 / 短边一半）。
    /// </summary>
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

    /// <summary>净空校验：半径 0.5m 内无其它 collider（Terrain 自身不计），且不与任何已配置点位重叠。</summary>
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

    /// <summary>是否与任意列表中的已配置点位重叠（距离 &lt; 0.5m）。</summary>
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

    /// <summary>锚点列表中是否存在与 pos 距离小于净空半径的点位（跳过空位）。</summary>
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

    /// <summary>坐标列表中是否存在与 pos 距离小于净空半径的点位。</summary>
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

    /// <summary>
    /// Gizmos：为每组点位绘制颜色互不相同的球 + 一条向上立柱（便于远景/斜视定位）。
    /// 半径分两类：**菜单生成的坐标点位（水晶 + 僵尸）共用 crystalGizmoRadius**，
    /// **其余 Transform 锚点用 otherGizmoRadius**（均可在 Inspector 调）。
    /// 点位均为**世界坐标**：锚点取 Transform.position，水晶/僵尸列表本身即世界坐标。
    /// 注意：在能看全 1280 单位地图的缩放下，半径 1 的球直径约 1.6 像素，需要放近观察或调大半径。
    /// </summary>
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

    /// <summary>绘制锚点列表（跳过空位）。</summary>
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

    /// <summary>绘制坐标列表。</summary>
    private static void DrawPoints(List<Vector3> list, Color color, float radius)
    {
        if (list == null || list.Count == 0) return;
        Gizmos.color = color;
        for (int i = 0; i < list.Count; i++) DrawPoint(list[i], radius);
    }

    /// <summary>单个点位：半径 radius 的球 + 高度为 radius × 2 的向上立柱。</summary>
    private static void DrawPoint(Vector3 pos, float radius)
    {
        Gizmos.DrawSphere(pos, radius);
        Gizmos.DrawLine(pos, pos + Vector3.up * (radius * GizmoPinFactor));
    }

    #endregion
}
