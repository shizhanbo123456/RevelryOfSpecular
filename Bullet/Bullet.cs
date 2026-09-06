using System;
using static EntityEffectController;

public struct Bullet
{
    public ushort shooter;
    public float rate;
    public BulletTrajectory trajectory;
    public float radius;
    public float lifeTime;
    public Damageable.IDamageable damageable;
    public Action<Action<EffectType, int, float>> addEffectEvent;

    public EntityAttribute attribute;
    public float spawnTime;

    public Vector3 Position =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - spawnTime) / lifeTime)) : Vector3.zero;
    public Vector3 LastPosition =>
        trajectory != null ? trajectory.Lerp(Mathf.Clamp01((Time.time - Time.deltaTime - spawnTime) / lifeTime)) : Vector3.zero;
    public readonly bool InDamageWindow =>
        damageable.InDamageWindow(Time.time - spawnTime);
}
