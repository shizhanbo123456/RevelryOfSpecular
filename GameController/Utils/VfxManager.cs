using UnityEngine;

/// <summary>
/// 特效管理器（全项目唯一的特效取用入口，资源编号与《特效清单与分配表.md》对应）。
/// 两层结构：
/// 1. 模板获取：GetXxxVfx(index) 返回特效预制体 GameObject（按类别区分，下标见特效清单）；
/// 2. 播放：PlayXxxVFX(index, ...) 按下标取模板并按需求播放（定点 / 沿轨迹 / 跟随实体轨迹），
///    或已持有模板时调用 Play(vfxPrefab, ...) 重载直接播放。
/// 技能表现侧约定：技能 PlayVFX 用与服务器相同的构建函数重建轨迹后，统一经本管理器播放特效，
/// 技能内禁止直接 Instantiate 或引用 AssetsManager。
/// 客户端专用，服务器不挂载本组件。
/// </summary>
public class VfxManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.VfxManager = this;
    }

    #region 特效模板获取（按类别返回预制体，供自定义播放方式使用）
    /// <summary>子弹特效模板（60 个：20 类 × 3 颜色变体，下标 0~59 见特效清单）。</summary>
    public GameObject GetBulletVfx(int index) => GetVFX(Tool.AssetsManager?.BulletVFX, index);

    /// <summary>护盾特效模板（13 个）。</summary>
    public GameObject GetShieldVfx(int index) => GetVFX(Tool.AssetsManager?.ShieldVFX, index);

    /// <summary>范围魔法特效模板（10 个）。</summary>
    public GameObject GetRangeMagicVfx(int index) => GetVFX(Tool.AssetsManager?.RangeMagicVFX, index);

    /// <summary>魔法阵特效模板（10 个）。</summary>
    public GameObject GetMagicCircleVfx(int index) => GetVFX(Tool.AssetsManager?.MagicCircleVFX, index);

    /// <summary>Buff 特效模板（31 个）。</summary>
    public GameObject GetBuffVfx(int index) => GetVFX(Tool.AssetsManager?.BuffVFX, index);
    #endregion

    #region 通用播放原语（已持有模板 GameObject 时使用）
    /// <summary>定点播放：pos/rot 处实例化，lifeTime 秒后自动销毁（lifeTime &lt;= 0 默认 1 秒）。</summary>
    public GameObject Play(GameObject vfxPrefab, Vector3 pos, Quaternion rot, float lifeTime)
    {
        if (vfxPrefab == null) return null;
        var obj = Instantiate(vfxPrefab, pos, rot);
        Destroy(obj, lifeTime > 0f ? lifeTime : 1f);
        return obj;
    }

    /// <summary>沿轨迹播放：实例化后由 BulletPlayer 驱动沿轨迹移动，lifeTime 到期自动销毁。
    /// 跟随类特效（护盾/Buff）用 FollowTrajectory(实体id, 偏移) 作为轨迹——位置恒等于目标当前位置，
    /// 服务器判定与客户端表现共用；持续期用 Buff 剩余时长作 lifeTime，或传极大值后手动销毁返回的实例。</summary>
    public GameObject Play(GameObject vfxPrefab, BulletTrajectory trajectory, float lifeTime,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent)
    {
        if (vfxPrefab == null || trajectory == null) return null;
        var obj = Instantiate(vfxPrefab);
        BulletPlayer.Create(obj, trajectory, lifeTime, rotation);
        return obj;
    }
    #endregion

    #region 按下标播放（取模板 + 播放的便捷封装，外部常规入口）
    /// <summary>播放子弹特效（60 种：20 类 × 3 颜色，下标 0~59 见特效清单）。</summary>
    public void PlayBulletVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        Play(GetBulletVfx(index), pos, rot, lifeTime);
    }

    /// <summary>沿弹道轨迹播放子弹特效（客户端技能表现侧统一入口）：
    /// 轨迹由技能用与服务器相同的构建函数重建，实例化与到期销毁由本管理器负责。</summary>
    public GameObject PlayBulletVFX(int index, BulletTrajectory trajectory, float lifeTime,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Tangent)
    {
        return Play(GetBulletVfx(index), trajectory, lifeTime, rotation);
    }

    /// <summary>沿轨迹播放护盾特效（13 种）。跟随实体用 FollowTrajectory(实体id, 偏移)；
    /// 持续期建议 = 护盾来源 Buff 的剩余时长，或传极大值后手动销毁返回的实例。</summary>
    public GameObject PlayShieldVFX(int index, BulletTrajectory trajectory, float lifeTime,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Constant)
    {
        return Play(GetShieldVfx(index), trajectory, lifeTime, rotation);
    }

    /// <summary>播放范围魔法特效（10 种）。</summary>
    public void PlayRangeMagicVFX(int index, Vector3 pos, Quaternion rot, float lifeTime)
    {
        Play(GetRangeMagicVfx(index), pos, rot, lifeTime);
    }

    /// <summary>播放魔法阵特效（10 种）：duration &gt; 0 到时自动销毁，&lt;= 0 由调用方管理销毁时机。</summary>
    public GameObject PlayMagicCircleVFX(int index, Vector3 pos, float duration)
    {
        var prefab = GetMagicCircleVfx(index);
        if (prefab == null) return null;
        var obj = Instantiate(prefab, pos, Quaternion.identity);
        if (duration > 0f) Destroy(obj, duration);
        return obj;
    }

    /// <summary>沿轨迹播放 Buff 特效（31 种）。跟随实体用 FollowTrajectory(实体id, 偏移)；
    /// 持续期建议 = Buff 剩余时长（服务器随 BuffRuntime 下发，永续传极大值），Buff 移除时手动销毁返回的实例。</summary>
    public GameObject PlayBuffVFX(int index, BulletTrajectory trajectory, float lifeTime,
        BulletPlayer.RotationMode rotation = BulletPlayer.RotationMode.Constant)
    {
        return Play(GetBuffVfx(index), trajectory, lifeTime, rotation);
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
