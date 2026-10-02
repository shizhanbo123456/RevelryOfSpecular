using UnityEngine;

public abstract class BulletTrajectory
{
    public abstract Vector3 Lerp(float factor);

    public virtual Vector3 Start => Lerp(0f);

    public virtual Vector3 End => Lerp(1f);

    public float Duration = 1f;

    public static bool TryGetEntityPosition(ushort entityId, out Vector3 pos)
    {
        return TryGetEntityTransform(entityId, out pos, out _);
    }

    public static bool TryGetEntityTransform(ushort entityId, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        // 服务器：实体容器
        if (Tool.BattleManager != null &&
            BattleManager.EntityContainer.Entities.TryGetObject(entityId, out var entity) &&
            entity != null)
        {
            pos = entity.transform.position;
            rot = entity.transform.rotation;
            return true;
        }
        // 客户端：表现物体
        var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
        if (players != null)
        {
            return players.TryGetEntityTransform(entityId, out pos, out rot);
        }
        return false;
    }
}
