using System.Collections.Generic;
using System.Linq;
using FairyGUI;
using Ros.UI.Main;
using Ros.Transport;
using UnityEngine;

public class LobbyPage : PageBase
{
    private readonly UI_LobbyPanel panel;
    private int myCamp = -1;          // 本地当前选择（-1 未选 / 0 进攻 / 1 防守）
    private int attackAICount;        // 本地显示值，服务器回显为准
    private int defenseAICount;
    private bool syncingFromServer;   // 服务器回显期间屏蔽本地回调，避免回环上报
    private bool canStartBattle;      // 最近一次服务器回显：是否满足开始条件（双方均有玩家或 AI）
    private bool battleStarted;       // 最近一次服务器回显：对局是否已开始

    public LobbyPage(UI_LobbyPanel panel) : base(panel)
    {
        this.panel = panel;
    }

    public override void Construct()
    {
        var view = panel.m_mainView;
        view.m_btn_joinAttacker.onClick.Add(() => JoinCamp(0));
        view.m_btn_joinDefenser.onClick.Add(() => JoinCamp(1));
        view.m_attackerAI_minus.onClick.Add(() => ChangeAICount(false, -1));
        view.m_attackerAI_add.onClick.Add(() => ChangeAICount(false, +1));
        view.m_defenserAI_minus.onClick.Add(() => ChangeAICount(true, -1));
        view.m_defenserAI_add.onClick.Add(() => ChangeAICount(true, +1));
        view.m_btn_battleStart.onClick.Add(OnStartClicked);
        panel.m_btn_exit.onClick.Add(OnExitClicked);
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        EventManager.AddEvent<SCRoomInfo>(ClientEvent.OnRoomInfoUpdate, OnRoomInfoUpdate);
        // 有缓存立即渲染（服务器只在事件时广播，错过就没了）；无缓存清空展示，不留 FGUI 默认内容
        var latest = NetworkManager.LatestRoomInfo;
        if (latest != null) OnRoomInfoUpdate(latest);
        else ClearRoomDisplay();
        // 大厅与首页一样展示所选攻/守角色的场景预览（战斗页 Enter 时会隐藏，故返回大厅需重建）
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
            Tool.ClientLogicManager.HomePreview.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
    }

    public override void Exit()
    {
        base.Exit();
        EventManager.RemoveEvent<SCRoomInfo>(ClientEvent.OnRoomInfoUpdate, OnRoomInfoUpdate);
    }

    #region//Local
    private void JoinCamp(int camp)
    {
        if (syncingFromServer) return;
        myCamp = camp;
        SendRoomState();
    }

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
        var view = panel.m_mainView;
        if (view.m_label_attackerAI_count != null) view.m_label_attackerAI_count.text = $"x{attackAICount}";
        if (view.m_label_defenserAI_count != null) view.m_label_defenserAI_count.text = $"x{defenseAICount}";
    }

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

    private void OnRoomInfoUpdate(SCRoomInfo info)
    {
        if (info == null) return;
        syncingFromServer = true;
        attackAICount = info.attackAICount;
        defenseAICount = info.defenseAICount;
        RefreshAICountLabels();

        // 回显自己的队伍选择
        var me = info.members.Find(m => m != null && m.clientId == EnsInstance.LocalClientId);
        int myServerCamp = -1;
        if (me != null) myServerCamp = me.camp;
        myCamp = myServerCamp;

        RenderMemberList(panel.m_mainView.m_attackerPlayers, info.members, 0);
        RenderMemberList(panel.m_mainView.m_defenserPlayers, info.members, 1);

        int attackHumans = info.members.Count(m => m != null && m.camp == 0);
        int defenseHumans = info.members.Count(m => m != null && m.camp == 1);
        canStartBattle = attackHumans + info.attackAICount > 0 && defenseHumans + info.defenseAICount > 0;
        battleStarted = info.battleStarted;
        syncingFromServer = false;
    }

    private void ClearRoomDisplay()
    {
        syncingFromServer = true;
        attackAICount = 0;
        defenseAICount = 0;
        myCamp = -1;
        canStartBattle = false;
        battleStarted = false;
        RefreshAICountLabels();
        var empty = new List<SCRoomInfo.RoomMemberInfo>();
        RenderMemberList(panel.m_mainView.m_attackerPlayers, empty, 0);
        RenderMemberList(panel.m_mainView.m_defenserPlayers, empty, 1);
        syncingFromServer = false;
    }

    private void RenderMemberList(GList list, List<SCRoomInfo.RoomMemberInfo> members, int camp)
    {
        var memberList = members.Where(m => m != null && m.camp == camp).ToList();
        var icons = Tool.AssetsManager == null ? null
            : camp == 0 ? Tool.AssetsManager.AttackCharacterIcons : Tool.AssetsManager.DefenseCharacterIcons;
        list.itemRenderer = (i, obj) =>
        {
            var head = (UI_RoleHead)obj;
            var member = memberList[i];
            head.m_roleName.text = string.IsNullOrEmpty(member.name) ? $"玩家{member.clientId}" : member.name;
            head.m_level.selectedIndex = 0; // 成员条目不展示等级
            head.m_lock.selectedIndex = 0;
            bool hasIcon = member.characterIndex >= 0 && icons != null
                && member.characterIndex < icons.Count && icons[member.characterIndex] != null;
            head.m_headIcon.texture = hasIcon ? new NTexture(icons[member.characterIndex]) : null;
        };
        list.numItems = memberList.Count;
    }

    private void OnStartClicked()
    {
        // 不在代码里用 enabled 禁用按钮（disabled 会让 onClick 完全不触发）；改为在回调内判断，不满足条件则阻断并飘字提示
        if (battleStarted) { if (Tool.UIManager != null) Tool.UIManager.ShowFlyText("对局已开始"); return; }
        if (!canStartBattle) { if (Tool.UIManager != null) Tool.UIManager.ShowFlyText("进攻方与防守方都需至少一名玩家或AI"); return; }
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendStartRequest();
    }

    private void OnExitClicked()
    {
        if (Tool.NetworkManager != null) Tool.NetworkManager.ExitWorld();
    }
    #endregion
}
