using Ros.Transport;
using UnityEngine;

/// <summary>
/// 输入管理器（客户端）。
/// 操作方案见策划案 10.2：WASD 相对相机移动 / 鼠标转动视角 / 滚轮循环选技能 /
/// 左键近身攻击 / 右键触发远程/施法类技能（选中非远程时阻断并提示）/ Shift 滑铲（无格挡）。
/// 输入 → CSInputCommand → NetworkManager.SendInputCommand（服务器权威处理）。
/// </summary>
public class InputManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InputManager = this;
    }

    #region 静态输入状态（每帧刷新，供其它模块读取）
    /// <summary>移动输入（相对相机，x 横向 z 纵向，范围 -1~1）。</summary>
    public static Vector2 MoveInput { get; private set; }

    /// <summary>左键近身攻击按下（本帧）。</summary>
    public static bool MeleePressed { get; private set; }

    /// <summary>右键技能触发按下（本帧）。</summary>
    public static bool SkillPressed { get; private set; }

    /// <summary>Shift 滑铲按下（本帧）。</summary>
    public static bool SlidePressed { get; private set; }

    /// <summary>滚轮增量（本帧累计，>0 下一技能 <0 上一技能）。</summary>
    public static int SkillScrollDelta { get; private set; }

    /// <summary>屏幕中心瞄准点（世界坐标）。</summary>
    public static Vector3 AimPoint { get; private set; }

    /// <summary>是否锁定鼠标（默认锁定，Esc 切换）。</summary>
    public static bool MouseLocked { get; private set; } = true;
    #endregion

    private void Update()
    {
        RefreshInputStates();
        SendMoveCommand();
    }

    private void RefreshInputStates()
    {
        // 鼠标锁定切换
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MouseLocked = !MouseLocked;
            Cursor.visible = !MouseLocked;
            Cursor.lockState = MouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        // 移动（相对相机）
        float x = 0f, z = 0f;
        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        MoveInput = new Vector2(x, z).normalized;

        MeleePressed = Input.GetMouseButtonDown(0);
        SkillPressed = Input.GetMouseButtonDown(1);
        SlidePressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        SkillScrollDelta = Mathf.RoundToInt(Input.mouseScrollDelta.y);

        // 瞄准点：屏幕中心射线（TODO: 命中层掩码与地形高度修正）
        AimPoint = GetCenterAimPoint();
    }

    private void SendMoveCommand()
    {
        if (Tool.NetworkManager == null) return;
        var command = new CSInputCommand()
        {
            moving = MoveInput.sqrMagnitude > 0.01f,
            yaw = CameraController.Yaw,
            moveDir = MoveInput,
            meleePressed = MeleePressed,
            skillPressed = SkillPressed,
            slidePressed = SlidePressed,
            skillScrollDelta = SkillScrollDelta,
            selectedSkillId = GetSelectedSkillId(),
            aimPoint = AimPoint,
        };
        Tool.NetworkManager.SendInputCommand(command);
    }

    /// <summary>当前选中技能 id（本地技能运行时缓存，由服务器下发维护）。</summary>
    private int GetSelectedSkillId()
    {
        if (Tool.ClientLogicManager != null)
        {
            return Tool.ClientLogicManager.SelectedSkillId;
        }
        return -1;
    }

    /// <summary>屏幕中心世界瞄准点（供技能目标/武器瞄准使用）。</summary>
    public static Vector3 GetCenterAimPoint()
    {
        var cam = Camera.main;
        if (cam == null) return Vector3.zero;
        var ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        // TODO: 用地形/碰撞层获取命中点，当前退化为 100m 远点
        return ray.origin + ray.direction * 100f;
    }
}
