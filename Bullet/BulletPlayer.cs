using UnityEngine;

public class BulletPlayer : MonoBehaviour
{
    public enum RotationMode
    {
        Constant,
        Tangent,
        Camera,
        Identity,
        CompleteTangent
    }

    private float createTime;
    private float oneMinusLifetime;
    private BezierCurve curve;
    private RotationMode rotation;
    private void Init(BezierCurve curve, float lifeTime, RotationMode rotation)
    {
        createTime = Time.time;
        oneMinusLifetime = 1f / lifeTime;
        this.curve= curve;
        transform.position=curve.Lerp(0);
        this.rotation = rotation;
        if (rotation == RotationMode.Identity) transform.rotation = Quaternion.identity;
        else if (rotation == RotationMode.CompleteTangent) transform.LookAt(curve.Lerp(1));
    }
    private void Update()
    {
        float f = (Time.time - createTime) * oneMinusLifetime;
        transform.position = curve.Lerp(f);
        if (rotation == RotationMode.Tangent)
        {
            transform.LookAt(curve.Lerp(f + 0.02f));
        }
        else if (rotation == RotationMode.Camera)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
    public static void Create(GameObject vfx,BezierCurve curve,float lifeTime,RotationMode rotation=RotationMode.Constant)
    {
        BulletPlayer player;
        if(!vfx.TryGetComponent(out player))
        {
            player=vfx.AddComponent<BulletPlayer>();
        }
        player.Init(curve,lifeTime,rotation);
        Destroy(player.gameObject, lifeTime);
    }
}