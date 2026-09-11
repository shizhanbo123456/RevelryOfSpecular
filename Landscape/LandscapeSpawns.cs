using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Landscape 生成锚点组件（置于地形预制体内，随地形摆进场景，Awake 自动注册 Tool.LandscapeSpawns）。
/// **全项目唯一的地图点位来源**：守护点、防御塔（不复活）、水晶刷新位置、瘟疫树、僵尸出生点、
/// 双方开局出生点与复活备选位置均在此配置（策划案 6.1/7/8.1/17.1）。任何其它组件不得再持有地图点位数据。
/// 部署形态：**服务器与客户端场景各挂一份**，使用同一套世界坐标点位——服务器侧只提供点位
/// （不含贴图与图形表现），客户端侧才是完整地形。
/// 点位列表由关卡搭建时人工配置，允许暂时为空（为空时回退地图中心 Landscape.MapCenter，不影响编译与加载）。
/// 编辑器辅助：组件右键菜单「随机生成水晶刷新点」可按 Terrain 表面高度批量生成水晶点位，详见文件末尾 #region。
/// 注：本组件是战斗逻辑的绝对前提，未注册时 Tool.LandscapeSpawns 取用即报错。
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

    [Tooltip("右键菜单「随机生成水晶刷新点」单次生成的数量；生成时会先清空 crystalSpawnPositions")]
    [Min(1)] public int crystalGenerateCount = 100;

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

    #region 点位生成（编辑器工具）

    /// <summary>点位净空半径（米）：与其它 collider、与其它已配置点位的最短距离下限。</summary>
    private const float ClearanceRadius = 0.5f;

    /// <summary>单个点位的最大尝试次数，超过则放弃该点。</summary>
    private const int MaxAttemptsPerPoint = 500;

    /// <summary>物理检测缓冲（半径 0.5m 内重叠的 collider 数，超出即视为拥挤）。</summary>
    private static readonly Collider[] s_overlapBuffer = new Collider[32];

    /// <summary>Gizmos 标记大小 = 相机距离 × 该系数（约视口高度的 1%，保证任何缩放下都可见）。</summary>
    private const float MarkerScreenFactor = 0.012f;

    /// <summary>Gizmos 标记半径下限（米）：相机贴近时避免标记糊住点位。</summary>
    private const float MarkerMinRadius = 0.15f;

    /// <summary>Gizmos 标记半径上限（米）：相机远离时避免标记铺满屏幕 / 连成一片。</summary>
    private const float MarkerMaxRadius = 8f;

    /// <summary>
    /// 组件右键菜单：清空后按「越靠 Terrain 中心密度越高」随机重建水晶刷新点。
    /// 每个点位的高度取自 Terrain 表面；并保证其周围 0.5m 内没有其它 collider、且不与其它已配置点位重叠。
    /// </summary>
    [ContextMenu("随机生成水晶刷新点（清空后重建）")]
    public void GenerateCrystalSpawnPositions()
    {
        var terrain = FindTerrain();
        if (terrain == null)
        {
            Debug.LogError("[LandscapeSpawns] 场景中找不到可用的 Terrain（含 TerrainData），无法生成水晶刷新点。");
            return;
        }

        crystalSpawnPositions.Clear();

        int failed = 0;
        for (int i = 0; i < crystalGenerateCount; i++)
        {
            if (TryFindFreePointOnTerrain(terrain, out Vector3 pos)) crystalSpawnPositions.Add(pos);
            else failed++;
        }

        if (failed > 0)
        {
            Debug.LogWarning($"[LandscapeSpawns] 水晶刷新点：已生成 {crystalSpawnPositions.Count} 个，{failed} 个因找不到净空位置而放弃。");
        }
        else
        {
            Debug.Log($"[LandscapeSpawns] 水晶刷新点：已生成 {crystalSpawnPositions.Count} 个（Terrain = {terrain.name}）。");
        }
    }

    /// <summary>宽度优先搜索场景中的 Terrain：先在本对象所属层级树内找，再遍历所有已加载场景（含 Prefab Mode）。</summary>
    private Terrain FindTerrain()
    {
        Terrain found = BreadthFirstSearch(transform != null ? transform.root : null);
        if (found != null) return found;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var roots = SceneManager.GetSceneAt(i).GetRootGameObjects();
            for (int j = 0; j < roots.Length; j++)
            {
                found = BreadthFirstSearch(roots[j].transform);
                if (found != null) return found;
            }
        }
        return null;
    }

    /// <summary>以 root 为起点做宽度优先（逐层）搜索，返回第一个带 TerrainData 的 Terrain。</summary>
    private static Terrain BreadthFirstSearch(Transform root)
    {
        if (root == null) return null;

        var queue = new Queue<Transform>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            var terrain = t.GetComponent<Terrain>();
            if (terrain != null && terrain.terrainData != null) return terrain;

            for (int i = 0; i < t.childCount; i++) queue.Enqueue(t.GetChild(i));
        }
        return null;
    }

    /// <summary>在 Terrain 上找一个净空点：XZ 随机取点（越靠中心越容易被接受）→ 采样高度 → 净空校验。</summary>
    private bool TryFindFreePointOnTerrain(Terrain terrain, out Vector3 result)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float centerX = origin.x + size.x * 0.5f;
        float centerZ = origin.z + size.z * 0.5f;
        // 中心到四角的最远距离：作为密度衰减半径，越靠外接受概率越低（四角趋近 0）
        float biasRadius = new Vector2(size.x * 0.5f, size.z * 0.5f).magnitude;

        for (int i = 0; i < MaxAttemptsPerPoint; i++)
        {
            float x = origin.x + Random.value * size.x;
            float z = origin.z + Random.value * size.z;

            // 中心密度加权：接受概率 p = 1 - d / biasRadius
            float d = new Vector2(x - centerX, z - centerZ).magnitude;
            if (Random.value > 1f - d / biasRadius) continue;

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
            || NearIn(attackSpawnPositions, pos)
            || NearIn(defenseSpawnPositions, pos)
            || NearIn(attackRevivePositions, pos)
            || NearIn(defenseRevivePositions, pos);
    }

    /// <summary>列表中是否存在与 pos 距离小于净空半径的点位。</summary>
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

    /// <summary>Gizmos：显示全部点位（点位本体 + 向上立柱 + 0.5m 真实净空范围）。</summary>
    /// <remarks>
    /// 点位坐标是**世界坐标**，Gizmos.DrawSphere/DrawLine/DrawWireSphere 默认就在世界空间绘制
    /// （Gizmos.matrix 保持单位矩阵，本文件不改动它）。
    /// 注意：标记大小按 Scene 视图相机距离自适应——地图达 1280 单位时，固定半径的球体在远景下会小到亚像素而看不见。
    /// </remarks>
    private void OnDrawGizmos()
    {
        DrawPoints(beaconSpawnPositions, new Color(0.25f, 0.85f, 0.35f));   // 守护点：绿
        DrawPoints(crystalSpawnPositions, new Color(0.25f, 0.80f, 1.00f));  // 水晶：青
        DrawPoints(towerSpawnPositions, new Color(1.00f, 0.35f, 0.30f));    // 防御塔：红
        DrawPoints(plagueTreeSpawnPositions, new Color(0.70f, 0.40f, 1.00f)); // 瘟疫树：紫
        DrawPoints(zombieSpawnPositions, new Color(1.00f, 0.65f, 0.20f));   // 僵尸：橙
        DrawPoints(attackSpawnPositions, new Color(0.35f, 0.55f, 1.00f));   // 进攻方出生：蓝
        DrawPoints(defenseSpawnPositions, new Color(0.10f, 0.90f, 0.90f));  // 防守方出生：青绿
        DrawPoints(attackRevivePositions, new Color(1.00f, 0.90f, 0.30f));  // 进攻方复活：黄
        DrawPoints(defenseRevivePositions, new Color(1.00f, 0.40f, 0.80f)); // 防守方复活：粉
    }

    /// <summary>绘制一个点位列表。</summary>
    private static void DrawPoints(List<Vector3> list, Color color)
    {
        if (list == null || list.Count == 0) return;
        Gizmos.color = color;
        for (int i = 0; i < list.Count; i++)
        {
            Vector3 p = list[i];
            float r = ResolveMarkerRadius(p);
            Gizmos.DrawSphere(p, r * 0.4f);                 // 点位本体（随缩放自适应，保证可见）
            Gizmos.DrawLine(p, p + Vector3.up * r * 2f);    // 向上立柱：远景/斜视下也容易定位
            Gizmos.DrawWireSphere(p, ClearanceRadius);      // 0.5m 真实净空范围（世界尺寸，不缩放）
        }
    }

    /// <summary>按 Scene 视图相机距离自适应标记大小，保证任意缩放级别下都可见（非编辑器环境退回固定值）。</summary>
    private static float ResolveMarkerRadius(Vector3 worldPos)
    {
#if UNITY_EDITOR
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        if (view != null && view.camera != null)
        {
            float distance = Vector3.Distance(view.camera.transform.position, worldPos);
            return Mathf.Clamp(distance * MarkerScreenFactor, MarkerMinRadius, MarkerMaxRadius);
        }
#endif
        return MarkerMinRadius;
    }

    #endregion
}
