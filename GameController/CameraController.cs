using UnityEngine;

public class CameraController : MonoBehaviour
{
    /// <summary>相机模式：第三人称轨道 / 俯视</summary>
    public enum CameraMode
    {
        ThirdPerson,
        TopDown,
    }

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private float yaw;
    private float pitch;

    /// <summary>当前相机模式（默认第三人称）</summary>
    public CameraMode Mode { get; private set; } = CameraMode.ThirdPerson;
    /// <summary>当前相机旋转中心（LookAt 目标 = 本地玩家位置 + look_height）。供技能瞄准等外部系统判定"命中点是否在玩家背后"。</summary>
    public Vector3 LookTarget { get; private set; }
    /// <summary>俯视模式下相机相对玩家的固定偏移</summary>
    private static readonly Vector3 TopDownOffset = new Vector3(0f, 10f, -4f);

    /// <summary>
    /// 相机当前水平朝向：
    /// 第三人称 = 轨道 yaw（鼠标拖拽值）；俯视 = 相机 transform 实际朝向（LookAt 玩家后的朝向）。
    /// InputManager 基于此计算 WASD 移动方向，保证两种模式下"相对相机"移动方向正确。
    /// </summary>
    public float Yaw
    {
        get
        {
            if (Mode == CameraMode.TopDown) return Mathf.Repeat(transform.eulerAngles.y, 360f);
            return Mathf.Repeat(yaw, 360f);
        }
    }

    private void Awake()
    {
        Tool.CameraController = this;
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
        yaw = transform.eulerAngles.y;
        pitch = Config.battle_camera_default_pitch;

        EventManager.AddEvent(ClientEvent.OnRestartGame, ResetToDefault);
        EventManager.AddEvent(ClientEvent.ExitMax, ResetToDefault);
        EventManager.AddEvent(ClientEvent.ExitNormal, ResetToDefault);
        EventManager.AddEvent(ClientEvent.ExitMin, ResetToDefault);
        EventManager.AddEvent(ClientEvent.ExitLost, ResetToDefault);
        // 角色位置更新完成后跟随玩家；随后通知血条等 UI 系统刷新（保证用本帧相机位置做世界→屏幕转换）
        EventManager.AddEvent(ClientEvent.OnPostEntityPlayerUpdate, OnPostEntityPlayerUpdate);
    }

    private void OnDestroy()
    {
        if (Tool.CameraController == this) Tool.CameraController = null;
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, ResetToDefault);
        EventManager.RemoveEvent(ClientEvent.ExitMax, ResetToDefault);
        EventManager.RemoveEvent(ClientEvent.ExitNormal, ResetToDefault);
        EventManager.RemoveEvent(ClientEvent.ExitMin, ResetToDefault);
        EventManager.RemoveEvent(ClientEvent.ExitLost, ResetToDefault);
        EventManager.RemoveEvent(ClientEvent.OnPostEntityPlayerUpdate, OnPostEntityPlayerUpdate);
    }

    private void LateUpdate()
    {
        // B 键（或 Inspector 配置的按键）切换相机模式：放 LateUpdate 确保 InputManager 已刷新本帧输入。
        // 相机位置更新已移至 OnPostEntityPlayerUpdate 事件（角色位置更新后立即跟随）。
        if (Tool.InputManager != null && Tool.InputManager.ToggleCameraPressed)
        {
            ToggleMode();
        }
    }

    /// <summary>
    /// 角色位置更新完成后回调：跟随本地玩家更新相机位置，随后触发 OnPostCameraControllerUpdate，
    /// 让血条等依赖本帧相机位置的 UI 系统在同一帧内完成刷新。
    /// 无本地玩家位置时相机保持不动，但仍触发下游事件（保证血条列表每帧照常刷新）。
    /// </summary>
    private void OnPostEntityPlayerUpdate()
    {
        if (NetworkManager.CanSendWorldCommand &&
            Tool.ClientLogicManager != null &&
            Tool.ClientLogicManager.TryGetLocalPlayerPosition(out var playerPosition))
        {
            if (Mode == CameraMode.TopDown)
            {
                RefreshTopDownPosition(playerPosition);
            }
            else
            {
                UpdateBattleYaw();
                RefreshBattlePosition(playerPosition);
            }
        }

        EventManager.TrigEvent(ClientEvent.OnPostCameraControllerUpdate);
    }

    /// <summary>
    /// 切换相机模式。
    /// 俯视 → 第三人称时，把 yaw 重置为角色朝向，相机初始位置落在角色后方。
    /// </summary>
    public void ToggleMode()
    {
        if (Mode == CameraMode.ThirdPerson)
        {
            Mode = CameraMode.TopDown;
        }
        else
        {
            Mode = CameraMode.ThirdPerson;
            // 相机位于角色 forward 的反方向（即角色背后），因此 yaw = 角色朝向
            yaw = GetPlayerYaw();
        }
    }

    /// <summary>获取本地玩家当前朝向角（yaw），失败时保持现状。</summary>
    private float GetPlayerYaw()
    {
        var logic = Tool.ClientLogicManager;
        if (logic == null) return yaw;
        int playerId = NetworkManager.roomInfo.playerEntityId;
        if (logic.EntityInfos != null && logic.EntityInfos.TryGetValue(playerId, out var info))
        {
            return Mathf.Repeat(info.yaw, 360f);
        }
        return yaw;
    }

    /// <summary>俯视模式：相机相对玩家固定 (0,10,-4)，看向玩家。</summary>
    private void RefreshTopDownPosition(Vector3 playerPosition)
    {
        var lookTarget = playerPosition + Vector3.up * Config.battle_camera_look_height;
        LookTarget = lookTarget;
        transform.position = playerPosition + TopDownOffset;
        transform.LookAt(lookTarget);
    }

    public void ResetToDefault()
    {
        Mode = CameraMode.ThirdPerson;
        transform.SetPositionAndRotation(defaultPosition, defaultRotation);
        yaw = defaultRotation.eulerAngles.y;
        pitch = Config.battle_camera_default_pitch;
    }

    /// <summary>
    /// 第三人称视角转动：鼠标滑动即转（无需按键），直接使用 InputManager.MouseDelta。
    /// </summary>
    private void UpdateBattleYaw()
    {
        var input = Tool.InputManager;
        if (input == null) return;

        float mouseX = input.MouseDelta.x;
        float mouseY = input.MouseDelta.y;

        if (Mathf.Abs(mouseX) >= 0.001f)
        {
            yaw = Mathf.Repeat(yaw + mouseX * Config.battle_camera_drag_rotate_speed_horizontal*Time.deltaTime, 360f);
        }
        if (Mathf.Abs(mouseY) >= 0.001f)
        {
            pitch = Mathf.Clamp(pitch + mouseY * Config.battle_camera_drag_rotate_speed_vertical*Time.deltaTime,
                Config.battle_camera_pitch_min,
                Config.battle_camera_pitch_max);
        }
    }

    private void RefreshBattlePosition(Vector3 playerPosition)
    {
        Vector3 lookTarget = playerPosition + Vector3.up * Config.battle_camera_look_height;
        LookTarget = lookTarget;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraPosition = lookTarget - rotation * Vector3.forward * Config.battle_camera_distance;
        transform.position = cameraPosition;
        transform.LookAt(lookTarget);
    }
}
