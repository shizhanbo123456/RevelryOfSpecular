using UnityEngine;

public class VfxManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.VfxManager = this;
    }

    #region 特效模板获取（按类别返回预制体，供自定义播放方式使用）
    public GameObject GetBulletVfx(int index)
    {
        var list = Tool.AssetsManager != null ? Tool.AssetsManager.BulletVFX : null;
        return GetVFX(list, index);
    }

    public GameObject GetShieldVfx(int index)
    {
        var list = Tool.AssetsManager != null ? Tool.AssetsManager.ShieldVFX : null;
        return GetVFX(list, index);
    }

    public GameObject GetRangeMagicVfx(int index)
    {
        var list = Tool.AssetsManager != null ? Tool.AssetsManager.RangeMagicVFX : null;
        return GetVFX(list, index);
    }

    public GameObject GetMagicCircleVfx(int index)
    {
        var list = Tool.AssetsManager != null ? Tool.AssetsManager.MagicCircleVFX : null;
        return GetVFX(list, index);
    }

    public GameObject GetBuffVfx(int index)
    {
        var list = Tool.AssetsManager != null ? Tool.AssetsManager.BuffVFX : null;
        return GetVFX(list, index);
    }

    public GameObject GetHitVfx(int index)
    {
        var list = Tool.AssetsManager != null ? Tool.AssetsManager.HitVFX : null;
        return GetVFX(list, index);
    }
    #endregion

    #region 通用播放原语（已持有模板 GameObject 时使用）
    public GameObject Play(GameObject vfxPrefab, Vector3 pos, Quaternion rot, float lifeTime, float scale = 0f)
    {
        if (vfxPrefab == null) return null;
        var obj = Instantiate(vfxPrefab, pos, rot);
        ApplyScale(obj, scale);
        Destroy(obj, lifeTime > 0f ? lifeTime : 1f);
        return obj;
    }

    public GameObject Play(GameObject vfxPrefab, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent, float scale = 0f)
    {
        if (vfxPrefab == null || trajectory == null) return null;
        // 必须实例化时就给到起点：世界空间的粒子在 playOnAwake 的瞬间就会按预制体位置喷一帧，之后再摆位置会留下原点残影
        var obj = Instantiate(vfxPrefab, trajectory.Lerp(0f), vfxPrefab.transform.rotation);
        ApplyScale(obj, scale);
        BulletPlayer.Create(obj, trajectory, lifeTime, rotation);
        return obj;
    }

    // 特效资产的位姿与缩放统一是 (0,0,0)/(1,1,1)，倍率直接写成根节点缩放（0 = 不缩放）
    private static void ApplyScale(GameObject obj, float scale)
    {
        if (obj == null || scale <= 0f || Mathf.Approximately(scale, 1f)) return;
        obj.transform.localScale = Vector3.one * scale;
    }
    #endregion

    #region 按下标播放（取模板 + 播放的便捷封装，外部常规入口）
    public void PlayBulletVFX(int index, Vector3 pos, Quaternion rot, float lifeTime, float scale = 0f)
    {
        Play(GetBulletVfx(index), pos, rot, lifeTime, scale);
    }

    public GameObject PlayBulletVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent, float scale = 0f)
    {
        return Play(GetBulletVfx(index), trajectory, lifeTime, rotation, scale);
    }

    // 传全局武器 id，内部经隐式转换还原 WeaponRef
    public GameObject PlayWeaponVFX(int weaponId, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent, float scale = 0f)
    {
        if (Tool.AssetsManager == null || !Tool.AssetsManager.TryGetWeaponPrefab(weaponId, out var prefab)) return null;
        return Play(prefab, trajectory, lifeTime, rotation, scale);
    }

    // 传全局武器 id，内部经隐式转换还原 WeaponRef
    public GameObject PlayWeaponVFX(int weaponId, Vector3 pos, Quaternion rot, float lifeTime, float scale = 0f)
    {
        if (Tool.AssetsManager == null || !Tool.AssetsManager.TryGetWeaponPrefab(weaponId, out var prefab)) return null;
        return Play(prefab, pos, rot, lifeTime, scale);
    }

    public GameObject PlayShieldVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Constant, float scale = 0f)
    {
        return Play(GetShieldVfx(index), trajectory, lifeTime, rotation, scale);
    }

    public GameObject PlayShieldVFX(int index, Vector3 pos, Quaternion rot, float lifeTime, float scale = 0f)
    {
        return Play(GetShieldVfx(index), pos, rot, lifeTime, scale);
    }

    public void PlayRangeMagicVFX(int index, Vector3 pos, Quaternion rot, float lifeTime, float scale = 0f)
    {
        Play(GetRangeMagicVfx(index), pos, rot, lifeTime, scale);
    }

    // 轨迹版语义：取轨迹终点（Lerp(1)）落地定点播放，朝向固定 identity
    public void PlayRangeMagicVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f, float scale = 0f)
    {
        if (trajectory == null) return;
        float life = lifeTime > 0f ? lifeTime : trajectory.Duration;
        PlayRangeMagicVFX(index, trajectory.End, Quaternion.identity, life, scale);
    }

    public GameObject PlayMagicCircleVFX(int index, Vector3 pos, float duration, float scale = 0f)
    {
        var prefab = GetMagicCircleVfx(index);
        if (prefab == null) return null;
        var obj = Instantiate(prefab, pos, Quaternion.identity);
        ApplyScale(obj, scale);
        if (duration > 0f) Destroy(obj, duration);
        return obj;
    }

    // 轨迹版语义：取轨迹终点（Lerp(1)）落地定点播放，朝向固定 identity
    public GameObject PlayMagicCircleVFX(int index, BulletTrajectory trajectory, float duration = 0f, float scale = 0f)
    {
        if (trajectory == null) return null;
        float life = duration > 0f ? duration : trajectory.Duration;
        return PlayMagicCircleVFX(index, trajectory.End, life, scale);
    }

    public GameObject PlayBuffVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Constant, float scale = 0f)
    {
        return Play(GetBuffVfx(index), trajectory, lifeTime, rotation, scale);
    }

    public GameObject PlayBuffVFX(int index, Vector3 pos, Quaternion rot, float lifeTime, float scale = 0f)
    {
        return Play(GetBuffVfx(index), pos, rot, lifeTime, scale);
    }

    public GameObject PlayHitVFX(int index, Vector3 pos, float lifeTime, float scale = 0f)
    {
        return Play(GetHitVfx(index), pos, Quaternion.identity, lifeTime, scale);
    }
    #endregion

    #region//Local
    private GameObject GetVFX(System.Collections.Generic.List<GameObject> list, int index)
    {
        if (list == null)
        {
            Debug.LogError("[Vfx] 特效列表未配置（AssetsManager 缺失或启动自检未通过）");
            return null;
        }
        if (index < 0 || index >= list.Count)
        {
            Debug.LogError($"[Vfx] 特效下标越界：{index}（该类共 {list.Count} 个），请核对《特效清单与分配表》");
            return null;
        }
        if (list[index] == null) Debug.LogError($"[Vfx] 特效预制体为空：下标 {index}");
        return list[index];
    }
    #endregion
}
