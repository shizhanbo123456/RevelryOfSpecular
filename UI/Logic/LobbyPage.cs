using System.Collections.Generic;
using System.Linq;
using FairyGUI;
using Ros.UI.Main;
using Ros.Transport;
using UnityEngine;

public class LobbyPage : PageBase
{
    private readonly UI_LobbyPanel panel;
    private int myCamp = -1;          // 本地当前选择（-1 未同步 / 0 进攻 / 1 防守 / 2 观战）
    private bool myReady;             // 本地准备状态（服务器回显为准）
    private int attackAICount;        // 本地显示值，服务器回显为准
    private int defenseAICount;
    private bool syncingFromServer;   // 服务器回显期间屏蔽本地回调，避免回环上报

    public LobbyPage(UI_LobbyPanel panel) : base(panel)
    {
        this.panel = panel;
    }

    //结算在大厅之上播放期间隐藏/恢复面板
    public void SetPanelVisible(bool visible) => Root.visible = visible;

    public override void Construct()
    {
        var view = panel.m_mainView;
        view.m_btn_joinAttacker.onClick.Add(() => JoinCamp(0));
        view.m_btn_joinDefenser.onClick.Add(() => JoinCamp(1));
        view.m_btn_joinSpectate.onClick.Add(() => JoinCamp(2));
        view.m_btn_prepare.onClick.Add(ToggleReady);
        view.m_attackerAI_minus.onClick.Add(() => ChangeAICount(false, -1));
        view.m_attackerAI_add.onClick.Add(() => ChangeAICount(false, +1));
        view.m_defenserAI_minus.onClick.Add(() => ChangeAICount(true, -1));
        view.m_defenserAI_add.onClick.Add(() => ChangeAICount(true, +1));
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
        if (BlockedByReady()) return;
        myCamp = camp;
        SendRoomState();
    }

    private void ChangeAICount(bool defense, int delta)
    {
        if (syncingFromServer) return;
        if (BlockedByReady()) return;
        if (defense) defenseAICount = Mathf.Max(0, defenseAICount + delta);
        else attackAICount = Mathf.Max(0, attackAICount + delta);
        RefreshAICountLabels();
        SendRoomState();
    }

    // 已准备（含开战倒计时中）不允许改阵营与 AI 数量：服务器只在准备状态变化时重算开局条件，此时改了也不会生效
    private bool BlockedByReady()
    {
        if (!myReady) return false;
        if (Tool.UIManager != null) Tool.UIManager.ShowFlyText("请先取消准备");
        return true;
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
            ready = myReady,
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
        myReady = me != null && me.ready;
        RefreshPrepareButton();

        RenderMemberList(panel.m_mainView.m_attackerPlayers, info.members, 0);
        RenderMemberList(panel.m_mainView.m_defenserPlayers, info.members, 1);
        RenderMemberList(panel.m_mainView.m_spectatePlayers, info.members, 2);
        syncingFromServer = false;
    }

    private void ClearRoomDisplay()
    {
        syncingFromServer = true;
        attackAICount = 0;
        defenseAICount = 0;
        myCamp = -1;
        myReady = false;
        RefreshPrepareButton();
        RefreshAICountLabels();
        var empty = new List<SCRoomInfo.RoomMemberInfo>();
        RenderMemberList(panel.m_mainView.m_attackerPlayers, empty, 0);
        RenderMemberList(panel.m_mainView.m_defenserPlayers, empty, 1);
        RenderMemberList(panel.m_mainView.m_spectatePlayers, empty, 2);
        syncingFromServer = false;
    }

    private void RenderMemberList(GList list, List<SCRoomInfo.RoomMemberInfo> members, int camp)
    {
        var memberList = members.Where(m => m != null && m.camp == camp && m.clientId >= 0).ToList(); // 负数 = AI，不进真人列表
        var icons = Tool.AssetsManager == null || camp == 2 ? null // 观战方无角色头像
            : camp == 0 ? Tool.AssetsManager.AttackCharacterIcons : Tool.AssetsManager.DefenseCharacterIcons;
        list.itemRenderer = (i, obj) =>
        {
            var head = (UI_RoleHead)obj;
            var member = memberList[i];
            head.m_roleName.text = string.IsNullOrEmpty(member.name) ? $"玩家{member.clientId}" : member.name;
            head.m_level.selectedIndex = 0; // 成员条目不展示等级
            head.m_lock.selectedIndex = 0;
            head.m_confirm.selectedIndex = member.ready ? 1 : 0; // 准备勾选
            bool hasIcon = member.characterIndex >= 0 && icons != null
                && member.characterIndex < icons.Count && icons[member.characterIndex] != null;
            head.m_headIcon.texture = hasIcon ? new NTexture(icons[member.characterIndex]) : null;
        };
        list.numItems = memberList.Count;
    }

    private void ToggleReady()
    {
        if (syncingFromServer) return;
        myReady = !myReady;
        RefreshPrepareButton();
        SendRoomState(); // 服务器广播回显后，所有成员头像同步勾选状态
    }

    private void RefreshPrepareButton()
    {
        var view = panel.m_mainView;
        if (view.m_btn_prepare != null) view.m_btn_prepare.title = myReady ? "取消准备" : "准备";
    }

    // 断开不可逆，先弹二次确认
    private void OnExitClicked()
    {
        if (Tool.UIManager != null) Tool.UIManager.ShowConfirm("确定断开与服务器的连接？", DoExitWorld);
        else DoExitWorld();
    }

    private static void DoExitWorld()
    {
        if (Tool.NetworkManager != null) Tool.NetworkManager.ExitWorld();
    }
    #endregion
}
