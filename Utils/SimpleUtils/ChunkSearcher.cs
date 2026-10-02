using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkSearcher<T> : IEnumerable<T>
{
    // 特殊区块：所有不在地图范围内的物体都归于此区块
    public static readonly Vector2Int OutOfMapChunk = new Vector2Int(-1, -1);
    // 全局物体字典：id -> 物体
    private readonly Dictionary<int, T> _allObjects = new Dictionary<int, T>();
    // 区块物体字典：区块索引 -> 该区块内的物体id集合
    private readonly Dictionary<Vector2Int, HashSet<int>> _chunkObjects = new Dictionary<Vector2Int, HashSet<int>>();
    // 物体当前所在区块：位置会变，定位旧区块不能靠再读一次位置，必须记下来
    private readonly Dictionary<int, Vector2Int> _chunkOf = new Dictionary<int, Vector2Int>();
    // 获取物体位置的委托
    private readonly Func<T, Vector3> _getPosition;

    public int Count => _allObjects.Count;

    public ChunkSearcher(Func<T, Vector3> getPosition)
    {
        _getPosition = getPosition ?? throw new ArgumentNullException(nameof(getPosition));
    }

    #region 基础增删查操作
    public void Add(int id, T obj)
    {
        if (_allObjects.ContainsKey(id))
        {
            Debug.LogWarning($"物体id {id} 已存在，将覆盖原有数据");
            Remove(id);
        }
        _allObjects.Add(id, obj);
        Vector3 pos = _getPosition(obj);
        Vector2Int chunkIndex = GetChunkIndex(pos);
        AddToChunk(chunkIndex, id);
        _chunkOf[id] = chunkIndex;
    }

    public bool Remove(int id)
    {
        if (!_allObjects.TryGetValue(id, out T obj))
            return false;
        // 用记录下来的区块：物体可能已经移动，按当前位置反推会从错误的区块里删（旧区块残留脏 id）
        Vector2Int chunkIndex = _chunkOf.TryGetValue(id, out var tracked)
            ? tracked
            : GetChunkIndex(_getPosition(obj));
        RemoveFromChunk(chunkIndex, id);
        _chunkOf.Remove(id);
        return _allObjects.Remove(id);
    }

    public bool Contains(int id)
    {
        return _allObjects.ContainsKey(id);
    }

    public bool TryGetObject(int id, out T obj)
    {
        return _allObjects.TryGetValue(id, out obj);
    }

    public void Clear()
    {
        _allObjects.Clear();
        _chunkObjects.Clear();
        _chunkOf.Clear();
    }
    #endregion

    #region 区块查询（含特殊区块支持）
    public void GetObjectsInChunk(Vector2Int chunkIndex, HashSet<T> outList, bool append = false)
    {
        ValidateChunkIndex(chunkIndex);
        if (!append) outList.Clear();
        if (!_chunkObjects.TryGetValue(chunkIndex, out HashSet<int> objectIds))
            return;

        foreach (int id in objectIds)
        {
            if (_allObjects.TryGetValue(id, out T obj))
            {
                outList.Add(obj);
            }
        }
    }

    public void GetObjectsInChunk(int chunkX, int chunkY, HashSet<T> outList, bool append = false)
    {
        GetObjectsInChunk(new Vector2Int(chunkX, chunkY), outList, append);
    }

    public void GetObjectIdsInChunk(Vector2Int chunkIndex, HashSet<int> outIdSet, bool append = false)
    {
        ValidateChunkIndex(chunkIndex);
        if (!append) outIdSet.Clear();
        if (!_chunkObjects.TryGetValue(chunkIndex, out HashSet<int> objectIds))
            return;

        foreach (int id in objectIds)
        {
            outIdSet.Add(id);
        }
    }

    public void GetObjectIdsInChunk(int chunkX, int chunkY, HashSet<int> outIdSet, bool append = false)
    {
        GetObjectIdsInChunk(new Vector2Int(chunkX, chunkY), outIdSet, append);
    }

    public void GetOutOfMapObjects(HashSet<T> outList, bool append = false)
    {
        GetObjectsInChunk(OutOfMapChunk, outList, append);
    }

    public void GetOutOfMapObjectIds(HashSet<int> outIdSet, bool append = false)
    {
        GetObjectIdsInChunk(OutOfMapChunk, outIdSet, append);
    }
    #endregion

    #region 范围查询（自动排除地图外物体）
    public void GetIdsInRange(Vector3 center, float radius, HashSet<int> outIdSet, bool append = false)
    {
        if (!append) outIdSet.Clear();
        float radiusSquared = radius * radius;

        Vector3 minPoint = center - new Vector3(radius, 0, radius);
        Vector3 maxPoint = center + new Vector3(radius, 0, radius);

        int minChunkX = Mathf.Max(0, Mathf.FloorToInt(minPoint.x / Landscape.chunkSize));
        int minChunkZ = Mathf.Max(0, Mathf.FloorToInt(minPoint.z / Landscape.chunkSize));
        int maxChunkX = Mathf.Min(Landscape.chunkCount - 1, Mathf.FloorToInt(maxPoint.x / Landscape.chunkSize));
        int maxChunkZ = Mathf.Min(Landscape.chunkCount - 1, Mathf.FloorToInt(maxPoint.z / Landscape.chunkSize));

        for (int x = minChunkX; x <= maxChunkX; x++)
        {
            for (int z = minChunkZ; z <= maxChunkZ; z++)
            {
                Vector2Int chunkIndex = new Vector2Int(x, z);
                if (!_chunkObjects.TryGetValue(chunkIndex, out HashSet<int> objectIds))
                    continue;

                foreach (int id in objectIds)
                {
                    if (!_allObjects.TryGetValue(id, out T obj))
                        continue;

                    Vector3 objPos = _getPosition(obj);
                    float distSq = (objPos.x - center.x) * (objPos.x - center.x) +
                                   (objPos.z - center.z) * (objPos.z - center.z);
                    if (distSq <= radiusSquared)
                    {
                        outIdSet.Add(id);
                    }
                }
            }
        }
    }

    public void GetIdsInRelativeBlocks(Vector3 center, float radius, HashSet<int> outIdSet, bool append = false)
    {
        if (!append) outIdSet.Clear();

        Vector3 minPoint = center - new Vector3(radius, 0, radius);
        Vector3 maxPoint = center + new Vector3(radius, 0, radius);

        int minChunkX = Mathf.Max(0, Mathf.FloorToInt(minPoint.x / Landscape.chunkSize));
        int minChunkZ = Mathf.Max(0, Mathf.FloorToInt(minPoint.z / Landscape.chunkSize));
        int maxChunkX = Mathf.Min(Landscape.chunkCount - 1, Mathf.FloorToInt(maxPoint.x / Landscape.chunkSize));
        int maxChunkZ = Mathf.Min(Landscape.chunkCount - 1, Mathf.FloorToInt(maxPoint.z / Landscape.chunkSize));

        for (int x = minChunkX; x <= maxChunkX; x++)
        {
            for (int z = minChunkZ; z <= maxChunkZ; z++)
            {
                Vector2Int chunkIndex = new Vector2Int(x, z);
                if (!_chunkObjects.TryGetValue(chunkIndex, out HashSet<int> objectIds))
                    continue;

                foreach (int id in objectIds)
                {
                    outIdSet.Add(id);
                }
            }
        }
    }

    public static void GetAllRelativeBlocks(Vector3 center, float radius, HashSet<Vector2Int> outChunkSet, bool append = false)
    {
        if (!append) outChunkSet.Clear();

        Vector3 minPoint = center - new Vector3(radius, 0, radius);
        Vector3 maxPoint = center + new Vector3(radius, 0, radius);

        int minChunkX = Mathf.Max(0, Mathf.FloorToInt(minPoint.x / Landscape.chunkSize));
        int minChunkZ = Mathf.Max(0, Mathf.FloorToInt(minPoint.z / Landscape.chunkSize));
        int maxChunkX = Mathf.Min(Landscape.chunkCount - 1, Mathf.FloorToInt(maxPoint.x / Landscape.chunkSize));
        int maxChunkZ = Mathf.Min(Landscape.chunkCount - 1, Mathf.FloorToInt(maxPoint.z / Landscape.chunkSize));

        for (int x = minChunkX; x <= maxChunkX; x++)
        {
            for (int z = minChunkZ; z <= maxChunkZ; z++)
            {
                Vector2Int chunkIndex = new Vector2Int(x, z);
                outChunkSet.Add(chunkIndex);
            }
        }
    }
    #endregion

    #region 物体移动更新
    public bool UpdateObjectPosition(int id)
    {
        if (!_allObjects.TryGetValue(id, out T obj))
            return false;

        Vector2Int chunk = GetChunkIndex(_getPosition(obj));
        if (_chunkOf.TryGetValue(id, out var tracked))
        {
            if (tracked == chunk) return true; // 还在同一区块
            RemoveFromChunk(tracked, id);
        }
        AddToChunk(chunk, id);
        _chunkOf[id] = chunk;
        return true;
    }

    public void RefreshAll()
    {
        _chunkObjects.Clear();
        _chunkOf.Clear();
        foreach (var kv in _allObjects)
        {
            int id = kv.Key;
            T obj = kv.Value;
            Vector3 pos = _getPosition(obj);
            Vector2Int chunk = GetChunkIndex(pos);
            AddToChunk(chunk, id);
            _chunkOf[id] = chunk;
        }
    }
    #endregion

    #region 边界检查工具方法
    public bool IsValidChunk(Vector2Int chunkIndex)
    {
        return chunkIndex.x >= 0 && chunkIndex.x < Landscape.chunkCount &&
               chunkIndex.y >= 0 && chunkIndex.y < Landscape.chunkCount;
    }

    public bool IsPositionInMap(Vector3 worldPos)
    {
        float mapMaxRange = Landscape.chunkCount * Landscape.chunkSize;
        return worldPos.x >= 0 && worldPos.x < mapMaxRange &&
               worldPos.z >= 0 && worldPos.z < mapMaxRange;
    }

    private void ValidateChunkIndex(Vector2Int chunkIndex)
    {
#if !RELEASE
        // 特殊地图外区块允许
        if (chunkIndex == OutOfMapChunk)
            return;

        bool valid = IsValidChunk(chunkIndex);
        if (!valid)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkIndex),
                $"区块索引越界！Chunk:{chunkIndex}，地图区块总数:{Landscape.chunkCount}，合法范围 [0, {Landscape.chunkCount - 1}]"
            );
        }
#endif
    }

    public static Vector2Int GetChunkIndex(Vector3 worldPos)
    {
        float mapMaxRange = Landscape.chunkCount * Landscape.chunkSize;
        if (worldPos.x < 0 || worldPos.x >= mapMaxRange ||
            worldPos.z < 0 || worldPos.z >= mapMaxRange)
        {
            return OutOfMapChunk;
        }
        int x = Mathf.FloorToInt(worldPos.x / Landscape.chunkSize);
        int z = Mathf.FloorToInt(worldPos.z / Landscape.chunkSize);
        return new Vector2Int(x, z);
    }
    #endregion

    #region 内部工具方法
    private void AddToChunk(Vector2Int chunkIndex, int id)
    {
        if (!_chunkObjects.TryGetValue(chunkIndex, out HashSet<int> idSet))
        {
            idSet = new HashSet<int>();
            _chunkObjects.Add(chunkIndex, idSet);
        }
        idSet.Add(id);
    }

    private void RemoveFromChunk(Vector2Int chunkIndex, int id)
    {
        if (_chunkObjects.TryGetValue(chunkIndex, out HashSet<int> idSet))
        {
            idSet.Remove(id);
            if (idSet.Count == 0)
            {
                _chunkObjects.Remove(chunkIndex);
            }
        }
    }
    #endregion

    // 索引器
    public T this[int index]
    {
        get { return _allObjects[index]; }
        set { _allObjects[index] = value; }
    }

    public Dictionary<int, T>.KeyCollection Keys => _allObjects.Keys;
    public Dictionary<int, T>.ValueCollection Values => _allObjects.Values;

    public IEnumerator<T> GetEnumerator()
    {
        return _allObjects.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _allObjects.Values.GetEnumerator();
    }
}