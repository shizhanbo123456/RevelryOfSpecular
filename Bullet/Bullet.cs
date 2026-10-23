using System.Collections.Generic;
using UnityEngine;

// 子弹命中判定参数：命中球来自特效预制体的 BulletVfxInfo，倍率/间隔来自 skill.csv
public readonly struct BulletHitSpec
{
    private readonly BulletVfxInfo.HitInfo[] hits;   // 局部空间伤害球（资产位姿归零，center 即特效实际位置）
    public readonly float[] rates;                   // 与 hits 一一对应的伤害倍率，null = 全 1
    public readonly float scale;                     // 特效缩放倍率，命中球同步放大
    public readonly float interval;                  // 同一目标最短再伤害间隔（秒），<=0 = 只结算一次
    public readonly string label;                    // 日志用：特效类别[下标]

    public BulletVfxInfo.HitInfo[] Hits => hits ?? System.Array.Empty<BulletVfxInfo.HitInfo>();

    public BulletHitSpec(BulletVfxInfo.HitInfo[] hits, float[] rates, float scale, float interval, string label)
    {
        this.hits = hits;
        this.rates = rates;
        this.scale = scale > 0f ? scale : 1f;
        this.interval = interval;
        this.label = label;
    }
}

public struct Bullet
{
    public AttackData attack;
    public BulletTrajectory trajectory;
    public float lifeTime;
    public float spawnTime;
    public float lastTickTime;                    // 上一帧采样时刻（时间戳差值推进，不累加 deltaTime）
    public BulletHitSpec spec;
    public Dictionary<ushort, float> lastHitTime; // 每目标上次受伤时刻

    public readonly float Age(float now) => now - spawnTime;

    public readonly Vector3 At(float t) =>
        trajectory == null ? Vector3.zero
        : trajectory.Lerp(lifeTime > 0f ? Mathf.Clamp01(t / lifeTime) : 1f);
}
