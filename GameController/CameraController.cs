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

    // 俯仰偏转平滑速率
    public float pitchSmooth = 10f;
    // 俯仰钳制范围（度，世界俯仰、0=水平）：TickPitch 里钳制目标角
    public Vector2 pitchClamp = new Vector2(-45f, 45f);
    
    public float xSmooth = 2f;

    // 自动取景：每 autoFrameInterval 遍历可见实体，把可见模型的俯仰跨度约束进视野；
    // 有越界时经 SetPitch 按周期续期修正，无越界时停止刷新、由保持时间自然到期回正
    public float autoFrameInterval = 0.2f;
    // 俯仰角自适应是否忽略玩家角色：忽略时看向敌人而非自身
    public bool autoPitchIgnoreSelf = true;

    private float autoFrameTimer;

    // 俯仰偏转
    private float pitchTarget;
    private float pitchHoldTime;

    private void Awake()
    {
        Tool.CameraController = this;
        WorldCamera = GetComponent<Camera>();
    }

    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
        if (target == null) return;

        Vector3 back = Quaternion.Euler(0f, target.eulerAngles.y, 0f) * Vector3.back;
        float yCenter = (yRange.x + yRange.y) * 0.5f;
        float zCenter = (zRange.x + zRange.y) * 0.5f;
        transform.position = target.position + Vector3.up * yCenter + back * zCenter;

        transform.LookAt(lookTarget.position);
        UpdateAutoPitch();
        TickPitch();

        NotifyCameraUpdated();
    }

    // 单轴平滑速率：带内 slow，带外 fast，过渡带内线性插值
    private float AxisRate(float v, Vector2 range, float smoothDistance, float slow, float fast)
    {
        if (v < range.x)
        {
            if (smoothDistance <= 0f || v <= range.x - smoothDistance) return fast;
            return Mathf.Lerp(fast, slow, (v - (range.x - smoothDistance)) / smoothDistance);
        }
        if (v > range.y)
        {
            if (smoothDistance <= 0f || v >= range.y + smoothDistance) return fast;
            return Mathf.Lerp(slow, fast, (v - range.y) / smoothDistance);
        }
        return slow;
    }

    private void LateUpdate()
    {
        if (lookTarget == null) return;

        // 偏航不平滑：相机全程在角色正后方，自身偏航与角色一致
        float yaw = lookTarget.eulerAngles.y;
        Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
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

        float x=Vector3.Dot(lookTarget.position - transform.position, lookTarget.right);
        float xNew=Mathf.Lerp(x, 0, 1f - Mathf.Exp(-xSmooth * Time.deltaTime));

        transform.position = lookTarget.position + Vector3.up * yNew + back * zNew - lookTarget.right * xNew;

        // 自动取景周期遍历
        autoFrameTimer += Time.deltaTime;
        if (autoFrameTimer >= autoFrameInterval)
        {
            autoFrameTimer = 0f;
            UpdateAutoPitch();
        }
        TickPitch();
        NotifyCameraUpdated();
    }

    // 自动取景：把所有可见模型的上下边界压进水平视野中间 80%，0 偏转够用时不干预（自然回正）
    // 装不下经 SetPitch 给最小修正并按周期续期；两侧都装不下取上下限平均
    private void UpdateAutoPitch()
    {
        var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
        if (players == null || WorldCamera == null || lookTarget == null) return;

        Vector3 camPos = transform.position;
        float vHalf = WorldCamera.fieldOfView * 0.5f;
        float hHalf = Mathf.Atan(Mathf.Tan(vHalf * Mathf.Deg2Rad) * WorldCamera.aspect) * Mathf.Rad2Deg;
        float bandTan = Mathf.Tan(hHalf * Mathf.Deg2Rad) * 0.8f; // 中间 80%：两侧各让出 10%

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return;
        fwd.Normalize();

        float pitchTop = float.MaxValue;    // 上限：最高的所需视线（数值最小，向下为正）
        float pitchBottom = float.MinValue; // 下限：最低的所需视线（数值最大）
        bool any = false;

        var loading = ClientLoadingManager.Current;
        foreach (var view in players.AllViews)
        {
            if (view == null) continue;
            if (autoPitchIgnoreSelf && loading != null && loading.HasSession
                && view.id == loading.OwnEntityId) continue; // 忽略玩家角色
            Vector3 p = view.transform.position;
            float dx = p.x - camPos.x;
            float dz = p.z - camPos.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < 0.05f) continue; // 与相机几乎同位置：水平视线角无定义

            // 俯仰近似为 0 的水平视野测试：只取水平视场中间 80% 的实体
            float angle = Vector2.SignedAngle(new Vector2(fwd.x, fwd.z), new Vector2(dx / dist, dz / dist));
            if (Mathf.Abs(angle) >= hHalf) continue;
            if (Mathf.Abs(Mathf.Tan(angle * Mathf.Deg2Rad)) > bandTan) continue;

            // 模型上下边界对应的俯仰角（向下为正）
            float pitchToTop = Mathf.Atan2(camPos.y - (p.y + view.modelInfo.yRange.y), dist) * Mathf.Rad2Deg;
            float pitchToBottom = Mathf.Atan2(camPos.y - (p.y + view.modelInfo.yRange.x), dist) * Mathf.Rad2Deg;
            if (pitchToTop < pitchTop) pitchTop = pitchToTop;
            if (pitchToBottom > pitchBottom) pitchBottom = pitchToBottom;
            any = true;
        }

        if (!any) return; // 本轮没有纳入取景的实体：不刷新，已持有的偏转到保持期结束自然回正

        // 0 偏转（正看锚点）时的视角中心俯仰
        Vector3 toAnchor = lookTarget.position - camPos;
        float anchorPitch = Mathf.Atan2(-toAnchor.y, new Vector2(toAnchor.x, toAnchor.z).magnitude) * Mathf.Rad2Deg;

        // 视角中心可行区间：上边缘 ≤ 上限 且 下边缘 ≥ 下限（均为世界俯仰角，与 pitchTarget 同口径）
        float fitLow = pitchBottom - vHalf;
        float fitHigh = pitchTop + vHalf;
        if (fitLow > fitHigh)
        {
            // 视场装不下：中心取上限与下限的平均值
            SetPitch((pitchTop + pitchBottom) * 0.5f, autoFrameInterval * 1.5f);
            return;
        }
        if (anchorPitch < fitLow) SetPitch(fitLow, autoFrameInterval * 1.5f); // 压低
        else if (anchorPitch > fitHigh) SetPitch(fitHigh, autoFrameInterval * 1.5f); // 抬起
        // 否则 0 偏转已完整覆盖：不刷新，自然回正
    }

    // 俯仰目标（世界俯仰角，度）：保持 duration 秒后自动回归看向锚点；重复调用刷新保持时间
    private void SetPitch(float pitch, float duration = 0.1f)
    {
        pitchTarget = pitch;
        pitchHoldTime = duration;
    }

    private void TickPitch()
    {
        if (pitchHoldTime > 0f)
        {
            pitchHoldTime -= Time.deltaTime;
        }
        else
        {
            var d = lookTarget.position - transform.position;
            float dh = d.y;
            float ds = Mathf.Sqrt(d.x * d.x + d.z * d.z);
            pitchTarget = -Mathf.Atan(dh / ds)*Mathf.Rad2Deg;
        }
        pitchTarget = Mathf.Clamp(pitchTarget, pitchClamp.x, pitchClamp.y);
        var eular = new Vector3(pitchTarget, lookTarget.rotation.eulerAngles.y);
        var targetQuaternion = Quaternion.Euler(eular);
        transform.rotation = Quaternion.Lerp(transform.rotation,targetQuaternion, 1f - Mathf.Exp(-pitchSmooth * Time.deltaTime));
    }

    // Unity 要到渲染才重算 worldToCameraMatrix，同帧 WorldToScreenPoint 会用上一帧矩阵（血条/名字差一帧震颤）
    // 故改完变换后手动重置，让随后的投影回调拿到本帧相机
    private void NotifyCameraUpdated()
    {
        if (WorldCamera != null) WorldCamera.ResetWorldToCameraMatrix();
        if (OnCameraUpdated != null) OnCameraUpdated();
    }
}
