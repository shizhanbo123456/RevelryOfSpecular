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
    #endregion

    #region 通用播放原语（已持有模板 GameObject 时使用）
    public GameObject Play(GameObject vfxPrefab, Vector3 pos, Quaternion rot, float lifeTime)
    {
        if (vfxPrefab == null) return null;
        var obj = Instantiate(vfxPrefab, pos, rot);
        Destroy(obj, lifeTime > 0f ? lifeTime : 1f);
        return obj;
    }

    public GameObject Play(GameObject vfxPrefab, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent)
    {
        if (vfxPrefab == null || trajectory == null) return null;
        var obj = Instantiate(vfxPrefab);
        BulletPlayer.Create(obj, trajectory, lifeTime, rotation);
        return obj;
    }
    #endregion

    #region 按下标播放（取模板 + 播放的便捷封装，外部常规入口）
    public void PlayBulletVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        Play(GetBulletVfx(index), pos, rot, lifeTime);
    }

    public GameObject PlayBulletVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent)
    {
        return Play(GetBulletVfx(index), trajectory, lifeTime, rotation);
    }

    public GameObject PlayWeaponVFX(WeaponRef weapon, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent)
    {
        if (Tool.AssetsManager == null || !Tool.AssetsManager.TryGetWeaponPrefab(weapon, out var prefab)) return null;
        return Play(prefab, trajectory, lifeTime, rotation);
    }

    public GameObject PlayWeaponVFX(WeaponRef weapon, Vector3 pos, Quaternion rot, float lifeTime)
    {
        if (Tool.AssetsManager == null || !Tool.AssetsManager.TryGetWeaponPrefab(weapon, out var prefab)) return null;
        return Play(prefab, pos, rot, lifeTime);
    }

    public GameObject PlayShieldVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Constant)
    {
        return Play(GetShieldVfx(index), trajectory, lifeTime, rotation);
    }

    public GameObject PlayShieldVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        return Play(GetShieldVfx(index), pos, rot, lifeTime);
    }

    public void PlayRangeMagicVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        Play(GetRangeMagicVfx(index), pos, rot, lifeTime);
    }

    // 轨迹版语义：取轨迹终点（Lerp(1)）落地定点播放，朝向固定 identity
    public void PlayRangeMagicVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f)
    {
        if (trajectory == null) return;
        float life = lifeTime > 0f ? lifeTime : trajectory.Duration;
        PlayRangeMagicVFX(index, trajectory.End, Quaternion.identity, life);
    }

    public GameObject PlayMagicCircleVFX(int index, Vector3 pos, float duration)
    {
        var prefab = GetMagicCircleVfx(index);
        if (prefab == null) return null;
        var obj = Instantiate(prefab, pos, Quaternion.identity);
        if (duration > 0f) Destroy(obj, duration);
        return obj;
    }

    // 轨迹版语义：取轨迹终点（Lerp(1)）落地定点播放，朝向固定 identity
    public GameObject PlayMagicCircleVFX(int index, BulletTrajectory trajectory, float duration = 0f)
    {
        if (trajectory == null) return null;
        float life = duration > 0f ? duration : trajectory.Duration;
        return PlayMagicCircleVFX(index, trajectory.End, life);
    }

    public GameObject PlayBuffVFX(int index, BulletTrajectory trajectory, float lifeTime = 0f,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Constant)
    {
        return Play(GetBuffVfx(index), trajectory, lifeTime, rotation);
    }

    public GameObject PlayBuffVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        return Play(GetBuffVfx(index), pos, rot, lifeTime);
    }
    #endregion

    #region//Local
    private GameObject GetVFX(System.Collections.Generic.List<GameObject> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count) return null;
        return list[index];
    }
    #endregion
}
