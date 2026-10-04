using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform lookTarget;

    // 渲染用的相机组件（CameraController 挂在主相机 GameObject 上）
    public Camera WorldCamera { get; private set; }

    // 相机插值收尾后回调：UI 在此用最终相机变换投影，避免与渲染差一帧导致震颤
    public event System.Action OnCameraUpdated;

    public Vector2 zRange = new Vector2(3f, 8f);
    public Vector2 yRange = new Vector2(1f, 3f);

    public float slowSmooth = 2f;
    public float fastSmooth = 10f;
    // 旋转(绕角色水平角)平滑速率：独立配置，快转回后方时手感与位置平滑解耦
    public float yawSmooth = 10f;

    // 俯仰角（度，向下为正）；相机看向与角色水平对齐、抬高 (y - z*tan(pitch)) 的点，而非脚底
    public float pitch = 15f;
    // 越界缓冲带半宽：带内线性插值快慢平滑，超出带才用快速，避免速率阶跃震颤
    public float smoothDistance = 1.5f;
    // 瞬移阈值：相机与理想机位的距离超过该值时直接吸附，不做平滑（覆盖从预览机位进战斗、复活换出生点）
    public float snapDistance = 10f;

    private float yaw;

    private void Awake()
    {
        Tool.CameraController = this;
        WorldCamera = GetComponent<Camera>();
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

        // 理想机位（yaw 取目标当前朝向）：与当前位置超过瞬移阈值就直接吸附，跳过平滑
        Vector3 idealBack = Quaternion.Euler(0f, targetYaw, 0f) * Vector3.back;
        float zCenter = (zRange.x + zRange.y) * 0.5f;
        float yCenter = (yRange.x + yRange.y) * 0.5f;
        Vector3 idealPos = lookTarget.position + Vector3.up * yCenter + idealBack * zCenter;
        if ((transform.position - idealPos).sqrMagnitude > snapDistance * snapDistance)
        {
            yaw = targetYaw;
            transform.position = idealPos;
            transform.LookAt(lookTarget.position + Vector3.up * (yCenter - zCenter * Mathf.Tan(pitch * Mathf.Deg2Rad)));
            NotifyCameraUpdated();
            return;
        }

        yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-yawSmooth * Time.deltaTime));

        Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;

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
        NotifyCameraUpdated();
    }

    // LateUpdate 改完相机变换后，worldToCameraMatrix 不会立即刷新（Unity 在渲染时才重算），
    // 导致同帧 WorldToScreenPoint 仍用上一帧矩阵——平滑相机一直在动，血条/名字就会差一帧震颤。
    // 强制刷新当前矩阵，让随后的投影回调拿到本帧相机，消除震颤。
    private void NotifyCameraUpdated()
    {
        if (WorldCamera != null) WorldCamera.ResetWorldToCameraMatrix();
        if (OnCameraUpdated != null) OnCameraUpdated();
    }
}
