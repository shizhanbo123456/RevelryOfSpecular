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
    private BulletTrajectory trajectory;
    private RotationMode rotation;
    private void Init(BulletTrajectory trajectory, float lifeTime, RotationMode rotation)
    {
        createTime = Time.time;
        oneMinusLifetime = 1f / lifeTime;
        this.trajectory = trajectory;
        transform.position = trajectory.Lerp(0);
        this.rotation = rotation;
        if (rotation == RotationMode.Identity) transform.rotation = Quaternion.identity;
        else if (rotation == RotationMode.CompleteTangent) transform.LookAt(trajectory.End);
    }
    private void Update()
    {
        float f = (Time.time - createTime) * oneMinusLifetime;
        transform.position = trajectory.Lerp(f);
        if (rotation == RotationMode.Tangent)
        {
            transform.LookAt(trajectory.Lerp(f + 0.02f));
        }
        else if (rotation == RotationMode.Camera)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
    public static void Create(GameObject vfx, BulletTrajectory trajectory, float lifeTime = 0f, RotationMode rotation = RotationMode.Constant)
    {
        if (vfx == null || trajectory == null) return;
        float life = lifeTime > 0f ? lifeTime : trajectory.Duration; // 未传时长则用轨迹自带的
        BulletPlayer player;
        if (!vfx.TryGetComponent(out player))
        {
            player = vfx.AddComponent<BulletPlayer>();
        }
        player.Init(trajectory, life, rotation);
        Destroy(player.gameObject, life);
    }
}
