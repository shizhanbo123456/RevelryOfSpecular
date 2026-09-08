using UnityEngine;

/// <summary>
/// 相机控制器（客户端，第三人称跟随）。
/// 双手键盘方案：无鼠标操控，Yaw 跟随角色朝向（朝向由移动渐转在服务器权威推进），俯仰为固定配置值。
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>相机朝向（欧拉角 Y，度）——跟随角色当前朝向。</summary>
    public static float Yaw { get; set; } = 0f;

    /// <summary>相机俯仰（度，固定值）。</summary>
    public static float Pitch { get; set; } = 20f;

    /// <summary>跟随目标（本地玩家表现根物体）。</summary>
    public Transform lookTarget;

    /// <summary>相机距离。</summary>
    public float distance = 5f;

    /// <summary>目标偏移（头顶上方）。</summary>
    public Vector3 offset = new Vector3(0f, 1.6f, 0f);

    private void Awake()
    {
        Tool.CameraController = this;
    }

    /// <summary>设置跟随目标（本地玩家出生时调用）。</summary>
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
