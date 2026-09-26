using System.Linq;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyPage : RosPage
{
    private int myCamp = -1;             // 本地当前选择（-1 未选 / 0 进攻 / 1 防守）
    private int attackAICount;           // 本地显示值，服务器回显为准
    private int defenseAICount;
    private bool syncingFromServer;      // 服务器回显期间屏蔽本地回调，避免回环上报

    public override void Init()
    {
        attackToggle.SetCallback(OnAttackToggle);
        defenseToggle.SetCallback(OnDefenseToggle);
        attackMinusButton.SetCallback(() => ChangeAICount(false, -1));
        attackPlusButton.SetCallback(() => ChangeAICount(false, +1));
        defenseMinusButton.SetCallback(() => ChangeAICount(true, -1));
        defensePlusButton.SetCallback(() => ChangeAICount(true, +1));
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

    /// <summary>AI 数量 +/-：本地立即显示，随后整体上报（服务器回显为准，最小 0）。</summary>
    private void ChangeAICount(bool defense, int delta)
    {
        if (syncingFromServer) return;
        if (defense) defenseAICount = Mathf.Max(0, defenseAICount + delta);
        else attackAICount = Mathf.Max(0, attackAICount + delta);
        RefreshAICountLabels();
        SendRoomState();
    }

    private void RefreshAICountLabels()
    {
        if (attackAICountLabel != null) attackAICountLabel.text = $"x{attackAICount}";
        if (defenseAICountLabel != null) defenseAICountLabel.text = $"x{defenseAICount}";
    }

    /// <summary>上报本客户端的大厅选择（选队 + AI 数量，服务器取最新值）。</summary>
    private void SendRoomState()
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendRoomUpdate(new CSRoomUpdate()
        {
            camp = myCamp,
            attackAICount = attackAICount,
            defenseAICount = defenseAICount,
        });
    }

    /// <summary>服务器广播的房间状态 → 刷新成员列表/AI 数量/自己的选队回显。</summary>
    private void OnRoomInfoUpdate(SCRoomInfo info)
    {
        if (info == null) return;
        syncingFromServer = true;
        attackAICount = info.attackAICount;
        defenseAICount = info.defenseAICount;
        RefreshAICountLabels();

        // 回显自己的队伍选择
        var me = info.members.Find(m => m != null && m.clientId == EnsInstance.LocalClientId);
        int myServerCamp = me?.camp ?? -1;
        attackToggle.SetSelectedWithoutNotify(myServerCamp == 0);
        defenseToggle.SetSelectedWithoutNotify(myServerCamp == 1);
        myCamp = myServerCamp;

        RenderMemberList(attackMemberListWrapper, info.members, 0);
        RenderMemberList(defenseMemberListWrapper, info.members, 1);

        bool canStart = attackHumans(info) + info.attackAICount > 0 && defenseHumans(info) + info.defenseAICount > 0;
        startButton.SetInteractable(canStart && !info.battleStarted);
        syncingFromServer = false;
    }

    private static int attackHumans(SCRoomInfo info) => info.members.Count(m => m != null && m.camp == 0);
    private static int defenseHumans(SCRoomInfo info) => info.members.Count(m => m != null && m.camp == 1);

    /// <summary>渲染指定阵营的成员列表（玩家名 + 所选角色头像；AI 只计数量不进列表）。</summary>
    private void RenderMemberList(RosListWrapper<LobbyMemberItem> wrapper, List<SCRoomInfo.RoomMemberInfo> members, int camp)
    {
        if (wrapper == null) return;
        var list = members.Where(m => m != null && m.camp == camp).ToList();
        var icons = Tool.AssetsManager == null ? null
            : camp == 0 ? Tool.AssetsManager.AttackCharacterIcons : Tool.AssetsManager.DefenseCharacterIcons;
        wrapper.itemRenderer = (item, i) =>
        {
            var member = list[i];
            if (item.NameText != null) item.NameText.text = $"玩家{member.clientId}";
            //头像 = 成员当前所选角色；未选/未导入时隐藏图标位
            bool hasIcon = member.characterIndex >= 0 && icons != null
                && member.characterIndex < icons.Count && icons[member.characterIndex] != null;
            if (item.Icon != null)
            {
                item.Icon.sprite = hasIcon ? icons[member.characterIndex] : null;
                item.Icon.gameObject.SetActive(hasIcon);
            }
        };
        wrapper.SetItemCount(list.Count);
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
