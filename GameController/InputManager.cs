using Ros.Transport;
using UnityEngine;

/// <summary>
/// 输入管理器（客户端，双手键盘方案，无鼠标操控）：
/// W/S 前后移动 / A/D 左右移动 / 前后+左右同按 = 向前后移动并逐渐转向（转向由服务器权威推进）/
/// J 空手攻击（静止=跃起砸地，移动=出拳）/ K 跳跃 / 左 Shift 滑铲 / U I O L H 技能槽 1~5 触发
/// （技能释放走 CSUseSkillRequest 单独发送）。
/// 瞄准点 = 自动索敌最近可见敌人，没有则取角色前方。
/// 输入 → CSInputCommand → NetworkManager.SendInputCommand（服务器权威处理）。
/// </summary>
public class InputManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InputManager = this;
    }

    #region 静态输入状态（每帧刷新，供其它模块读取）
    /// <summary>原始按键输入（相对角色自身：x = 左右横移 -1~1，z = 前后 -1~1）。</summary>
    public static Vector2 MoveInput { get; private set; }

    /// <summary>空手攻击按下（本帧，J 键）。</summary>
    public static bool MeleePressed { get; private set; }

    /// <summary>跳跃按下（本帧，K 键）。</summary>
    public static bool JumpPressed { get; private set; }

    /// <summary>滑铲按下（本帧，左 Shift）。</summary>
    public static bool SlidePressed { get; private set; }

    /// <summary>本帧按下的技能槽下标（-1 = 无；0~4 对应 U I O L H）。</summary>
    public static int SkillSlotPressed { get; private set; } = -1;

    /// <summary>瞄准点（世界坐标）。</summary>
    public static Vector3 AimPoint { get; private set; }
    #endregion

    private void Update()
    {
        RefreshInputStates();
        SendMoveCommand();
        SendSkillRequests();
    }

    private void RefreshInputStates()
    {
        // 原始按键输入（相对角色自身，不做相机相对换算）
        float x = 0f, z = 0f;
        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        MoveInput = new Vector2(x, z).normalized;

        MeleePressed = Input.GetKeyDown(Config.melee_key);
        JumpPressed = Input.GetKeyDown(Config.jump_key);
        SlidePressed = Input.GetKeyDown(Config.slide_key);

        SkillSlotPressed = -1;
        for (int i = 0; i < Config.skill_slot_keys.Length; i++)
        {
            if (Input.GetKeyDown(Config.skill_slot_keys[i]))
            {
                SkillSlotPressed = i;
                break;
            }
        }

        // 瞄准点：自动索敌最近可见敌人，没有则取角色前方
        AimPoint = GetAimPoint();
    }

    private void SendMoveCommand()
    {
        if (Tool.NetworkManager == null) return;
        var command = new CSInputCommand()
        {
            moving = MoveInput.sqrMagnitude > 0.01f,
            moveDir = MoveInput,
            meleePressed = MeleePressed,
            jumpPressed = JumpPressed,
            slidePressed = SlidePressed,
            aimPoint = AimPoint,
        };
        Tool.NetworkManager.SendInputCommand(command);
    }

    /// <summary>技能槽触发：按槽位取技能 id，单独发送技能释放请求。</summary>
    private void SendSkillRequests()
    {
        if (SkillSlotPressed < 0 || Tool.NetworkManager == null) return;
        int skillId = GetSlotSkillId(SkillSlotPressed);
        if (skillId < 0) return;
        Tool.NetworkManager.SendUseSkill(new CSUseSkillRequest(skillId, AimPoint));
    }

    /// <summary>按槽位下标取技能 id（本地技能运行时缓存，由服务器下发维护）。</summary>
    private static int GetSlotSkillId(int slot)
    {
        var display = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.LocalDisplay : null;
        if (display == null || slot < 0 || slot >= display.skills.Count) return -1;
        var slotData = display.skills[slot];
        return slotData != null ? slotData.skillId : -1;
    }

    #region//Local
    /// <summary>
    /// 瞄准点：自动索敌最近可见敌人（不同阵营、可见距离内），没有则取角色前方。
    /// </summary>
    private static Vector3 GetAimPoint()
    {
        if (Tool.ClientLogicManager == null || Tool.ClientDisplayManager == null) return Vector3.zero;
        if (!Tool.ClientLogicManager.TryGetLocalPlayerPosition(out var playerPos)) return Vector3.zero;

        float viewDistance = GetLocalViewDistance();
        if (Tool.ClientDisplayManager.TryGetNearestEnemyPosition(playerPos, viewDistance,
                (EntityCamp)Tool.ClientLogicManager.LocalCamp, out var enemyPos))
        {
            return enemyPos;
        }

        // 没有可见敌人：取角色前方（朝向由服务器渐转权威推进，读本地玩家表现物体 forward）
        if (Tool.ClientDisplayManager.TryGetEntityTransform(Tool.ClientLogicManager.LocalPlayerEntityId, out var t))
        {
            Vector3 forward = t.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f) return playerPos + forward.normalized * 10f;
        }
        return playerPos;
    }

    /// <summary>本地玩家可见距离（来自角色属性配置）。</summary>
    private static float GetLocalViewDistance()
    {
        var battleInfo = NetworkManager.battleInfo;
        if (battleInfo == null || Tool.InfoManager == null) return Config.default_skill_auto_target_radius;
        var attr = Tool.InfoManager.GetAttribute(battleInfo.characterType, battleInfo.characterLevel);
        return attr != null && attr.viewDistance > 0f ? attr.viewDistance : Config.default_skill_auto_target_radius;
    }
    #endregion
}
