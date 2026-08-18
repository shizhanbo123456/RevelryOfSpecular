using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Tooltip("切换相机模式（第三人称/俯视）的按键")]
    [SerializeField] private KeyCode toggleCameraKey = KeyCode.B;

    private CSInputCommand moveCommand;

    // ---- 统一输入状态（每帧由 RefreshInputStates 刷新，所有获取 Input 的地方从这里访问）----
    /// <summary>WASD 移动方向（未归一化，y=前后 x=左右）</summary>
    public Vector2 MoveInput { get; private set; }
    /// <summary>相机切换键按下（本帧）</summary>
    public bool ToggleCameraPressed { get; private set; }
    /// <summary>鼠标移动增量（第三人称视角转动直接使用，无需按键）</summary>
    public Vector2 MouseDelta { get; private set; }
    /// <summary>左键按下（第三人称=屏幕中央技能，俯视=最近敌人）</summary>
    public bool LeftSkillPressed { get; private set; }
    /// <summary>右键按下（第三人称=最近敌人，俯视=最近敌人）</summary>
    public bool RightSkillPressed { get; private set; }
    /// <summary>滚轮增量（技能切换输入，是否切换由技能管理器处理）</summary>
    public float SkillScroll { get; private set; }

    private void Awake()
    {
        if (Application.platform == RuntimePlatform.WindowsServer)
        {
            enabled = false;
            return;
        }
        Tool.InputManager = this;
        ResetMoveCommand();
        EventManager.AddEvent(ClientEvent.OnEnterWorld, ResetMoveCommand);
        EventManager.AddEvent(ClientEvent.OnRestartGame, ResetMoveCommand);
    }

    private void OnDestroy()
    {
        if (Tool.InputManager == this) Tool.InputManager = null;
        EventManager.RemoveEvent(ClientEvent.OnEnterWorld, ResetMoveCommand);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, ResetMoveCommand);
    }

    private void Update()
    {
        RefreshInputStates();
        if (!NetworkManager.CanSendWorldCommand) return;
        UpdateMoveCommand();
    }

    /// <summary>
    /// 每帧刷新统一输入状态缓存（不依赖 CanSendWorldCommand，保证 CameraController 等始终可读）。
    /// </summary>
    private void RefreshInputStates()
    {
        Vector2 move = Vector2.zero;
        if (Input.GetKey(Config.move_forward_key)) move.y += 1f;
        if (Input.GetKey(Config.move_backward_key)) move.y -= 1f;
        if (Input.GetKey(Config.move_right_key)) move.x += 1f;
        if (Input.GetKey(Config.move_left_key)) move.x -= 1f;
        MoveInput = move;

        ToggleCameraPressed = Input.GetKeyDown(toggleCameraKey);
        MouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        LeftSkillPressed = Input.GetMouseButtonDown(0);
        RightSkillPressed = Input.GetMouseButtonDown(Config.target_position_mouse_button);
        SkillScroll = Input.mouseScrollDelta.y;
    }

    private void UpdateMoveCommand()
    {
        Vector2 input = MoveInput;
        bool moving = input.sqrMagnitude > 0.01f;
        float yaw = moveCommand.yaw;
        if (moving)
        {
            input.Normalize();
            float cameraYaw = Tool.CameraController == null ? 0f : Tool.CameraController.Yaw;
            Vector3 forward = Quaternion.Euler(0, cameraYaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, cameraYaw, 0) * Vector3.right;
            Vector3 direction = forward * input.y + right * input.x;
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        CSInputCommand next = new CSInputCommand(moving, yaw);

        if (next.moving == moveCommand.moving &&
            Mathf.Abs(Mathf.DeltaAngle(next.yaw, moveCommand.yaw)) < 0.1f)
        {
            return;
        }
        moveCommand = next;
        Tool.NetworkManager?.SendMoveCommand(moveCommand);
    }

    private void ResetMoveCommand()
    {
        moveCommand = new CSInputCommand(false, 0f);
    }

    /// <summary>
    /// 获取屏幕中心瞄准点（射线找地面，技能定位辅助；由技能管理器调用）。
    /// 使用 InfoManager 配置的 SkillTargetLayerMask（需在 Inspector 排除实体层，避免命中角色/NPC）。
    /// 额外跳过"玩家背后的命中"：命中点若比旋转中心（CameraController.LookTarget）更靠近相机至少
    /// center_skill_occlude_gap 米，说明射线在到达玩家前被（玩家背后的墙）挡住，命中点位于玩家背后，直接跳过，
    /// 从而防止玩家背对墙时技能释放到玩家背后。
    /// </summary>
    public bool TryGetCenterSkillPosition(out Vector3 position)
    {
        position = Vector3.zero;
        var mainCamera = Camera.main;
        if (mainCamera == null) return false;

        LayerMask mask = Tool.InfoManager.SkillTargetLayerMask;

        // 旋转中心 A（相机 LookAt 目标）与相机位置 C：|AC| 与命中点 |BC| 的距离差用于判定"命中点在玩家背后"
        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 center = cameraPos;
        bool hasCenter = false;
        if (Tool.CameraController != null && Tool.CameraController.LookTarget != Vector3.zero)
        {
            center = Tool.CameraController.LookTarget;
            hasCenter = true;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, s_skillHits, Config.center_skill_ray_distance, mask, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            // 命中点比旋转中心更靠近相机至少阈值（m）→ 在玩家背后（被背后的墙遮挡），跳过
            if (hasCenter &&
                Vector3.Distance(cameraPos, center) - Vector3.Distance(cameraPos, s_skillHits[i].point) >= Config.center_skill_occlude_gap)
            {
                continue;
            }
            if (s_skillHits[i].distance < bestDist)
            {
                bestDist = s_skillHits[i].distance;
                position = s_skillHits[i].point;
            }
        }
        if (bestDist < float.MaxValue) return true;

        position = ray.GetPoint(Config.center_skill_ray_distance);
        return true;
    }

    private static readonly RaycastHit[] s_skillHits = new RaycastHit[16];
}


