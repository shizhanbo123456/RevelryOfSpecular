using UnityEngine;

/// <summary>
/// 特效管理器（统一播放入口，资源编号与策划案 2.4 素材清单对应）。
/// 客户端表现侧调用；服务端不播特效。
/// </summary>
public class VfxManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.VfxManager = this;
    }

    #region 各类型特效播放入口（index 与 AssetsManager 列表下标对应）
    /// <summary>播放子弹特效（60 种：20 类 × 3 颜色，策划案 2.4）。</summary>
    public void PlayBulletVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        var prefab = GetVFX(Tool.AssetsManager?.BulletVFX, index);
        PlayAndDestroy(prefab, pos, rot, lifeTime);
    }

    /// <summary>播放护盾特效（13 种），跟随父物体。</summary>
    public GameObject PlayShieldVFX(int index, Transform parent)
    {
        var prefab = GetVFX(Tool.AssetsManager?.ShieldVFX, index);
        if (prefab == null) return null;
        var obj = Instantiate(prefab, parent);
        obj.transform.localPosition = Vector3.zero;
        return obj;
    }

    /// <summary>播放范围魔法特效（10 种）。</summary>
    public void PlayRangeMagicVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        var prefab = GetVFX(Tool.AssetsManager?.RangeMagicVFX, index);
        PlayAndDestroy(prefab, pos, rot, lifeTime);
    }

    /// <summary>播放魔法阵特效（10 种）。</summary>
    public GameObject PlayMagicCircleVFX(int index, Vector3 pos, float duration)
    {
        var prefab = GetVFX(Tool.AssetsManager?.MagicCircleVFX, index);
        if (prefab == null) return null;
        var obj = Instantiate(prefab, pos, Quaternion.identity);
        if (duration > 0f) Destroy(obj, duration);
        return obj;
    }

    /// <summary>播放 Buff 特效（31 种），跟随父物体。</summary>
    public GameObject PlayBuffVFX(int index, Transform parent)
    {
        var prefab = GetVFX(Tool.AssetsManager?.BuffVFX, index);
        if (prefab == null) return null;
        var obj = Instantiate(prefab, parent);
        obj.transform.localPosition = Vector3.zero;
        return obj;
    }
    #endregion

    #region//Local
    private GameObject GetVFX(System.Collections.Generic.List<GameObject> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count) return null;
        return list[index];
    }

    private void PlayAndDestroy(GameObject prefab, Vector3 pos, Quaternion rot, float lifeTime)
    {
        if (prefab == null) return;
        var obj = Instantiate(prefab, pos, rot);
        Destroy(obj, lifeTime > 0f ? lifeTime : 1f);
    }
    #endregion
}
