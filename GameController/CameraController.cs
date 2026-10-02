using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static float Yaw { get; set; } = 0f;

    public static float Pitch { get; set; } = 20f;

    public Transform lookTarget;

    public float distance = 5f;

    public Vector3 offset = new Vector3(0f, 1.6f, 0f);

    private void Awake()
    {
        Tool.CameraController = this;
    }

    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
    }

    private void LateUpdate()
    {
        if (lookTarget == null) return;

        // 视角跟随角色朝向（双手键盘：朝向由移动渐转权威推进，客户端随表现同步/推演）
        Yaw = lookTarget.eulerAngles.y;
        var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        transform.position = lookTarget.position + offset - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}
