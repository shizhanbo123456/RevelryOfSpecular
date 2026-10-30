using UnityEngine;

public enum EntityAnchor
{
    //完全不依赖人物骨骼的
    GameObjectPosition,
    Center,
    LabelFallBack,//伤害飘字回退位置

    //可依赖人物骨骼，但也支持非人形的
    Bar,//头顶血条位置
    UpCenter,//中上中心
    UpFront,//中上前方
    Camera,//相机注视点

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
    public static EntityAnchorInfo GetTransform(this EntityAnchor anchor, EntityModelInfo model,EntityAnim anim)
    {
        if (anim != null)
        {
            switch (anchor)
            {
                case EntityAnchor.ModelRootPosition: return anim.GetBoneTransform(HumanBodyBones.Hips).ToAnchorInfo(model.transform.rotation);
                case EntityAnchor.Head: return anim.GetBoneTransform(HumanBodyBones.Head).ToAnchorInfo(model.transform.rotation);
                case EntityAnchor.LeftHand: return anim.GetBoneTransform(HumanBodyBones.LeftHand).ToAnchorInfo(model.transform.rotation);
                case EntityAnchor.RightHand: return anim.GetBoneTransform(HumanBodyBones.RightHand).ToAnchorInfo(model.transform.rotation);
                case EntityAnchor.LeftFoot: return anim.GetBoneTransform(HumanBodyBones.LeftFoot).ToAnchorInfo(model.transform.rotation);
                case EntityAnchor.RightFoot: return anim.GetBoneTransform(HumanBodyBones.RightFoot).ToAnchorInfo(model.transform.rotation);
                case EntityAnchor.Chest: return anim.GetBoneTransform(HumanBodyBones.Chest).ToAnchorInfo(model.transform.rotation);
                case EntityAnchor.Camera: return EntityAnchor.ModelRootPosition.GetTransform(model, anim) + (model.yRange.y*0.5f + 0.5f) * Vector3.up;

                case EntityAnchor.Bar: return EntityAnchor.Head.GetTransform(model, anim) + Vector3.up * 0.5f;
                case EntityAnchor.UpCenter: return EntityAnchor.Chest.GetTransform(model, anim);
                case EntityAnchor.UpFront: return EntityAnchor.Chest.GetTransform(model, anim) + (model.zRange.y+0.5f) * model.transform.forward;
            }
        }
        switch (anchor)
        {
            case EntityAnchor.GameObjectPosition: return model.transform.ToAnchorInfo();
            case EntityAnchor.Center: return model.transform.ToAnchorInfo() + (model.yRange.x + model.yRange.y) * 0.5f * Vector3.up;
            case EntityAnchor.LabelFallBack: return model.transform.ToAnchorInfo() + (model.yRange.y + 0.75f) * Vector3.up;

            case EntityAnchor.Bar: return model.transform.ToAnchorInfo() + (model.yRange.y + 0.5f) * Vector3.up;
            case EntityAnchor.UpCenter: return model.transform.ToAnchorInfo() + model.yRange.y*0.75f * Vector3.up;
            case EntityAnchor.UpFront: return model.transform.ToAnchorInfo() + model.yRange.y*0.75f * Vector3.up+ (model.zRange.y + 0.5f) * model.transform.forward;
            case EntityAnchor.Camera: return model.transform.ToAnchorInfo() + (model.yRange.y * 0.5f + 0.5f) * Vector3.up;
        }
        throw new System.Exception("Unknown anchor: " + anchor);
    }
    // 骨骼锚点专用：旋转取骨骼所属实体（模型根），不取骨骼自身动画旋转
    private static EntityAnchorInfo ToAnchorInfo(this Transform transform, Quaternion entityRotation, Vector3 offset = default)
    {
        if(offset.sqrMagnitude<float.Epsilon)
        {
            return new EntityAnchorInfo()
            {
                transform = transform,
                position = transform.position,
                rotation = entityRotation
            };
        }
        return new EntityAnchorInfo()
        {
            transform = null,
            position = transform.position + offset,
            rotation = entityRotation
        };
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