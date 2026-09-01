using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资产对象池（高复用内容减少创建和销毁）。
/// 持有资源类型枚举和整数 id，根据这两者确定资源类型（架构说明）。
/// 例如（类型=进攻方角色，id=1）。
/// </summary>
public class AssetsObjectPool : MonoBehaviour
{
    /// <summary>资源类型。</summary>
    public enum AssetType
    {
        Character,
        Weapon,
        BulletVFX,
        ShieldVFX,
        RangeMagicVFX,
        MagicCircleVFX,
        BuffVFX,
        Prop,
    }

    private readonly Dictionary<(AssetType, int), Queue<GameObject>> pools = new();

    private void Awake()
    {
        Tool.AssetsObjectPool = this;
    }

    /// <summary>取出（无池则实例化预制体）。</summary>
    public GameObject Get(AssetType type, int id, GameObject prefab, Transform parent = null)
    {
        GameObject obj = null;
        var key = (type, id);
        if (pools.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        if (obj == null)
        {
            if (prefab == null) return null;
            obj = Instantiate(prefab);
        }
        obj.transform.SetParent(parent, false);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>归还（停用并入池）。</summary>
    public void Return(AssetType type, int id, GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        var key = (type, id);
        if (!pools.TryGetValue(key, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[key] = queue;
        }
        queue.Enqueue(obj);
    }

    /// <summary>清空全部池（对局结束）。</summary>
    public void Clear()
    {
        foreach (var queue in pools.Values)
        {
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
        pools.Clear();
    }
}
