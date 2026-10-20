using System.Collections.Generic;
using UnityEngine;

public struct Bullet
{
    public AttackData attack;
    public BulletTrajectory trajectory;
    public float lifeTime;
    public float spawnTime;
    public float damageWindowFrom;  // 归一化伤害窗口起（0~1）：表现有前摇时推迟生效
    public float damageWindowTo;    // 归一化伤害窗口止（0~1）：表现有后摇时提前结束
    public float damageInterval;    // 同一目标最短再伤害间隔（秒），<=0 = 一目标只结算一次
    public Dictionary<ushort, float> lastHitTime; // 每目标上次受伤时刻（替代一次性 hitIds）

    public Vector3 Position =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - spawnTime) / lifeTime)) : Vector3.zero;
    public Vector3 LastPosition =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - Time.deltaTime - spawnTime) / lifeTime)) : Vector3.zero;
    public readonly bool InDamageWindow =>
        attack != null && attack.damageable != null && attack.damageable.InDamageWindow(Time.time - spawnTime);
}
