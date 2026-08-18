using System.Collections.Generic;
using UnityEngine;
public class Landscape : MonoBehaviour
{
    public static Landscape instance;
    public const int chunkCount = 15;
    public const int chunkSize = 20;
    public List<Transform> SpawnPosList;
    public List<Transform> ExitPortList;
    public List<Transform> CharacterUnlockPropPos;
    [Header("Gizmos绘制总开关")]
    [Tooltip("是否绘制各类随机生成锚点（植物/矿石/感染/僵尸）")]
    public bool drawSpawnAnchorGizmos = true;
    [Tooltip("是否绘制上方5个Transform点位列表")]
    public bool drawTransformListGizmos = true;
    [Header("Transform点位Gizmos配色")]
    public Color spawnPosColor = Color.cyan;
    public Color exitPortColor = Color.blue;
    public Color unlockPropColor = Color.yellow;
    public float transformGizmosRadius = 0.6f;
    [Header("生成数量配置（Inspector可调）")]
    public int plantSpawnCount = 200;
    public int oreSpawnCount = 100;
    public int infectionSpawnCount = 30;
    public int zombieSpawnCount = 150;
    [Tooltip("单次生成最大随机重试次数，防止死循环")]
    [SerializeField] private int maxGenerateLoop = 1000;
    [Header("生成锚点集合")]
    public List<Vector3> PlantSpawnAnchors = new List<Vector3>();
    public List<Vector3> OreSpawnAnchors = new List<Vector3>();
    public List<Vector3> InfectionSpawnAnchors = new List<Vector3>();
    public List<Vector3> ZombieSpawnAnchors = new List<Vector3>();
    [Header("地形掩码贴图规则")]
    [SerializeField] private Texture2D maskTexture;
    // R通道：区块禁区，四角任一R>0.5 → 整个区块禁用
    // B通道：点位精细过滤，采样点B>0.5 → 当前坐标不能生成锚点
    [Header("Gizmos 绘制设置")]
    [SerializeField] private float gizmosSphereRadius = 0.5f;
    [SerializeField] private Color plantColor = Color.green;
    [SerializeField] private Color oreColor = Color.yellow;
    [SerializeField] private Color infectionColor = Color.red;
    [SerializeField] private Color zombieColor = new Color(0.6f, 0.2f, 0.8f);
    [SerializeField] private Color invalidChunkColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
    [Header("地形配置")]
    [SerializeField] private Terrain activeTerrain;
    // 双缓存：无效区块 + 有效区块
    public HashSet<Vector2Int> _invalidChunkCache = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> _validChunkCache = new HashSet<Vector2Int>();
    private float _mapTotalSize => chunkCount * chunkSize;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    #region 坐标转换 世界坐标 ↔ 贴图UV(0~1) ↔ 区块索引
    private Vector2Int WorldToChunkIndex(Vector3 worldPos)
    {
        float offsetX = worldPos.x;
        float offsetZ = worldPos.z;
        int cx = Mathf.FloorToInt(offsetX / chunkSize);
        int cz = Mathf.FloorToInt(offsetZ / chunkSize);
        return new Vector2Int(cx, cz);
    }
    /// <summary>世界XZ转贴图UV 0~1</summary>
    private Vector2 WorldToUV(float worldX, float worldZ)
    {
        float localX = worldX;
        float localZ = worldZ;
        float u = Mathf.Clamp01(localX / _mapTotalSize);
        float v = Mathf.Clamp01(localZ / _mapTotalSize);
        return new Vector2(u, v);
    }
    /// <summary>获取一个区块四个角的UV坐标</summary>
    private Vector2[] GetChunkFourCornerUV(Vector2Int chunkIdx)
    {
        float chunkWorldX = chunkIdx.x * chunkSize;
        float chunkWorldZ = chunkIdx.y * chunkSize;
        Vector2 uv00 = WorldToUV(chunkWorldX, chunkWorldZ);
        Vector2 uv10 = WorldToUV(chunkWorldX + chunkSize, chunkWorldZ);
        Vector2 uv01 = WorldToUV(chunkWorldX, chunkWorldZ + chunkSize);
        Vector2 uv11 = WorldToUV(chunkWorldX + chunkSize, chunkWorldZ + chunkSize);
        return new[] { uv00, uv10, uv01, uv11 };
    }
    #endregion
    #region 掩码贴图采样判断 & 双向区块缓存
    /// <summary>判断区块是否整体无效：四角任一R>0.5</summary>
    private bool IsChunkWholeInvalid(Vector2Int chunkIdx)
    {
        if (maskTexture == null) return false;
        Vector2[] corners = GetChunkFourCornerUV(chunkIdx);
        foreach (var uv in corners)
        {
            Color pixel = maskTexture.GetPixelBilinear(uv.x, uv.y);
            if (pixel.r > 0.5f)
                return true;
        }
        return false;
    }
    /// <summary>单个世界点位精细过滤：该点B>0.5则不能生成锚点</summary>
    private bool IsPointMaskBlocked(float worldX, float worldZ)
    {
        if (maskTexture == null) return false;
        Vector2 uv = WorldToUV(worldX, worldZ);
        Color pixel = maskTexture.GetPixelBilinear(uv.x, uv.y);
        return pixel.b > 0.5f;
    }
    /// <summary>一次性刷新有效、无效双区块缓存</summary>
    public void RefreshChunkCache()
    {
        _invalidChunkCache.Clear();
        _validChunkCache.Clear();
        if (maskTexture == null)
        {
            // 无贴图时全部视为有效区块
            for (int cx = 0; cx < chunkCount; cx++)
            {
                for (int cz = 0; cz < chunkCount; cz++)
                {
                    _validChunkCache.Add(new Vector2Int(cx, cz));
                }
            }
            return;
        }
        for (int cx = 0; cx < chunkCount; cx++)
        {
            for (int cz = 0; cz < chunkCount; cz++)
            {
                Vector2Int idx = new Vector2Int(cx, cz);
                if (IsChunkWholeInvalid(idx))
                {
                    _invalidChunkCache.Add(idx);
                }
                else
                {
                    _validChunkCache.Add(idx);
                }
            }
        }
    }
    /// <summary>快速查询区块是否整体禁用</summary>
    public bool IsChunkInvalidFast(Vector2Int chunkIdx)
    {
        return _invalidChunkCache.Contains(chunkIdx);
    }
    public bool IsChunkValidFast(Vector2Int chunkIdx)
    {
        return _validChunkCache.Contains(chunkIdx);
    }
    #endregion
    #region 地形高度采样
    private float GetTerrainHeight(float worldX, float worldZ)
    {
        if (activeTerrain == null) return 0f;
        float localH = activeTerrain.SampleHeight(new Vector3(worldX, 0, worldZ));
        return localH + activeTerrain.transform.position.y;
    }
    #endregion
    #region 锚点生成（带最大循环防死锁）
    /// <summary>
    /// 生成指定数量锚点，带最大循环限制防止死循环
    /// </summary>
    private void GenerateRandomAnchors(List<Vector3> targetList, int targetCount)
    {
        targetList.Clear();
        RefreshChunkCache();
        float minX = 0;
        float minZ = 0;
        float maxX = minX + _mapTotalSize;
        float maxZ = minZ + _mapTotalSize;
        int loopTimes = 0;
        // 填充目标数量 + 限制最大循环次数
        while (targetList.Count < targetCount && loopTimes < maxGenerateLoop)
        {
            loopTimes++;
            float randX = Random.Range(minX, maxX);
            float randZ = Random.Range(minZ, maxZ);
            // 第一层：区块整体禁用直接跳过
            Vector2Int chunk = WorldToChunkIndex(new Vector3(randX, 0, randZ));
            if (IsChunkInvalidFast(chunk))
                continue;
            // 第二层：点位精细B通道过滤
            if (IsPointMaskBlocked(randX, randZ))
                continue;
            // 双校验通过
            float terrainY = GetTerrainHeight(randX, randZ);
            targetList.Add(new Vector3(randX, terrainY, randZ));
        }
        // 循环耗尽仍没凑够数量给出警告
        if (targetList.Count < targetCount)
        {
            Debug.LogWarning($"生成锚点数量不足！目标:{targetCount} 实际:{targetList.Count} 最大循环{maxGenerateLoop}已用尽，可用区块过少或点位过滤太强");
        }
    }
    #endregion
    #region 编辑器菜单
    [ContextMenu("生成所有随机锚点")]
    public void GenerateAllSpawnAnchors()
    {
        GenerateRandomAnchors(PlantSpawnAnchors, plantSpawnCount);
        GenerateRandomAnchors(OreSpawnAnchors, oreSpawnCount);
        GenerateRandomAnchors(InfectionSpawnAnchors, infectionSpawnCount);
        GenerateRandomAnchors(ZombieSpawnAnchors, zombieSpawnCount);
        Debug.Log($"锚点生成汇总:\n植物:{PlantSpawnAnchors.Count}\n矿石:{OreSpawnAnchors.Count}\n感染:{InfectionSpawnAnchors.Count}\n僵尸:{ZombieSpawnAnchors.Count}");
    }
    [ContextMenu("清空所有生成锚点")]
    public void ClearAllAnchors()
    {
        PlantSpawnAnchors.Clear();
        OreSpawnAnchors.Clear();
        InfectionSpawnAnchors.Clear();
        ZombieSpawnAnchors.Clear();
        Debug.Log("所有生成锚点已清空");
    }
    [ContextMenu("手动刷新有效/无效区块双缓存")]
    public void ManualRefreshChunkCache()
    {
        RefreshChunkCache();
        Debug.Log($"缓存刷新完成 | 有效区块:{_validChunkCache.Count} 无效区块:{_invalidChunkCache.Count}");
    }
    #endregion
    #region Gizmos绘制
    private void OnDrawGizmos()
    {
        // 新增：绘制5个Transform列表点位，由 drawTransformListGizmos 控制开关
        if (drawTransformListGizmos)
        {
            // SpawnPosList
            Gizmos.color = spawnPosColor;
            foreach (var t in SpawnPosList)
            {
                if (t != null) Gizmos.DrawSphere(t.position, transformGizmosRadius);
            }
            // ExitPortList
            Gizmos.color = exitPortColor;
            foreach (var t in ExitPortList)
            {
                if (t != null) Gizmos.DrawSphere(t.position, transformGizmosRadius);
            }
            // CharacterUnlockPropPos
            Gizmos.color = unlockPropColor;
            foreach (var t in CharacterUnlockPropPos)
            {
                if (t != null) Gizmos.DrawSphere(t.position, transformGizmosRadius);
            }
        }

        // 原有随机锚点绘制，由 drawSpawnAnchorGizmos 开关控制
        if (drawSpawnAnchorGizmos)
        {
            // 植物
            Gizmos.color = plantColor;
            foreach (var pos in PlantSpawnAnchors)
                Gizmos.DrawSphere(pos, gizmosSphereRadius);
            // 矿石
            Gizmos.color = oreColor;
            foreach (var pos in OreSpawnAnchors)
                Gizmos.DrawSphere(pos, gizmosSphereRadius);
            // 感染
            Gizmos.color = infectionColor;
            foreach (var pos in InfectionSpawnAnchors)
                Gizmos.DrawSphere(pos, gizmosSphereRadius);
            // 僵尸
            Gizmos.color = zombieColor;
            foreach (var pos in ZombieSpawnAnchors)
                Gizmos.DrawSphere(pos, gizmosSphereRadius);
        }

        // 无效区块线框（不受两个开关控制，始终绘制）
        Gizmos.color = invalidChunkColor;
        foreach (var chunkIdx in _invalidChunkCache)
        {
            float worldX = chunkIdx.x * chunkSize;
            float worldZ = chunkIdx.y * chunkSize;
            Vector3 center = new Vector3(worldX + chunkSize * 0.5f, 0, worldZ + chunkSize * 0.5f);
            Vector3 size = new Vector3(chunkSize, 200, chunkSize);
            Gizmos.DrawWireCube(center, size);
        }
    }
    #endregion
}
