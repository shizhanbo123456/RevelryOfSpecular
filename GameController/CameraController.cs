using Ros.Transport;
using UnityEngine;

public enum CameraViewMode
{
    ThirdPerson,
    TopDown,
    Free,
}

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

    // 俯视模式：相机相对跟随点的世界偏移（yaw 恒为 0），以及位置平滑速率
    public Vector3 topDownOffset = new Vector3(0f, 16f, -4f);
    public float topDownSmooth = 8f;

    // 自由视角（仅纯观战）：平移 / 升降 / 转向 / 俯仰速度与俯仰钳制范围
    public float freeMoveSpeed = 12f;
    public float freeVerticalSpeed = 8f;
    public float freeTurnSpeed = 90f;
    public float freePitchSpeed = 60f;
    public Vector2 freePitchClamp = new Vector2(-89f, 89f);
    // 自由视角位置上报：每 freeReportInterval 秒检查一次，与上次上报位置水平距离小于 freeReportDistance 则不发
    public float freeReportInterval = 1f;
    public float freeReportDistance = 1f;

    public CameraViewMode Mode { get; private set; } = CameraViewMode.ThirdPerson;

    public bool IsFree => Mode == CameraViewMode.Free;

    private float autoFrameTimer;

    // 俯仰偏转
    private float pitchTarget;
    private float pitchHoldTime;

    // 自由视角位姿
    private float freeYaw;
    private float freePitch;

    // 自由视角位置上报状态
    private float freeReportTimer;
    private Vector3 freeReportPos;
    private bool freeReportPending; // 进入自由视角后的首包（不等满一个上报周期）

    // 会话跟踪：新一局回第三人称，复活换实体后补发一次模式
    private bool sessionActive;
    private ushort lastOwnEntityId;
    private bool pendingModeSync; // 待同步给服务器的相机模式（等视体建好再发）

    private void Awake()
    {
        Tool.CameraController = this;
        WorldCamera = GetComponent<Camera>();
    }

    public void SetLookTarget(Transform target, bool snap = true)
    {
        lookTarget = target;
        if (target == null) return;

        // 俯视/自由视角下不摆位：俯视由 TickTopDown 平滑追新目标，自由视角不能被跟随点拽走
        if (!snap || Mode == CameraViewMode.Free) return;
        if (Mode == CameraViewMode.TopDown)
        {
            NotifyCameraUpdated();
            return;
        }
        SnapToThirdPerson();
        NotifyCameraUpdated();
    }

    // 瞬移到第三人称稳态位姿（绑定/换绑/从自由视角返回）
    private void SnapToThirdPerson()
    {
        Vector3 back = Quaternion.Euler(0f, lookTarget.eulerAngles.y, 0f) * Vector3.back;
        float yCenter = (yRange.x + yRange.y) * 0.5f;
        float zCenter = (zRange.x + zRange.y) * 0.5f;
        transform.position = lookTarget.position + Vector3.up * yCenter + back * zCenter;

        transform.LookAt(lookTarget.position);
        UpdateAutoPitch();
        TickPitch();
    }

    public void SetMode(CameraViewMode mode)
    {
        if (Mode == mode) return;
        bool fromFree = Mode == CameraViewMode.Free;
        Mode = mode;

        if (mode == CameraViewMode.Free)
        {
            freeYaw = transform.eulerAngles.y;
            freePitch = NormalizePitch(transform.eulerAngles.x);
            freeReportPending = true; // 首包立刻发，避免进入后 1s 内服务器仍按原实体下发
            freeReportTimer = 0f;
            return;
        }
        // 第三人称 ⇄ 俯视交给各自的 Tick 平滑过渡；从自由视角回来必须瞬移，否则会横穿整张地图
        if (fromFree) SendFreeCameraExit(); // 通知服务器把视野锚点交还跟随实体
        pitchHoldTime = 0f; // 丢弃切换前的取景保持，回第三人称时直接按当前几何重算俯仰
        if (fromFree && mode == CameraViewMode.ThirdPerson && lookTarget != null)
        {
            SnapToThirdPerson();
            NotifyCameraUpdated();
        }
    }

    private static float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }

    private void Update()
    {
        var loading = ClientLoadingManager.Current;
        TickSession(loading);
        if (!sessionActive) return;

        bool spectator = loading.IsSpectator;

        if (spectator && Mode != CameraViewMode.Free && Input.GetKeyDown(KeyCode.W))
        {
            SetMode(CameraViewMode.Free); // 纯观战：按 W 脱离跟随进自由视角
        }

        if (Input.GetKeyDown(Config.camera_mode_key))
        {
            switch (Mode)
            {
                case CameraViewMode.Free: SetMode(CameraViewMode.ThirdPerson); break;
                case CameraViewMode.ThirdPerson: SetMode(CameraViewMode.TopDown); break;
                case CameraViewMode.TopDown: SetMode(CameraViewMode.ThirdPerson); break;
            }
            if (!spectator) pendingModeSync = true; // 视体已在的下一帧就会发出
        }

        if (Mode == CameraViewMode.Free) TickFreeKeys();
    }

    // 会话起止与实体 id 变化：新一局回第三人称；复活/换实体后服务器侧模式需重建
    private void TickSession(ClientLoadingManager loading)
    {
        bool active = loading != null && loading.HasSession;
        if (active != sessionActive)
        {
            sessionActive = active;
            lastOwnEntityId = 0;
            pendingModeSync = false;
            Mode = CameraViewMode.ThirdPerson;
        }
        if (!active) return;

        ushort ownId = loading.OwnEntityId;
        if (ownId != lastOwnEntityId)
        {
            lastOwnEntityId = ownId;
            pendingModeSync = ownId != 0;
            return;
        }
        if (!pendingModeSync) return;
        if (loading.IsSpectator) { pendingModeSync = false; return; }

        // 视体真正建好后再发：实体尚未创建时服务器会丢弃这次同步（复活/开局都在换实体之后才建视体）
        var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
        if (players == null || players.GetView(ownId) == null) return;
        pendingModeSync = false;
        SendModeToServer();
    }

    private void SendModeToServer()
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendCameraMode(Mode == CameraViewMode.TopDown);
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
        if (Mode == CameraViewMode.Free)
        {
            TickFreeMove(Time.deltaTime);
            TickFreeReport(Time.deltaTime);
            NotifyCameraUpdated();
            return;
        }
        if (lookTarget == null) return;

        if (Mode == CameraViewMode.TopDown)
        {
            TickTopDown(Time.deltaTime);
            NotifyCameraUpdated();
            return;
        }

        TickThirdPerson(Time.deltaTime);

        NotifyCameraUpdated();
    }

    private void TickThirdPerson(float deltaTime)
    {
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
        float zNew = Mathf.Lerp(zCur, zCenter, 1f - Mathf.Exp(-zRate * deltaTime));
        float yNew = Mathf.Lerp(yCur, yCenter, 1f - Mathf.Exp(-yRate * deltaTime));

        float x=Vector3.Dot(lookTarget.position - transform.position, lookTarget.right);
        float xNew=Mathf.Lerp(x, 0, 1f - Mathf.Exp(-xSmooth * deltaTime));

        transform.position = lookTarget.position + Vector3.up * yNew + back * zNew - lookTarget.right * xNew;

        // 自动取景周期遍历
        autoFrameTimer += deltaTime;
        if (autoFrameTimer >= autoFrameInterval)
        {
            autoFrameTimer = 0f;
            UpdateAutoPitch();
        }
        TickPitch();
    }

    // 俯视：位置平滑追「跟随点 + 偏移」，朝向 LookAt 跟随点但偏航恒为 0（只保留俯角）
    private void TickTopDown(float deltaTime)
    {
        Vector3 target = lookTarget.position + topDownOffset;
        // 速率为 0 即视为不平滑（瞬时跟随），避免 1-Exp(-0) = 0 卡住不动
        float k = topDownSmooth > 0f ? 1f - Mathf.Exp(-topDownSmooth * deltaTime) : 1f;
        transform.position = Vector3.Lerp(transform.position, target, k);

        Vector3 d = lookTarget.position - transform.position;
        float flat = Mathf.Sqrt(d.x * d.x + d.z * d.z);
        float pitch = -Mathf.Atan2(d.y, flat) * Mathf.Rad2Deg;
        var targetRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-pitchSmooth * deltaTime));
    }

    #region 自由视角（仅纯观战）
    // 键位固定：WASD 水平平移 / K 升 J 降 / Q 左转 E 右转 / Y 抬头 H 低头
    private void TickFreeKeys()
    {
        float deltaTime = Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) freeYaw -= freeTurnSpeed * deltaTime;
        if (Input.GetKey(KeyCode.E)) freeYaw += freeTurnSpeed * deltaTime;
        if (Input.GetKey(KeyCode.Y)) freePitch -= freePitchSpeed * deltaTime;
        if (Input.GetKey(KeyCode.H)) freePitch += freePitchSpeed * deltaTime;
        freePitch = Mathf.Clamp(freePitch, freePitchClamp.x, freePitchClamp.y);
    }

    private void TickFreeMove(float deltaTime)
    {
        float yawRad = freeYaw * Mathf.Deg2Rad;
        Vector3 fwd = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
        Vector3 right = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));

        Vector3 flat = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) flat += fwd;
        if (Input.GetKey(KeyCode.S)) flat -= fwd;
        if (Input.GetKey(KeyCode.D)) flat += right;
        if (Input.GetKey(KeyCode.A)) flat -= right;
        if (flat.sqrMagnitude > 1e-6f) flat = flat.normalized * freeMoveSpeed;

        float vertical = 0f;
        if (Input.GetKey(KeyCode.K)) vertical += 1f;
        if (Input.GetKey(KeyCode.J)) vertical -= 1f;

        transform.position += (flat + Vector3.up * (vertical * freeVerticalSpeed)) * deltaTime;
        transform.rotation = Quaternion.Euler(freePitch, freeYaw, 0f);
    }

    // 服务器按上报位置下发该玩家周围的实体：位移不足阈值时省掉这一包，客户端继续沿用上一次的锚点
    private void TickFreeReport(float deltaTime)
    {
        freeReportTimer -= deltaTime;
        if (freeReportTimer > 0f) return;
        freeReportTimer = freeReportInterval;

        Vector3 pos = transform.position;
        if (!freeReportPending)
        {
            float dx = pos.x - freeReportPos.x, dz = pos.z - freeReportPos.z;
            if (dx * dx + dz * dz < freeReportDistance * freeReportDistance) return;
        }
        freeReportPending = false;
        freeReportPos = pos;
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendFreeCamera(new CSFreeCamera() { free = true, posX = pos.x, posZ = pos.z });
    }

    private void SendFreeCameraExit()
    {
        freeReportPending = false;
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendFreeCamera(new CSFreeCamera() { free = false });
    }
    #endregion

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

        // 角色水平朝向：只对角色前方的实体做取景，身后的实体会贴到镜头上、不该拽俯仰角
        Vector3 charFwd = Quaternion.Euler(0f, lookTarget.eulerAngles.y, 0f) * Vector3.forward;

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
            // 必须在角色前方（角色后方＝相机与角色之间或更远，全部跳过）
            if ((p.x - lookTarget.position.x) * charFwd.x + (p.z - lookTarget.position.z) * charFwd.z < 0f) continue;

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
