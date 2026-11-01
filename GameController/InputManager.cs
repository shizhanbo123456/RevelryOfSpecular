using Ros.Transport;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private bool wHeld;
    private bool aHeld;
    private bool sHeld;
    private bool dHeld;
    private bool inputBlocked; // GM 面板打开期间屏蔽上行；进入屏蔽的那一帧先把按键全部抬起

    private void Awake()
    {
        Tool.InputManager = this;
    }

    private void Update()
    {
        SendInput();
    }

    // 唯一的键盘占用判定：UI 占用期间（GM 面板 / 文字输入），角色与相机都不再收键
    public static bool KeysCapturedByUi => Tool.UIManager != null && Tool.UIManager.KeyboardCaptured;

    private void SendInput()
    {
        if (Tool.NetworkManager == null) return;

        if (KeysCapturedByUi)
        {
            if (!inputBlocked)
            {
                inputBlocked = true;
                ReleaseHeldKeys(); // 否则服务器仍认为按键按住，角色会一直走
            }
            return;
        }
        inputBlocked = false;

        PlayerKey pressed = PlayerKey.None;

        if (Input.GetKeyDown(KeyCode.W)) { pressed |= PlayerKey.WPress; wHeld = true; }
        if (Input.GetKeyUp(KeyCode.W)) { pressed |= PlayerKey.WRelease; wHeld = false; }
        if (Input.GetKeyDown(KeyCode.S)) { pressed |= PlayerKey.SPress; sHeld = true; }
        if (Input.GetKeyUp(KeyCode.S)) { pressed |= PlayerKey.SRelease; sHeld = false; }
        if (Input.GetKeyDown(KeyCode.A)) { pressed |= PlayerKey.APress; aHeld = true; }
        if (Input.GetKeyUp(KeyCode.A)) { pressed |= PlayerKey.ARelease; aHeld = false; }
        if (Input.GetKeyDown(KeyCode.D)) { pressed |= PlayerKey.DPress; dHeld = true; }
        if (Input.GetKeyUp(KeyCode.D)) { pressed |= PlayerKey.DRelease; dHeld = false; }

        if (Input.GetKeyDown(Config.melee_key)) CastUnarmed();
        if (Input.GetKeyDown(Config.jump_key)) pressed |= PlayerKey.K;
        if (Input.GetKeyUp(Config.jump_key)) pressed |= PlayerKey.KRelease; // 长按抬起（飞行高度控制预留）
        if (Input.GetKeyDown(Config.slide_key)) pressed |= PlayerKey.LShift;
        if (Input.GetKeyUp(Config.slide_key)) pressed |= PlayerKey.LShiftRelease; // 长按抬起（飞行高度控制预留）
        if (Input.GetKeyDown(Config.minimap_radius_key)) Tool.NetworkManager.SendMinimapRadiusSwitch(); //雷达显示半径切换
        // 技能键：按键 → 技能 id 由客户端技能栏解析，上行技能包而不是这个包
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        if (skills != null)
        {
            for (int i = 0; i < Config.skill_slot_keys.Length; i++)
            {
                if (Input.GetKeyDown(Config.skill_slot_keys[i])) skills.CastSlot(i);
            }
        }

        if (pressed != PlayerKey.None)
            Tool.NetworkManager.SendInput(new CSPlayerInput() { pressed = pressed });
    }

    // 空手攻击：移动中随机左右拳，静止原地砸击（与服务器原口径一致，判断放客户端）
    private void CastUnarmed()
    {
        if (Tool.ClientLogicManager == null) return;
        bool topDown = Tool.CameraController != null && Tool.CameraController.Mode == CameraViewMode.TopDown;
        bool moving = topDown ? (wHeld || aHeld || sHeld || dHeld) : wHeld;
        Tool.ClientLogicManager.SkillVfx.CastUnarmed(moving);
    }

    // 进入屏蔽的那一帧把按住态全部抬起，告诉服务器按键已松开
    private void ReleaseHeldKeys()
    {
        PlayerKey pressed = PlayerKey.None;
        if (wHeld) { pressed |= PlayerKey.WRelease; wHeld = false; }
        if (sHeld) { pressed |= PlayerKey.SRelease; sHeld = false; }
        if (aHeld) { pressed |= PlayerKey.ARelease; aHeld = false; }
        if (dHeld) { pressed |= PlayerKey.DRelease; dHeld = false; }
        pressed |= PlayerKey.KRelease;
        pressed |= PlayerKey.LShiftRelease;
        Tool.NetworkManager.SendInput(new CSPlayerInput() { pressed = pressed });
    }
}
