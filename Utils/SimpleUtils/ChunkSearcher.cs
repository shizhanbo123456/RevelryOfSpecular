using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用区块空间搜索器（带地图外边界处理）
/// 基于Landscape的区块划分系统，提供快速的区块查询和范围查询
/// 所有超出地图范围的物体都会被自动归类到特殊的OutOfMapChunk区块
/// </summary>
/// <typeparam name="T">要管理的物体类型</typeparam>
public class ChunkSearcher<T> : IEnumerable<T>
{
    // 特殊区块：所有不在地图范围内的物体都归于此区块
    public static readonly Vector2Int OutOfMapChunk = new Vector2Int(-1, -1);
    // 全局物体字典：id -> 物体
    private readonly Dictionary<int, T> _allObjects = new Dictionary<int, T>();
    // 区块物体字典：区块索引 -> 该区块内的物体id集合
    private readonly Dictionary<Vector2Int, HashSet<int>> _chunkObjects = new Dictionary<Vector2Int, HashSet<int>>();
    // 获取物体位置的委托
    private readonly Func<T, Vector3> _getPosition;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="getPosition">获取物体世界坐标的方法</param>
    public ChunkSearcher(Func<T, Vector3> getPosition)
    {
        _getPosition = getPosition ?? throw new ArgumentNullException(nameof(getPosition));
    }

    #region 基础增删查操作
    /// <summary>
    /// 添加物体到搜索器
    /// 自动判断物体是否在地图内，并分配到正确的区块
    /// </summary>
    /// <param name="id">物体唯一id（与Landscape字典的id保持一致）</param>
    /// <param name="obj">物体实例</param>
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
    }

    /// <summary>
    /// 从搜索器中移除物体
    /// </summary>
    /// <param name="id">物体唯一id</param>
    /// <returns>是否成功移除</returns>
    public bool Remove(int id)
    {
        if (!_allObjects.TryGetValue(id, out T obj))
            return false;
        Vector3 pos = _getPosition(obj);
        Vector2Int chunkIndex = GetChunkIndex(pos);
        RemoveFromChunk(chunkIndex, id);
        return _allObjects.Remove(id);
    }

    /// <summary>
    /// 检查搜索器中是否包含指定id的物体
    /// </summary>
    /// <param name="id">物体唯一id</param>
    /// <returns>是否存在</returns>
    public bool Contains(int id)
    {
        return _allObjects.ContainsKey(id);
    }

    /// <summary>
    /// 根据id获取物体
    /// </summary>
    /// <param name="id">物体唯一id</param>
    /// <param name="obj">输出物体</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetObject(int id, out T obj)
    {
        return _allObjects.TryGetValue(id, out obj);
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        _allObjects.Clear();
        _chunkObjects.Clear();
    }
    #endregion

    #region 区块查询（含特殊区块支持）
    /// <summary>
    /// 获取指定区块内的所有物体，写入外部传入List
    /// </summary>
    /// <param name="chunkIndex">区块索引</param>
    /// <param name="outList">外部传入存储结果的List</param>
    /// <param name="append">false=先清空再写入，true=追加</param>
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

    /// <summary>
    /// 重载：按区块XY索引获取物体写入外部List
    /// </summary>
    public void GetObjectsInChunk(int chunkX, int chunkY, HashSet<T> outList, bool append = false)
    {
        GetObjectsInChunk(new Vector2Int(chunkX, chunkY), outList, append);
    }

    /// <summary>
    /// 获取指定区块内所有物体ID，写入外部HashSet（自动去重）
    /// </summary>
    /// <param name="chunkIndex">区块索引</param>
    /// <param name="outIdSet">外部传入存储ID的HashSet</param>
    /// <param name="append">false=先清空再写入，true=追加</param>
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

    /// <summary>
    /// 重载：按区块XY索引获取物体写入外部List
    /// </summary>
    public void GetObjectIdsInChunk(int chunkX, int chunkY, HashSet<int> outIdSet, bool append = false)
    {
        GetObjectIdsInChunk(new Vector2Int(chunkX, chunkY), outIdSet, append);
    }

    /// <summary>
    /// 获取所有地图外物体，写入外部List
    /// </summary>
    public void GetOutOfMapObjects(HashSet<T> outList, bool append = false)
    {
        GetObjectsInChunk(OutOfMapChunk, outList, append);
    }

    /// <summary>
    /// 获取所有地图外物体ID，写入外部HashSet
    /// </summary>
    public void GetOutOfMapObjectIds(HashSet<int> outIdSet, bool append = false)
    {
        GetObjectIdsInChunk(OutOfMapChunk, outIdSet, append);
    }
    #endregion

    #region 范围查询（自动排除地图外物体）
    /// <summary>
    /// 圆形范围查询物体ID，写入外部HashSet
    /// </summary>
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

    /// <summary>
    /// 获取半径覆盖区块内所有物体ID（不做距离过滤）写入外部HashSet
    /// </summary>
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

    /// <summary>
    /// 获取半径覆盖的所有区块索引，写入外部HashSet
    /// </summary>
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
    /// <summary>
    /// 更新物体区块位置
    /// </summary>
    public bool UpdateObjectPosition(int id)
    {
        if (!_allObjects.TryGetValue(id, out T obj))
            return false;

        Vector3 oldPos = _getPosition(obj);
        Vector2Int oldChunk = GetChunkIndex(oldPos);

        Vector3 newPos = _getPosition(obj);
        Vector2Int newChunk = GetChunkIndex(newPos);

        if (oldChunk == newChunk)
            return true;

        RemoveFromChunk(oldChunk, id);
        AddToChunk(newChunk, id);
        return true;
    }

    /// <summary>
    /// 全量刷新所有区块映射
    /// </summary>
    public void RefreshAll()
    {
        _chunkObjects.Clear();
        foreach (var kv in _allObjects)
        {
            int id = kv.Key;
            T obj = kv.Value;
            Vector3 pos = _getPosition(obj);
            Vector2Int chunk = GetChunkIndex(pos);
            AddToChunk(chunk, id);
        }
    }
    #endregion

    #region 边界检查工具方法
    /// <summary>
    /// 判断区块索引是否有效地图区块
    /// </summary>
    public bool IsValidChunk(Vector2Int chunkIndex)
    {
        return chunkIndex.x >= 0 && chunkIndex.x < Landscape.chunkCount &&
               chunkIndex.y >= 0 && chunkIndex.y < Landscape.chunkCount;
    }

    /// <summary>
    /// 判断世界坐标是否在地图范围内
    /// </summary>
    public bool IsPositionInMap(Vector3 worldPos)
    {
        float mapMaxRange = Landscape.chunkCount * Landscape.chunkSize;
        return worldPos.x >= 0 && worldPos.x < mapMaxRange &&
               worldPos.z >= 0 && worldPos.z < mapMaxRange;
    }

    /// <summary>
    /// 【开发校验】校验区块索引合法性，非RELEASE模式越界直接抛异常
    /// </summary>
    /// <param name="chunkIndex">待校验区块索引</param>
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

    /// <summary>
    /// 世界坐标转区块索引，超出地图返回OutOfMapChunk
    /// </summary>
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
    /// <summary>
    /// 将物体ID加入区块HashSet
    /// </summary>
    private void AddToChunk(Vector2Int chunkIndex, int id)
    {
        if (!_chunkObjects.TryGetValue(chunkIndex, out HashSet<int> idSet))
        {
            idSet = new HashSet<int>();
            _chunkObjects.Add(chunkIndex, idSet);
        }
        idSet.Add(id);
    }

    /// <summary>
    /// 从区块移除物体ID，空区块自动删除Key
    /// </summary>
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