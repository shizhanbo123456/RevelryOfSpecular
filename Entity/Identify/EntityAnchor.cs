using UnityEngine;

public enum EntityAnchor
{
    //完全不依赖人物骨骼的
    GameObjectPosition,
    WeaponSlot1,
    WeaponSlot2,
    WeaponSlot3,
    WeaponSlot4,
    WeaponSlot5,
    WeaponSlot6,
    WeaponSlot7,
    WeaponSlot8,

    //可依赖人物骨骼，但也支持非人形的
    Bar,//头顶血条位置
    Name,//头顶名称位置
    UpCenter,//中上中心
    UpFront,//中上前方

    //必须依赖人物骨骼的
    ModelRootPosition,//模型根位置
    Head,
    LeftHand,
    RightHand,
    LeftFoot,
    RightFoot,
    Chest,
}
public struct EntityAnchorInfo
{
    public Transform transform;
    public Vector3 position;
    public Quaternion rotation;

    public static EntityAnchorInfo operator +(EntityAnchorInfo info, Vector3 offset)
    {
        return new EntityAnchorInfo()
        {
            transform = null,
            position = info.position + offset,
            rotation = info.rotation
        };
    }
}
public static class EntityAnchorExtensions
{
    public static EntityAnchorInfo GetTransform(this EntityAnchor anchor, EntityData entity)
    {
        if (entity == null) return default;

        if (entity.anim != null)
        {
            switch (anchor)
            {
                case EntityAnchor.ModelRootPosition: return entity.anim.GetBoneTransform(HumanBodyBones.Hips).ToAnchorInfo();
                case EntityAnchor.Head: return entity.anim.GetBoneTransform(HumanBodyBones.Head).ToAnchorInfo();
                case EntityAnchor.LeftHand: return entity.anim.GetBoneTransform(HumanBodyBones.LeftHand).ToAnchorInfo();
                case EntityAnchor.RightHand: return entity.anim.GetBoneTransform(HumanBodyBones.RightHand).ToAnchorInfo();
                case EntityAnchor.LeftFoot: return entity.anim.GetBoneTransform(HumanBodyBones.LeftFoot).ToAnchorInfo();
                case EntityAnchor.RightFoot: return entity.anim.GetBoneTransform(HumanBodyBones.RightFoot).ToAnchorInfo();

                case EntityAnchor.Bar: return EntityAnchor.Head.GetTransform(entity) + Vector3.up * 0.5f;
                case EntityAnchor.Name: return EntityAnchor.Head.GetTransform(entity) + Vector3.up * 0.5f;
                case EntityAnchor.UpCenter: return entity.anim.GetBoneTransform(HumanBodyBones.UpperChest).ToAnchorInfo();
                case EntityAnchor.UpFront: return entity.anim.GetBoneTransform(HumanBodyBones.UpperChest).ToAnchorInfo() + (BoundZ(entity).y+0.5f) * entity.transform.forward;
            }
        }
        switch (anchor)
        {
            case EntityAnchor.GameObjectPosition: return entity.transform.ToAnchorInfo();
            case EntityAnchor.WeaponSlot1:
            case EntityAnchor.WeaponSlot2:
            case EntityAnchor.WeaponSlot3:
            case EntityAnchor.WeaponSlot4:
            case EntityAnchor.WeaponSlot5:
            case EntityAnchor.WeaponSlot6:
            case EntityAnchor.WeaponSlot7:
            case EntityAnchor.WeaponSlot8:
                int weaponSlotIndex = (int)anchor - (int)EntityAnchor.WeaponSlot1;
                entity.springWeapon.GetPos(weaponSlotIndex, out var v, out var q);
                return new EntityAnchorInfo()
                {
                    transform = null,
                    position = v,
                    rotation = q
                };

            case EntityAnchor.Bar: return entity.transform.ToAnchorInfo() + (BoundY(entity).y + 0.5f) * Vector3.up;
            case EntityAnchor.Name: return entity.transform.ToAnchorInfo() + (BoundY(entity).y + 1f) * Vector3.up;
            case EntityAnchor.UpCenter: return entity.transform.ToAnchorInfo() + BoundY(entity).y*0.75f * Vector3.up;
            case EntityAnchor.UpFront: return entity.transform.ToAnchorInfo() + BoundY(entity).y*0.75f * Vector3.up+ (BoundZ(entity).y + 0.5f) * entity.transform.forward;
        }
        throw new System.Exception("Unknown anchor: " + anchor);
    }
    private static Vector2 BoundX(EntityData entity) => entity.ModelInfo.xRange;
    private static Vector2 BoundY(EntityData entity) => entity.ModelInfo.yRange;
    private static Vector2 BoundZ(EntityData entity) => entity.ModelInfo.zRange;
    private static EntityAnchorInfo ToAnchorInfo(this Transform transform,Vector3 offset = default)
    {
        if(offset.sqrMagnitude<0.001f)
        {
            return new EntityAnchorInfo()
            {
                transform = transform,
                position = transform.position,
                rotation = transform.rotation
            };
        }
        return new EntityAnchorInfo()
        {
            transform = null,
            position = transform.position + offset,
            rotation = transform.rotation
        };
    }
}