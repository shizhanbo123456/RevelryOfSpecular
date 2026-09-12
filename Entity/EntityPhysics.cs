using UnityEngine;

/// <summary>
/// 实体判定的物理查询：统一实体层掩码，并把命中结果解析成 EntityData 写入调用方数组。
/// 结果先解析再返回，故允许嵌套调用（内层查询不会破坏外层正在遍历的结果）。
/// </summary>
public static class EntityPhysics
{
    private static readonly Collider[] s_colliderBuffer = new Collider[32];

    /// <summary>实体层掩码（层号由 InfoManager 配置）。</summary>
    public static int EntityMask => 1 << Tool.InfoManager.entity_layer;

    /// <summary>查询球内的实体（返回数量，最多 results.Length 个）。</summary>
    public static int OverlapSphere(Vector3 center, float radius, EntityData[] results)
    {
        return Resolve(Physics.OverlapSphereNonAlloc(center, radius, s_colliderBuffer, EntityMask), results);
    }

    /// <summary>查询胶囊内的实体：以「上一帧位置 → 当前位置」覆盖整段路径，防高速子弹穿模。</summary>
    public static int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, EntityData[] results)
    {
        return Resolve(Physics.OverlapCapsuleNonAlloc(point0, point1, radius, s_colliderBuffer, EntityMask), results);
    }

    /// <summary>把碰撞体结果解析成实体（非实体的碰撞体——如层配置错误时命中的地形——直接丢弃）。</summary>
    private static int Resolve(int count, EntityData[] results)
    {
        int resolved = 0;
        for (int i = 0; i < count && resolved < results.Length; i++)
        {
            var entity = s_colliderBuffer[i].GetComponentInParent<EntityData>();
            if (entity != null) results[resolved++] = entity;
        }
        return resolved;
    }
}
