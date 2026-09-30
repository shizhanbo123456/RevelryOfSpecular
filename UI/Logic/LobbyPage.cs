using System.Collections.Generic;
using System.Linq;
using FairyGUI;
using Ros.UI.Main;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 组队大厅逻辑（FGUI）：选队（joinAttacker/joinDefenser）、AI 数量编辑（± 按钮）、
/// 成员列表（RoleHead：玩家名+所选角色头像）、开始对局、断开返回。
/// 房间状态由服务器 SCRoomInfo 权威广播，本页只做表现与上报。
/// </summary>
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
        // 大厅与首页一样展示所选攻/守角色的场景预览（战斗页 Enter 时会隐藏，故返回大厅需重建）
        Tool.ClientLogicManager?.HomePreview?.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
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

    /// <summary>AI 数量 ±：本地立即显示，随后整体上报（服务器回显为准，最小 0）。</summary>
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

    /// <summary>服务器广播的房间状态 → 刷新成员列表/AI 数量/选队回显/开始按钮。</summary>
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
        myCamp = myServerCamp;

        RenderMemberList(panel.m_mainView.m_attackerPlayers, info.members, 0);
        RenderMemberList(panel.m_mainView.m_defenserPlayers, info.members, 1);

        int attackHumans = info.members.Count(m => m != null && m.camp == 0);
        int defenseHumans = info.members.Count(m => m != null && m.camp == 1);
        canStartBattle = attackHumans + info.attackAICount > 0 && defenseHumans + info.defenseAICount > 0;
        battleStarted = info.battleStarted;
        syncingFromServer = false;
    }

    /// <summary>渲染指定阵营的成员列表（条目 = RoleHead：玩家名+所选角色头像；AI 只计数量不进列表）。</summary>
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
        if (battleStarted) { Tool.UIManager?.ShowFlyText("对局已开始"); return; }
        if (!canStartBattle) { Tool.UIManager?.ShowFlyText("进攻方与防守方都需至少一名玩家或AI"); return; }
        Tool.NetworkManager?.SendStartRequest();
    }

    /// <summary>断开并返回初始界面（页面切换由 NetworkManager 的 OnRestartGame 事件统一处理）。</summary>
    private void OnExitClicked()
    {
        Tool.NetworkManager?.ExitWorld();
    }
    #endregion
}
