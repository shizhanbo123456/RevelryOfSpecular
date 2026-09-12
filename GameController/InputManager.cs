using Ros.Transport;
using UnityEngine;

/// <summary>
/// 输入管理器（客户端，双手键盘无鼠标）：只上报原始按键，不解释按键含义。
/// WASD 走不可靠移动通道（按下/抬起边沿），攻击/跳跃/滑铲/技能槽走可靠动作通道（仅按下）。
/// 瞄准点由服务器权威计算，客户端不再上报。
/// </summary>
public class InputManager : MonoBehaviour
{
    private PlayerKey lastMove; // 上一帧按住的移动键（求按下/抬起边沿）

    private void Awake()
    {
        Tool.InputManager = this;
    }

    private void Update()
    {
        SendMoveInput();
        SendActionInput();
    }

    /// <summary>WASD 按下/抬起边沿（不可靠）。</summary>
    private void SendMoveInput()
    {
        if (Tool.NetworkManager == null) return;
        PlayerKey now = PlayerKey.None;
        if (Input.GetKey(KeyCode.W)) now |= PlayerKey.W;
        if (Input.GetKey(KeyCode.S)) now |= PlayerKey.S;
        if (Input.GetKey(KeyCode.A)) now |= PlayerKey.A;
        if (Input.GetKey(KeyCode.D)) now |= PlayerKey.D;

        Tool.NetworkManager.SendMoveInput(new CSMoveInput()
        {
            pressed = now & ~lastMove,
            released = lastMove & ~now,
        });
        lastMove = now;
    }

    /// <summary>动作键按下边沿（可靠；这些键没有抬起事件）。</summary>
    private void SendActionInput()
    {
        if (Tool.NetworkManager == null) return;
        PlayerKey pressed = PlayerKey.None;
        if (Input.GetKeyDown(Config.melee_key)) pressed |= PlayerKey.J;
        if (Input.GetKeyDown(Config.jump_key)) pressed |= PlayerKey.K;
        if (Input.GetKeyDown(Config.slide_key)) pressed |= PlayerKey.LShift;
        for (int i = 0; i < Config.skill_slot_keys.Length; i++)
        {
            if (Input.GetKeyDown(Config.skill_slot_keys[i])) pressed |= Config.skill_slot_player_keys[i];
        }
        if (pressed != PlayerKey.None) Tool.NetworkManager.SendActionInput(new CSActionInput() { pressed = pressed });
    }
}
