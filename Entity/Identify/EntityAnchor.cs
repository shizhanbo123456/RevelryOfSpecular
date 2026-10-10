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
    public static EntityAnchorInfo GetTransform(this EntityAnchor anchor, EntityModelInfo model,EntityAnim anim,SpringWeapon springWeapon)
    {
        if (anim != null)
        {
            switch (anchor)
            {
                case EntityAnchor.ModelRootPosition: return anim.GetBoneTransform(HumanBodyBones.Hips).ToAnchorInfo();
                case EntityAnchor.Head: return anim.GetBoneTransform(HumanBodyBones.Head).ToAnchorInfo();
                case EntityAnchor.LeftHand: return anim.GetBoneTransform(HumanBodyBones.LeftHand).ToAnchorInfo();
                case EntityAnchor.RightHand: return anim.GetBoneTransform(HumanBodyBones.RightHand).ToAnchorInfo();
                case EntityAnchor.LeftFoot: return anim.GetBoneTransform(HumanBodyBones.LeftFoot).ToAnchorInfo();
                case EntityAnchor.RightFoot: return anim.GetBoneTransform(HumanBodyBones.RightFoot).ToAnchorInfo();
                case EntityAnchor.Chest: return anim.GetBoneTransform(HumanBodyBones.Chest).ToAnchorInfo();

                case EntityAnchor.Bar: return EntityAnchor.Head.GetTransform(model, anim,springWeapon) + Vector3.up * 0.5f;
                case EntityAnchor.Name: return EntityAnchor.Head.GetTransform(model, anim,springWeapon) + Vector3.up * 1f;
                case EntityAnchor.UpCenter: return EntityAnchor.Chest.GetTransform(model, anim,springWeapon);
                case EntityAnchor.UpFront: return EntityAnchor.Chest.GetTransform(model, anim,springWeapon) + (model.zRange.y+0.5f) * model.transform.forward;
            }
        }
        switch (anchor)
        {
            case EntityAnchor.GameObjectPosition: return model.transform.ToAnchorInfo();
            case EntityAnchor.WeaponSlot1:
            case EntityAnchor.WeaponSlot2:
            case EntityAnchor.WeaponSlot3:
            case EntityAnchor.WeaponSlot4:
            case EntityAnchor.WeaponSlot5:
            case EntityAnchor.WeaponSlot6:
            case EntityAnchor.WeaponSlot7:
            case EntityAnchor.WeaponSlot8:
                int weaponSlotIndex = (int)anchor - (int)EntityAnchor.WeaponSlot1;
                springWeapon.GetPos(weaponSlotIndex, out var v, out var q);
                return new EntityAnchorInfo()
                {
                    transform = null,
                    position = v,
                    rotation = q
                };

            case EntityAnchor.Bar: return model.transform.ToAnchorInfo() + (model.yRange.y + 0.5f) * Vector3.up;
            case EntityAnchor.Name: return model.transform.ToAnchorInfo() + (model.yRange.y + 1f) * Vector3.up;
            case EntityAnchor.UpCenter: return model.transform.ToAnchorInfo() + model.yRange.y*0.75f * Vector3.up;
            case EntityAnchor.UpFront: return model.transform.ToAnchorInfo() + model.yRange.y*0.75f * Vector3.up+ (model.zRange.y + 0.5f) * model.transform.forward;
        }
        throw new System.Exception("Unknown anchor: " + anchor);
    }
    private static EntityAnchorInfo ToAnchorInfo(this Transform transform,Vector3 offset = default)
    {
        if(offset.sqrMagnitude<float.Epsilon)
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