using UnityEngine;

// 回旋：前半程飞到最远点，后半程回到施法者"当前"位置
public class BoomerangTrajectory : BulletTrajectory
{
    private readonly ushort casterId;
    private readonly Vector3 fallbackCaster;
    private readonly Vector3 outPoint;

    public BoomerangTrajectory(ushort casterId, Vector3 fallbackCaster, Vector3 outPoint)
    {
        this.casterId = casterId;
        this.fallbackCaster = fallbackCaster;
        this.outPoint = outPoint;
    }

    public override Vector3 Lerp(float factor)
    {
        float f = Mathf.Clamp01(factor);
        Vector3 back = TryGetEntityPosition(casterId, out var pos) ? pos : fallbackCaster;
        if (f < 0.5f) return Vector3.Lerp(fallbackCaster, outPoint, f * 2f);
        return Vector3.Lerp(outPoint, back, (f - 0.5f) * 2f);
    }

    public override Vector3 End => outPoint;
}
