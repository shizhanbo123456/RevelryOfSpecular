using System.Collections.Generic;
using UnityEngine;

public class AssetsObjectPool : MonoBehaviour
{
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
