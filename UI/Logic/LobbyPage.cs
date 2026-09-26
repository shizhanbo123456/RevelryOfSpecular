using System.Linq;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyPage : RosPage
{
    private int myCamp = -1;             // 本地当前选择（-1 未选 / 0 进攻 / 1 防守）
    private bool syncingFromServer;      // 服务器回显期间屏蔽本地回调，避免回环上报

    public override void Init()
    {
        attackToggle.SetCallback(OnAttackToggle);
        defenseToggle.SetCallback(OnDefenseToggle);
        attackAIField.onValueChanged.AddListener(_ => OnAICountChanged());
        defenseAIField.onValueChanged.AddListener(_ => OnAICountChanged());
        startButton.SetCallback(OnStartClicked);
        backButton.SetCallback(OnBackClicked);
    }

    public override void Enter(ShowParam param)
    {
        EventManager.AddEvent<SCRoomInfo>(ClientEvent.OnRoomInfoUpdate, OnRoomInfoUpdate);
    }

    public override void Exit()
    {
        EventManager.RemoveEvent<SCRoomInfo>(ClientEvent.OnRoomInfoUpdate, OnRoomInfoUpdate);
    }

    #region//Local
    private void OnAttackToggle(bool on)
    {
        if (syncingFromServer) return;
        if (on)
        {
            defenseToggle.SetSelectedWithoutNotify(false);
            myCamp = 0;
        }
        else if (myCamp == 0)
        {
            myCamp = -1;
        }
        SendRoomState();
    }

    private void OnDefenseToggle(bool on)
    {
        if (syncingFromServer) return;
        if (on)
        {
            attackToggle.SetSelectedWithoutNotify(false);
            myCamp = 1;
        }
        else if (myCamp == 1)
        {
            myCamp = -1;
        }
        SendRoomState();
    }

    /// <summary>AI 数量输入变化 → 上报房间状态。</summary>
    private void OnAICountChanged()
    {
        if (syncingFromServer) return;
        SendRoomState();
    }

    /// <summary>上报本客户端的大厅选择（选队 + AI 数量，服务器取最新值）。</summary>
    private void SendRoomState()
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendRoomUpdate(new CSRoomUpdate()
        {
            camp = myCamp,
            attackAICount = ParseCount(attackAIField),
            defenseAICount = ParseCount(defenseAIField),
        });
    }

    private static int ParseCount(InputField field)
    {
        return field == null ? 0 : Mathf.Max(0, int.TryParse(field.text, out int v) ? v : 0);
    }

    /// <summary>服务器广播的房间状态 → 刷新 UI。</summary>
    private void OnRoomInfoUpdate(SCRoomInfo info)
    {
        if (info == null) return;
        syncingFromServer = true;
        attackAIField.SetTextWithoutNotify(info.attackAICount.ToString());
        defenseAIField.SetTextWithoutNotify(info.defenseAICount.ToString());

        // 回显自己的队伍选择
        var me = info.members.Find(m => m != null && m.clientId == EnsInstance.LocalClientId);
        int myServerCamp = me?.camp ?? -1;
        attackToggle.SetSelectedWithoutNotify(myServerCamp == 0);
        defenseToggle.SetSelectedWithoutNotify(myServerCamp == 1);
        myCamp = myServerCamp;

        int attackHumans = info.members.Count(m => m != null && m.camp == 0);
        int defenseHumans = info.members.Count(m => m != null && m.camp == 1);
        bool canStart = attackHumans + info.attackAICount > 0 && defenseHumans + info.defenseAICount > 0;
        if (roomLabel != null)
            roomLabel.text = $"进攻方：人类 {attackHumans} + AI {info.attackAICount}\n" +
                             $"防守方：人类 {defenseHumans} + AI {info.defenseAICount}\n" +
                             (canStart ? "满足开局条件（双方人数均 > 0）" : "双方人数均需 > 0 才能开始");
        startButton.SetInteractable(canStart && !info.battleStarted);
        syncingFromServer = false;
    }

    private void OnStartClicked()
    {
        Tool.NetworkManager?.SendStartRequest();
    }

    /// <summary>断开并返回初始界面（确认面板组件尚未在 uGUI 重建，暂为直接断开）。</summary>
    private void OnBackClicked()
    {
        Tool.NetworkManager?.ExitWorld();
        Tool.UIManager?.TurnPage(PageType.Home);
    }
    #endregion
}
