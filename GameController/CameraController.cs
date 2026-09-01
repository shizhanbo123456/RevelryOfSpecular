using UnityEngine;

/// <summary>
/// 相机控制器（客户端，第三人称跟随）。
/// WASD 相对相机移动依赖本类的 Yaw（InputManager 读取）。
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>相机朝向（欧拉角 Y，度）。</summary>
    public static float Yaw { get; set; } = 0f;

    /// <summary>相机俯仰（度）。</summary>
    public static float Pitch { get; set; } = 20f;

    /// <summary>跟随目标（本地玩家表现根物体）。</summary>
    public Transform lookTarget;

    /// <summary>相机距离。</summary>
    public float distance = 5f;

    /// <summary>目标偏移（头顶上方）。</summary>
    public Vector3 offset = new Vector3(0f, 1.6f, 0f);

    /// <summary>旋转灵敏度。</summary>
    public float rotateSpeed = 3f;

    /// <summary>俯仰范围。</summary>
    public float minPitch = -30f;
    public float maxPitch = 60f;

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
        if (InputManager.MouseLocked)
        {
            Yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            Pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            Pitch = Mathf.Clamp(Pitch, minPitch, maxPitch);
        }

        if (lookTarget == null)
        {
            // 无目标时保持初始位置（调试用）
            transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            return;
        }

        var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        transform.position = lookTarget.position + offset - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}
