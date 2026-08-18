using Ros.Transport;
using UnityEngine;

public class InfectionData : EntityData
{
    public override SCEntityDisplayInfo GetDisplayInfo()
    {
        return new SCEntityDisplayInfo()
        {
            id=id,
            type=type,
            level=level,
            x=transform.position.x,
            y=transform.position.y,
            z=transform.position.z,
            yaw=0,
            speed=0,
            angularSpeed=0,
            health = floatingAttribute.health,
            maxHealth = baseAttribute.health,
            effectGraphic = effectController == null ? 0 : effectController.GetEffectGraphicInfo(),
        };
    }
    public override Vector3 BulletShootPos()
    {
        return transform.position + colliderInfo.top * Vector3.up;
    }
}
