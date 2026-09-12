using System.Collections.Generic;
using UnityEngine;

public struct Bullet
{
    /// <summary>攻击数据（近战与子弹共用，含破霸体等伤害信息，见策划案 12.1）。</summary>
    public AttackData attack;
    /// <summary>弹道轨迹。</summary>
    public BulletTrajectory trajectory;
    /// <summary>生命周期（秒）。</summary>
    public float lifeTime;
    /// <summary>生成时刻（Time.time）。</summary>
    public float spawnTime;
    /// <summary>已结算过伤害的实体 id（穿透：同一发子弹对同一目标只结算一次）。</summary>
    public HashSet<ushort> hitIds;

    public Vector3 Position =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - spawnTime) / lifeTime)) : Vector3.zero;
    public Vector3 LastPosition =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - Time.deltaTime - spawnTime) / lifeTime)) : Vector3.zero;
    public readonly bool InDamageWindow =>
        attack != null && attack.damageable != null && attack.damageable.InDamageWindow(Time.time - spawnTime);
}
