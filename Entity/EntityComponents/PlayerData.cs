using Ros.Transport;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerData : EntityData
{
    private Rigidbody rb;
    private bool moving;
    private float yaw;
    public override void OnCreate(ushort id, EntityType type, int level)
    {
        base.OnCreate(id, type, level);
        rb=GetComponent<Rigidbody>();
    }
    public override void OnUpdate()
    {
        base.OnUpdate();
        Vector3 v = rb.velocity;
        if (moving)
        {
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            v.x = 0.15f * floatingAttribute.speed * direction.x;//本应为0.1，此处暂时加快，方便测试
            v.z = 0.15f * floatingAttribute.speed * direction.z;
            //仅覆盖水平分量，保留 Y：重力速度正常累积，悬空移动/下落不再被清零
            rb.velocity = v;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        else
        {
            v.x = 0f;
            v.z = 0f;
            //仅停住水平移动，保留 Y：悬空时自由落体不受影响
            rb.velocity = v;
        }
    }
    public override SCEntityDisplayInfo GetDisplayInfo()
    {
        return new SCEntityDisplayInfo()
        {
            id = id,
            type = type,
            level = level,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            yaw = transform.rotation.eulerAngles.y,
            speed = moving?floatingAttribute.speed*0.1f:0,
            angularSpeed = 0,
            health = floatingAttribute.health,
            maxHealth = baseAttribute.health,
            effectGraphic = effectController == null ? 0 : effectController.GetEffectGraphicInfo(),
        };
    }
    public void MoveTo(CSInputCommand command)
    {
        moving = command.moving;
        if (moving) yaw = Mathf.Repeat(command.yaw, 360f);
    }
}
