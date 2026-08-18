using System;
using UnityEngine;
using static EntityEffectController;

public struct Bullet
{
    public ushort shooter;
    public float rate;
    public BezierCurve curve;
    public float radius;
    public float lifeTime;
    public Damageable.IDamageable damageable;
    public Action<Action<EffectType, int, float>> addEffectEvent;

    public EntityAttribute attribute;
    public float spawnTime;

    public Vector3 Position => 
        curve.Lerp(Mathf.Clamp01((Time.time - spawnTime) / lifeTime));
    public Vector3 LastPosition => 
        curve.Lerp(Mathf.Clamp01((Time.time-Time.deltaTime - spawnTime) / lifeTime));
    public readonly bool InDamageWindow =>
        damageable.InDamageWindow(Time.time - spawnTime);
}