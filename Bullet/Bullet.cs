using System.Collections.Generic;
using UnityEngine;

public struct Bullet
{
    public AttackData attack;
    public BulletTrajectory trajectory;
    public float lifeTime;
    public float spawnTime;
    public HashSet<ushort> hitIds;

    public Vector3 Position =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - spawnTime) / lifeTime)) : Vector3.zero;
    public Vector3 LastPosition =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - Time.deltaTime - spawnTime) / lifeTime)) : Vector3.zero;
    public readonly bool InDamageWindow =>
        attack != null && attack.damageable != null && attack.damageable.InDamageWindow(Time.time - spawnTime);
}
