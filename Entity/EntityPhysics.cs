using UnityEngine;

public static class EntityPhysics
{
    private static readonly Collider[] s_colliderBuffer = new Collider[32];

    public static int EntityMask => 1 << Tool.InfoManager.entity_layer;

    public static int GroundMask => 1 << Tool.InfoManager.ground_layer;

    public static int OverlapSphere(Vector3 center, float radius, EntityData[] results)
    {
        return Resolve(Physics.OverlapSphereNonAlloc(center, radius, s_colliderBuffer, EntityMask), results);
    }

    public static int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, EntityData[] results)
    {
        return Resolve(Physics.OverlapCapsuleNonAlloc(point0, point1, radius, s_colliderBuffer, EntityMask), results);
    }

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
