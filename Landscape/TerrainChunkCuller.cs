using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkCuller : MonoBehaviour
{
    [Tooltip("方形可见距离（米）：相机 XZ ± 该值范围内的地形块可见")]
    public float visibleDistance = 250f;

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

    private void LateUpdate()
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
        Vector3 c = transform.position;
        Vector3 p1 = new(c.x - visibleDistance, c.y, c.z - visibleDistance);
        Vector3 p2 = new(c.x + visibleDistance, c.y, c.z - visibleDistance);
        Vector3 p3 = new(c.x + visibleDistance, c.y, c.z + visibleDistance);
        Vector3 p4 = new(c.x - visibleDistance, c.y, c.z + visibleDistance);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
#endif
}
