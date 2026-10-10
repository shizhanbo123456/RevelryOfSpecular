using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform lookTarget;

    // 渲染用的相机组件（CameraController 挂在主相机 GameObject 上）
    public Camera WorldCamera { get; private set; }

    // 相机插值收尾后回调：UI 在此用最终相机变换投影，避免与渲染差一帧导致震颤
    public event System.Action OnCameraUpdated;

    public Vector2 yRange = new Vector2(1f, 3f);
    public Vector2 zRange = new Vector2(3f, 8f);

    public float slowSmooth = 2f;
    public float fastSmooth = 10f;
    // 高度越界缓冲带半宽：带内线性插值快慢平滑，超出带才用快速，避免速率阶跃震颤
    public float smoothDistance = 1.5f;
    // 绑定目标时的初始水平后距
    public float initialDistance = 5.5f;

    // 俯仰偏转平滑速率
    public float pitchSmooth = 10f;

    // 俯仰偏转（度，向下为正；0 = 正看锚点）：全程向目标值平滑，保持时间结束自动归零
    private float pitchOffset;
    private float pitchTarget;
    private float pitchHoldTime;

    private Vector3 lastLookPos; // 上一帧锚点位置：先把相机随实体平移，再量距离摆到正后方，避免行进中距离累积漂移

    private void Awake()
    {
        Tool.CameraController = this;
        WorldCamera = GetComponent<Camera>();
    }

    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
        if (target == null) return;
        // 绑定/换绑（进战斗、复活换绑）：直接摆到目标正后方默认距离
        lastLookPos = target.position;
        Vector3 back = Quaternion.Euler(0f, target.eulerAngles.y, 0f) * Vector3.back;
        float yCenter = (yRange.x + yRange.y) * 0.5f;
        transform.position = target.position + Vector3.up * yCenter + back * initialDistance;
        ApplyView();
        NotifyCameraUpdated();
    }

    // 俯仰偏转：保持 duration 秒后自动回归看向锚点；重复调用刷新保持时间
    public void SetPitch(float pitch, float duration = 0.1f)
    {
        pitchTarget = pitch;
        pitchHoldTime = duration;
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

        TickPitch();

        // 偏航不平滑：相机全程在角色正后方，自身偏航与角色一致
        float yaw = lookTarget.eulerAngles.y;
        Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;

        // 1) 先随实体平移（量距不受行进位移污染），2) 再量当前水平距离/高度
        Vector3 delta = lookTarget.position - lastLookPos;
        delta.y = 0f;
        transform.position += delta;
        lastLookPos = lookTarget.position;

        Vector3 toCam = transform.position - lookTarget.position;
        toCam.y = 0f;
        float zCur = toCam.magnitude;
        float yCur = transform.position.y - lookTarget.position.y;

        // 半径/高度双轴弹性平滑（区间内慢速、越界快速）
        float zRate = AxisRate(zCur, zRange, smoothDistance, slowSmooth, fastSmooth);
        float yRate = AxisRate(yCur, yRange, smoothDistance, slowSmooth, fastSmooth);
        float zCenter = (zRange.x + zRange.y) * 0.5f;
        float yCenter = (yRange.x + yRange.y) * 0.5f;
        float zNew = Mathf.Lerp(zCur, zCenter, 1f - Mathf.Exp(-zRate * Time.deltaTime));
        float yNew = Mathf.Lerp(yCur, yCenter, 1f - Mathf.Exp(-yRate * Time.deltaTime));

        transform.position = lookTarget.position + Vector3.up * yNew + back * zNew;
        ApplyView();
        NotifyCameraUpdated();
    }

    // 直接看向锚点（不平滑），再叠加平滑的俯仰偏转
    private void ApplyView()
    {
        transform.LookAt(lookTarget.position);
        if (pitchOffset != 0f) transform.rotation *= Quaternion.Euler(pitchOffset, 0f, 0f);
    }

    // 俯仰偏转全程向目标值平滑；保持时间结束后目标归零（回归看向锚点）
    private void TickPitch()
    {
        if (pitchHoldTime > 0f)
        {
            pitchHoldTime -= Time.deltaTime;
            if (pitchHoldTime <= 0f) pitchTarget = 0f;
        }
        pitchOffset = Mathf.Lerp(pitchOffset, pitchTarget, 1f - Mathf.Exp(-pitchSmooth * Time.deltaTime));
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
