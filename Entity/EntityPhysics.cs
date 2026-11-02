using System.Collections.Generic;
using UnityEngine;

// 命中判定的唯一入口：全部在 XZ 平面计算（Y 不参与），不走物理查询
public static class EntityPhysics
{
    private static readonly HashSet<int> s_hitIds = new();
    private static bool s_truncationReported;

    public static int GroundMask => 1 << Tool.InfoManager.ground_layer;

    public static int OverlapCircleXZ(Vector3 center, float radius, EntityData[] results)
        => OverlapSegmentXZ(center, center, radius, results);

    public static int OverlapCapsuleXZ(Vector3 from, Vector3 to, float radius, EntityData[] results)
        => OverlapSegmentXZ(from, to, radius, results);

    // 判定体 = 线段 + 半径（圆头胶囊），目标 = 圆心 + 半径；两者距离用平面距离比较
    private static int OverlapSegmentXZ(Vector3 from, Vector3 to, float radius, EntityData[] results)
    {
        var entities = BattleManager.EntityContainer.Entities;
        float half = (to - from).magnitude * 0.5f;
        // 区块集合当宽相位：半径带上最大实体半径，保证边界附近的目标不会漏
        entities.GetIdsInRelativeBlocks((from + to) * 0.5f, half + radius + EntityData.MaxHitRadiusXZ, s_hitIds);

        int count = 0;
        foreach (int id in s_hitIds)
        {
            if (count >= results.Length)
            {
                if (!s_truncationReported)
                {
                    s_truncationReported = true;
                    Debug.LogError($"[判定] 单次命中目标超过 {results.Length} 个，超出部分被丢弃：确认这是否是预期内的大范围效果");
                }
                break;
            }
            if (!entities.TryGetObject(id, out var entity) || entity == null) continue;
            float sum = radius + entity.HitRadiusXZ;
            if (SqrDistPointSegmentXZ(entity.HitCenterXZ, from, to) > sum * sum) continue;
            results[count++] = entity;
        }
        return count;
    }

    private static float SqrDistPointSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        float ax = p.x - a.x, az = p.z - a.z;
        float bx = b.x - a.x, bz = b.z - a.z;
        float lenSqr = bx * bx + bz * bz;
        float t = lenSqr > 1e-8f ? Mathf.Clamp01((ax * bx + az * bz) / lenSqr) : 0f;
        float dx = ax - bx * t, dz = az - bz * t;
        return dx * dx + dz * dz;
    }
}
