using System.Collections.Generic;
using Ros.Skill;
using UnityEngine;

// 技能特效 → 预制体上的命中球。判定在服务器进行，服务器同样挂着 AssetsManager，故直接读预制体，不另存一份配表
public static class VfxHitSource
{
    private static readonly Dictionary<int, BulletVfxInfo.HitInfo[]> cache = new();

    public static BulletVfxInfo.HitInfo[] GetHits(SkillVfxKind kind, int index)
    {
        int key = (int)kind * 4096 + index;
        if (cache.TryGetValue(key, out var hits)) return hits;
        var info = Resolve(kind, index);
        hits = info != null ? info.hits.ToArray() : System.Array.Empty<BulletVfxInfo.HitInfo>();
        cache[key] = hits;
        return hits;
    }

    private static BulletVfxInfo Resolve(SkillVfxKind kind, int index)
    {
        var mgr = Tool.AssetsManager;
        if (mgr == null) return null;

        GameObject prefab = null;
        switch (kind)
        {
            case SkillVfxKind.Bullet:     prefab = At(mgr.BulletVFX, index); break;
            case SkillVfxKind.Shield:     prefab = At(mgr.ShieldVFX, index); break;
            case SkillVfxKind.RangeMagic: prefab = At(mgr.RangeMagicVFX, index); break;
            case SkillVfxKind.MagicCircle: prefab = At(mgr.MagicCircleVFX, index); break;
            case SkillVfxKind.Buff:       prefab = At(mgr.BuffVFX, index); break;
            case SkillVfxKind.Weapon:     mgr.TryGetWeaponPrefab(index, out prefab); break;
        }
        return prefab != null ? prefab.GetComponent<BulletVfxInfo>() : null;
    }

    private static GameObject At(List<GameObject> list, int index)
        => list != null && index >= 0 && index < list.Count ? list[index] : null;
}
