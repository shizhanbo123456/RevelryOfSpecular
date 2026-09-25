using Ros.Transport;
using UnityEngine;

/// <summary>
/// 输入管理器（客户端，双手键盘无鼠标）：只上报原始按键，不解释按键含义。
/// 全部按键走同一可靠触发通道：WASD 按下/抬起双边沿，动作键仅按下边沿；无事件不发送。
/// 瞄准点由服务器权威计算，客户端不再上报。
/// </summary>
public class InputManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.InputManager = this;
    }

    private void Update()
    {
        SendInput();
    }

    /// <summary>收集本帧全部按键边沿（WASD 含抬起），有事件才发送。</summary>
    private void SendInput()
    {
        if (Tool.NetworkManager == null) return;
        PlayerKey pressed = PlayerKey.None;

        if (Input.GetKeyDown(KeyCode.W)) pressed |= PlayerKey.WPress;
        if (Input.GetKeyUp(KeyCode.W)) pressed |= PlayerKey.WRelease;
        if (Input.GetKeyDown(KeyCode.S)) pressed |= PlayerKey.SPress;
        if (Input.GetKeyUp(KeyCode.S)) pressed |= PlayerKey.SRelease;
        if (Input.GetKeyDown(KeyCode.A)) pressed |= PlayerKey.APress;
        if (Input.GetKeyUp(KeyCode.A)) pressed |= PlayerKey.ARelease;
        if (Input.GetKeyDown(KeyCode.D)) pressed |= PlayerKey.DPress;
        if (Input.GetKeyUp(KeyCode.D)) pressed |= PlayerKey.DRelease;

        if (Input.GetKeyDown(Config.melee_key)) pressed |= PlayerKey.J;
        if (Input.GetKeyDown(Config.jump_key)) pressed |= PlayerKey.K;
        if (Input.GetKeyDown(Config.slide_key)) pressed |= PlayerKey.LShift;
        for (int i = 0; i < Config.skill_slot_keys.Length; i++)
        {
            if (Input.GetKeyDown(Config.skill_slot_keys[i])) pressed |= Config.skill_slot_player_keys[i];
        }

        if (pressed != PlayerKey.None) Tool.NetworkManager.SendInput(new CSPlayerInput() { pressed = pressed });
    }
}
