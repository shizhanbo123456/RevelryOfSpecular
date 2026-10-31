using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkCuller : MonoBehaviour
{
    [Tooltip("方形可见距离（米）：相机 XZ ± 该值范围内的地形块可见")]
    public float visibleDistance = 250f;

    [Tooltip("方形可见距离（米）：相机 XZ ± 该值范围内的场景物体可见")]
    public float objectVisibleDistance = 250f;

    // 相机位移小于 0.01m 视为没动，跳过整段判定
    private const float MoveEpsilonSq = 0.0001f;

    private struct ChunkInfo
    {
        public GameObject go;
        public Vector3 minXZ; // 块 XZ 包围盒最小角（世界坐标）
        public Vector3 maxXZ; // 块 XZ 包围盒最大角（世界坐标）
        public bool active;   // 当前显隐状态（避免每帧 SetActive）
    }

    private readonly List<ChunkInfo> chunkInfos = new();
    private List<GameObject> cachedSource;      // 上次构建缓存时的源列表引用（换列表即重建）
    private int cachedChunkCount = -1;

    // 场景物体：按 40m 区块建索引，每帧只比对区块集合差异，不逐物体遍历
    private ChunkSearcher<GameObject> objectChunks;
    private List<GameObject> cachedObjectSource;
    private int cachedObjectCount = -1;
    private readonly HashSet<Vector2Int> insideChunks = new();     // 完全落在可见方形内的区块
    private readonly HashSet<Vector2Int> edgeChunks = new();       // 与可见方形相交的边界区块
    private readonly HashSet<Vector2Int> nextInsideChunks = new();
    private readonly HashSet<Vector2Int> nextEdgeChunks = new();
    private readonly HashSet<Vector2Int> scratchChunks = new();
    private readonly HashSet<int> scratchIds = new();
    private Vector3 lastCamPos;
    private float lastObjectDistance = float.NaN;
    private bool objectsDirty = true;                               // 重建/OnEnable 后需整体同步一次

    private void OnEnable()
    {
        objectsDirty = true;
    }

    private void LateUpdate()
    {
        UpdateTerrainChunks();
        UpdateSceneObjects();
    }

    private void UpdateTerrainChunks()
    {
        var source = Tool.LandscapeSpawns != null ? Tool.LandscapeSpawns.terrainChunks : null;
        if (source == null) return;

        if (!ReferenceEquals(source, cachedSource) || source.Count != cachedChunkCount || !CachesValid())
        {
            RebuildCache(source);
        }

        float camX = transform.position.x;
        float camZ = transform.position.z;

        for (int i = 0; i < chunkInfos.Count; i++)
        {
            var info = chunkInfos[i];
            // 方形可见判定：块的 XZ 包围盒与 [camX±d, camZ±d] 方形相交
            bool shouldShow =
                info.maxXZ.x >= camX - visibleDistance && info.minXZ.x <= camX + visibleDistance &&
                info.maxXZ.z >= camZ - visibleDistance && info.minXZ.z <= camZ + visibleDistance;

            if (info.active == shouldShow) continue; // 状态没变绝不动 GameObject
            info.active = shouldShow;
            chunkInfos[i] = info;
            if (info.go != null) info.go.SetActive(shouldShow);
        }
    }

    private void UpdateSceneObjects()
    {
        var spawns = Tool.LandscapeSpawns;
        var source = spawns != null ? spawns.sceneObjects : null;
        if (source == null || source.Count == 0)
        {
            ClearObjectState();
            return;
        }

        if (!ReferenceEquals(source, cachedObjectSource) || source.Count != cachedObjectCount)
            RebuildObjectIndex(source);

        Vector3 cam = transform.position;
        bool moved = (cam - lastCamPos).sqrMagnitude > MoveEpsilonSq;
        bool distanceChanged = !Mathf.Approximately(objectVisibleDistance, lastObjectDistance);
        if (!moved && !distanceChanged && !objectsDirty) return;

        lastCamPos = cam;
        lastObjectDistance = objectVisibleDistance;

        if (objectsDirty)
        {
            ResyncAllObjects(cam);
            objectsDirty = false;
            return;
        }

        ApplyChunkDiff(cam);
    }

    #region 场景物体：区块索引与显隐

    private void ClearObjectState()
    {
        if (objectChunks == null && cachedObjectSource == null) return;
        objectChunks = null;
        cachedObjectSource = null;
        cachedObjectCount = -1;
        insideChunks.Clear();
        edgeChunks.Clear();
        nextInsideChunks.Clear();
        nextEdgeChunks.Clear();
        objectsDirty = true;
    }

    private void RebuildObjectIndex(List<GameObject> source)
    {
        objectChunks = new ChunkSearcher<GameObject>(go => go.transform.position);
        for (int i = 0; i < source.Count; i++)
        {
            var go = source[i];
            if (go == null) continue;
            objectChunks.Add(go.GetInstanceID(), go);
        }
        cachedObjectSource = source;
        cachedObjectCount = source.Count;
        insideChunks.Clear();
        edgeChunks.Clear();
        objectsDirty = true;
    }

    // 按当前相机位置把可见方形覆盖到的区块分成「完全包含」与「边界」两类
    private void BuildChunkSets(Vector3 cam)
    {
        nextInsideChunks.Clear();
        nextEdgeChunks.Clear();
        ChunkSearcher<GameObject>.GetAllRelativeBlocks(cam, objectVisibleDistance, scratchChunks);

        float minX = cam.x - objectVisibleDistance;
        float maxX = cam.x + objectVisibleDistance;
        float minZ = cam.z - objectVisibleDistance;
        float maxZ = cam.z + objectVisibleDistance;

        foreach (Vector2Int chunk in scratchChunks)
        {
            float chunkMinX = chunk.x * Landscape.chunkSize;
            float chunkMinZ = chunk.y * Landscape.chunkSize;
            float chunkMaxX = chunkMinX + Landscape.chunkSize;
            float chunkMaxZ = chunkMinZ + Landscape.chunkSize;

            if (chunkMinX >= minX && chunkMaxX <= maxX && chunkMinZ >= minZ && chunkMaxZ <= maxZ)
                nextInsideChunks.Add(chunk);
            else
                nextEdgeChunks.Add(chunk);
        }
    }

    private void ApplyChunkDiff(Vector3 cam)
    {
        BuildChunkSets(cam);

        // 新进入（含由边界升为完全包含）的区块：整块显示
        foreach (Vector2Int chunk in nextInsideChunks)
            if (!insideChunks.Contains(chunk)) SetChunkActive(chunk, true);

        // 完全离开可见范围的区块：整块隐藏
        foreach (Vector2Int chunk in insideChunks)
            if (!nextInsideChunks.Contains(chunk) && !nextEdgeChunks.Contains(chunk)) SetChunkActive(chunk, false);
        foreach (Vector2Int chunk in edgeChunks)
            if (!nextInsideChunks.Contains(chunk) && !nextEdgeChunks.Contains(chunk)) SetChunkActive(chunk, false);

        // 边界区块内逐个物体做方形距离判定
        foreach (Vector2Int chunk in nextEdgeChunks) ApplyEdgeChunk(chunk, cam);

        insideChunks.Clear();
        insideChunks.UnionWith(nextInsideChunks);
        edgeChunks.Clear();
        edgeChunks.UnionWith(nextEdgeChunks);
    }

    private void SetChunkActive(Vector2Int chunk, bool active)
    {
        objectChunks.GetObjectIdsInChunk(chunk, scratchIds);
        foreach (int id in scratchIds)
        {
            if (!objectChunks.TryGetObject(id, out GameObject go) || go == null) continue;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }

    private void ApplyEdgeChunk(Vector2Int chunk, Vector3 cam)
    {
        float d = objectVisibleDistance;
        objectChunks.GetObjectIdsInChunk(chunk, scratchIds);
        foreach (int id in scratchIds)
        {
            if (!objectChunks.TryGetObject(id, out GameObject go) || go == null) continue;
            Vector3 pos = go.transform.position;
            bool shouldShow = Mathf.Abs(pos.x - cam.x) <= d && Mathf.Abs(pos.z - cam.z) <= d;
            if (go.activeSelf != shouldShow) go.SetActive(shouldShow);
        }
    }

    // 首次 / 重建 / 重新启用：把全部物体的状态按当前相机对一次（一次性开销，非每帧）
    private void ResyncAllObjects(Vector3 cam)
    {
        BuildChunkSets(cam);
        float d = objectVisibleDistance;

        foreach (GameObject go in objectChunks.Values)
        {
            if (go == null) continue;
            Vector3 pos = go.transform.position;
            Vector2Int chunk = ChunkSearcher<GameObject>.GetChunkIndex(pos);
            if (chunk == ChunkSearcher<GameObject>.OutOfMapChunk) continue; // 地图外物体不参与剔除

            bool shouldShow;
            if (nextInsideChunks.Contains(chunk)) shouldShow = true;
            else if (nextEdgeChunks.Contains(chunk)) shouldShow = Mathf.Abs(pos.x - cam.x) <= d && Mathf.Abs(pos.z - cam.z) <= d;
            else shouldShow = false;

            if (go.activeSelf != shouldShow) go.SetActive(shouldShow);
        }

        insideChunks.Clear();
        insideChunks.UnionWith(nextInsideChunks);
        edgeChunks.Clear();
        edgeChunks.UnionWith(nextEdgeChunks);
    }

    #endregion

    private void RebuildCache(List<GameObject> source)
    {
        chunkInfos.Clear();
        for (int i = 0; i < source.Count; i++)
        {
            var go = source[i];
            if (go == null) continue;
            var terrain = go.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning($"[TerrainChunkCuller] LandscapeSpawns.terrainChunks 第 {i} 项 '{go.name}' 没有 Terrain 组件，已跳过", go);
                continue;
            }

            // 块的世界包围盒：地形从物体位置向 +X/+Z 延展 terrainData.size（按父级缩放修正）
            Vector3 size = Vector3.Scale(terrain.terrainData.size, go.transform.lossyScale);
            Vector3 pos = go.transform.position;
            chunkInfos.Add(new ChunkInfo
            {
                go = go,
                minXZ = pos,
                maxXZ = pos + size,
                active = go.activeSelf,
            });
        }
        cachedSource = source;
        cachedChunkCount = source.Count;
    }

    private bool CachesValid()
    {
        for (int i = 0; i < chunkInfos.Count; i++)
        {
            if (chunkInfos[i].go == null) return false;
        }
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.6f);
        DrawSquare(transform.position, visibleDistance);
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.6f);
        DrawSquare(transform.position, objectVisibleDistance);
    }

    private static void DrawSquare(Vector3 c, float d)
    {
        Vector3 p1 = new(c.x - d, c.y, c.z - d);
        Vector3 p2 = new(c.x + d, c.y, c.z - d);
        Vector3 p3 = new(c.x + d, c.y, c.z + d);
        Vector3 p4 = new(c.x - d, c.y, c.z + d);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
#endif
}
