using Ros.Info;
using Ros.Transport;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityData : MonoBehaviour
{
    [System.Serializable]
    public struct EntityColliderInfo
    {
        public float bottom;
        public float top;
        public float radius;
    }

    public EntityColliderInfo colliderInfo;
    public Transform BarPos;
    
    [HideInInspector]public ushort id;
    [HideInInspector]public EntityType type;
    [HideInInspector] public int level;
    [Header("属性")]
    public EntityAttribute baseAttribute;
    public EntityAttribute floatingAttribute;

    [HideInInspector] public EntityEffectController effectController;

    public static List<EntityData> KilledEntities = new List<EntityData>();

    public bool Alive => floatingAttribute.health > 0;
    
    public virtual void OnCreate(ushort id,EntityType type,int level)
    {
        this.id=id;
        this.type=type;
        this.level = level;

        var info= EntityPool.GetAttributeInfo(type);
        baseAttribute = info.GetAttribute(level);
        floatingAttribute = baseAttribute.Clone();

        effectController = new EntityEffectController();
        effectController.Init(this);
    }

    public virtual void OnUpdate()
    {
        bool wasAlive = Alive;
        effectController.OnUpdate();
        if (wasAlive && !Alive && !KilledEntities.Contains(this))
        {
            floatingAttribute.health = 0;
            KilledEntities.Add(this);
        }
    }

    public abstract SCEntityDisplayInfo GetDisplayInfo();


    public virtual Vector3 BulletShootPos()
    {
        return transform.position+(colliderInfo.top*7+colliderInfo.bottom*3)*0.1f*Vector3.up;
    }
    public float GetBarYOffset()
    {
        return BarPos == null ? colliderInfo.top : BarPos.position.y - transform.position.y;
    }
    public virtual void OnDamaged(int value)
    {
        if (floatingAttribute.health <= value)
        {
            floatingAttribute.health = 0;
            KilledEntities.Add(this);
        }
        else
        {
            floatingAttribute.health -= value;
        }
    }
    public virtual void OnDamaged(int value, EntityData attacker)
    {
        OnDamaged(value);
    }
    public virtual void OnKilled()
    {

    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.9f, 0.7f,0.5f);
        Gizmos.DrawSphere(transform.position + colliderInfo.bottom * Vector3.up, colliderInfo.radius);
        Gizmos.DrawSphere(transform.position + colliderInfo.top * Vector3.up, colliderInfo.radius);
    }
}
