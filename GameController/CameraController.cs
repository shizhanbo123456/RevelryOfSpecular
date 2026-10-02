using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform lookTarget;

    // 相机插值收尾后回调：UI 在此用最终相机变换投影，避免与渲染差一帧导致震颤
    public event System.Action OnCameraUpdated;

    public Vector2 zRange = new Vector2(3f, 8f);
    public Vector2 yRange = new Vector2(1f, 3f);

    public float slowSmooth = 2f;
    public float fastSmooth = 10f;

    // 俯仰角（度，向下为正）；相机看向与角色水平对齐、抬高 (y - z*tan(pitch)) 的点，而非脚底
    public float pitch = 15f;
    // 越界缓冲带半宽：带内线性插值快慢平滑，超出带才用快速，避免速率阶跃震颤
    public float smoothDistance = 1.5f;

    private float yaw;

    private void Awake()
    {
        Tool.CameraController = this;
    }

    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
        if (target != null) yaw = target.eulerAngles.y;
    }

    // 单轴平滑速率：带内 slow，带外 fast，过渡带内线性插值
    private float AxisRate(float v, Vector2 range, float d, float slow, float fast)
    {
        if (v < range.x)
        {
            if (d <= 0f || v <= range.x - d) return fast;
            return Mathf.Lerp(fast, slow, (v - (range.x - d)) / d);
        }
        if (v > range.y)
        {
            if (d <= 0f || v >= range.y + d) return fast;
            return Mathf.Lerp(slow, fast, (v - range.y) / d);
        }
        return slow;
    }

    private void LateUpdate()
    {
        if (lookTarget == null) return;

        float targetYaw = lookTarget.eulerAngles.y;
        yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-fastSmooth * Time.deltaTime));

        Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
        float zCenter = (zRange.x + zRange.y) * 0.5f;
        float yCenter = (yRange.x + yRange.y) * 0.5f;

        Vector3 toCam = transform.position - lookTarget.position;
        float yCur = toCam.y;
        float zCur = Vector3.Dot(toCam, back);

        float yRate = AxisRate(yCur, yRange, smoothDistance, slowSmooth, fastSmooth);
        float zRate = AxisRate(zCur, zRange, smoothDistance, slowSmooth, fastSmooth);

        float yNew = Mathf.Lerp(yCur, yCenter, 1f - Mathf.Exp(-yRate * Time.deltaTime));
        float zNew = Mathf.Lerp(zCur, zCenter, 1f - Mathf.Exp(-zRate * Time.deltaTime));

        transform.position = lookTarget.position + Vector3.up * yNew + back * zNew;
        float lookUp = yNew - zNew * Mathf.Tan(pitch * Mathf.Deg2Rad);
        transform.LookAt(lookTarget.position + Vector3.up * lookUp);
        if (OnCameraUpdated != null) OnCameraUpdated();
    }
}
