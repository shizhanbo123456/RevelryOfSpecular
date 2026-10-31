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
        // 三个阵营块的图标档位（PlayerCampIcon.m_player_camp：0 进攻 / 1 防守 / 2 观战 / 3 中立）
        view.m_camp_attack.m_player_camp.m_player_camp.selectedIndex = (int)PlayerCampType.Attack;
        view.m_camp_defense.m_player_camp.m_player_camp.selectedIndex = (int)PlayerCampType.Defense;
        view.m_camp_spectate.m_player_camp.m_player_camp.selectedIndex = (int)PlayerCampType.Spectator;
        view.m_camp_attack.m_btn_join.onClick.Add(() => JoinCamp(0));
        view.m_camp_defense.m_btn_join.onClick.Add(() => JoinCamp(1));
        view.m_camp_spectate.m_btn_join.onClick.Add(() => JoinCamp(2));
        view.m_camp_attack.m_add_ai.onClick.Add(() => ChangeAICount(false, +1));
        view.m_camp_attack.m_remove_ai.onClick.Add(() => ChangeAICount(false, -1));
        view.m_camp_defense.m_add_ai.onClick.Add(() => ChangeAICount(true, +1));
        view.m_camp_defense.m_remove_ai.onClick.Add(() => ChangeAICount(true, -1));
        view.m_btn_prepare.onClick.Add(ToggleReady);
        view.m_btn_exit.onClick.Add(OnExitClicked);
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        EventManager.AddEvent<SCLobbyInfo>(ClientEvent.OnLobbyInfoUpdate, OnLobbyInfoUpdate);
        // 有缓存立即渲染（服务器只在事件时广播，错过就没了）；无缓存清空展示，不留 FGUI 默认内容
        var latest = NetworkManager.LatestLobbyInfo;
        if (latest != null) OnLobbyInfoUpdate(latest);
        else ClearRoomDisplay();
        // 大厅与首页一样展示所选攻/守角色的场景预览（战斗页 Enter 时会隐藏，故返回大厅需重建）
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
            Tool.ClientLogicManager.HomePreview.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
    }

    public override void Exit()
    {
        base.Exit();
        EventManager.RemoveEvent<SCLobbyInfo>(ClientEvent.OnLobbyInfoUpdate, OnLobbyInfoUpdate);
    }

    #region//Local
    private void JoinCamp(int camp)
    {
        if (syncingFromServer) return;
        if (BlockedByReady()) return;
        if (camp != myCamp && !HasHumanSeat(camp))
        {
            if (Tool.UIManager != null)
                Tool.UIManager.ShowFlyText(camp == (int)PlayerCampType.Defense ? "防守方人数已满" : "进攻方人数已满");
            return;
        }
        myCamp = camp;
        SendRoomState();
    }

    // 真人名额（不含 AI）：观战不限；按服务器上次广播的成员列表预判，最终以服务器为准
    private static bool HasHumanSeat(int camp)
    {
        if (camp == (int)PlayerCampType.Spectator) return true;
        var info = NetworkManager.LatestLobbyInfo;
        if (info == null || info.members == null) return true;
        int limit = camp == (int)PlayerCampType.Attack
            ? ConfigManager.GlobalVariableConfig.player_limit_attacker
            : ConfigManager.GlobalVariableConfig.player_limit_defenser;
        int count = 0;
        foreach (var member in info.members)
        {
            if (member != null && member.clientId >= 0 && member.camp == camp) count++;
        }
        return count < limit;
    }

    private void ChangeAICount(bool defense, int delta)
    {
        if (syncingFromServer) return;
        if (BlockedByReady()) return;
        int limit = defense
            ? ConfigManager.GlobalVariableConfig.player_limit_defenser_AI
            : ConfigManager.GlobalVariableConfig.player_limit_attacker_AI;
        int current = defense ? defenseAICount : attackAICount;
        if (delta > 0 && current >= limit)
        {
            if (Tool.UIManager != null) Tool.UIManager.ShowFlyText(defense ? "防守方 AI 已达上限" : "进攻方 AI 已达上限");
            return;
        }
        int next = Mathf.Clamp(current + delta, 0, limit);
        if (defense) defenseAICount = next;
        else attackAICount = next;
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
        if (view.m_camp_attack.m_label_AI_count != null) view.m_camp_attack.m_label_AI_count.text = $"x{attackAICount}";
        if (view.m_camp_defense.m_label_AI_count != null) view.m_camp_defense.m_label_AI_count.text = $"x{defenseAICount}";
    }

    private void SendRoomState()
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendLobbyUpdate(new CSLobbyUpdate()
        {
            camp = myCamp,
            ready = myReady,
            attackAICount = attackAICount,
            defenseAICount = defenseAICount,
        });
    }

    private void OnLobbyInfoUpdate(SCLobbyInfo info)
    {
        if (info == null) return;
        syncingFromServer = true;
        attackAICount = info.attackAICount;
        defenseAICount = info.defenseAICount;
        RefreshAICountLabels();

        // 回显自己的队伍选择
        var me = info.members.Find(m => m != null && m.clientId == NetworkManager.LocalToken);
        int myServerCamp = -1;
        if (me != null) myServerCamp = me.camp;
        myCamp = myServerCamp;
        myReady = me != null && me.ready;
        RefreshPrepareButton();

        RenderMemberList(panel.m_mainView.m_camp_attack.m_attackerPlayers, info.members, 0);
        RenderMemberList(panel.m_mainView.m_camp_defense.m_attackerPlayers, info.members, 1);
        RenderSpectateCount(info.members);
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
        var empty = new List<SCLobbyInfo.LobbyMemberInfo>();
        RenderMemberList(panel.m_mainView.m_camp_attack.m_attackerPlayers, empty, 0);
        RenderMemberList(panel.m_mainView.m_camp_defense.m_attackerPlayers, empty, 1);
        RenderSpectateCount(empty);
        syncingFromServer = false;
    }

    // 观战方只显示人数，不列具体人员
    private void RenderSpectateCount(List<SCLobbyInfo.LobbyMemberInfo> members)
    {
        var view = panel.m_mainView.m_camp_spectate;
        int count = members.Count(m => m != null && m.camp == (int)PlayerCampType.Spectator);
        if (view.m_spectate_current_member_count != null) view.m_spectate_current_member_count.text = count.ToString();
        view.m_attackerPlayers.numItems = 0;
        view.m_attackerPlayers.visible = false;
    }

    private void RenderMemberList(GList list, List<SCLobbyInfo.LobbyMemberInfo> members, int camp)
    {
        var memberList = members.Where(m => m != null && m.camp == camp && m.clientId >= 0).ToList(); // 负数 = AI，不进真人列表
        var icons = Tool.AssetsManager == null ? null
            : camp == 0 ? Tool.AssetsManager.AttackCharacterIcons : Tool.AssetsManager.DefenseCharacterIcons;
        list.itemRenderer = (i, obj) =>
        {
            var head = (UI_RoleHead)obj;
            var member = memberList[i];
            head.m_roleName.text = NetworkManager.GetLobbyMemberName(member.clientId);
            head.m_level.selectedIndex = 0; // 成员条目不展示等级
            head.m_lock.selectedIndex = 0;
            head.m_confirm.selectedIndex = member.ready ? 1 : 0; // 准备勾选
            head.m_quality.selectedIndex = MemberQuality(camp, member.characterIndex);
            bool hasIcon = member.characterIndex >= 0 && icons != null
                && member.characterIndex < icons.Count && icons[member.characterIndex] != null;
            head.m_headIcon.texture = hasIcon ? new NTexture(icons[member.characterIndex]) : null;
        };
        list.numItems = memberList.Count;
    }

    // 成员品质档：未选角色（characterIndex < 0）按 0 档，不读表
    private static int MemberQuality(int camp, int characterIndex)
    {
        if (characterIndex < 0) return 0;
        var type = camp == 0 ? EntityType.Attack(characterIndex) : EntityType.Defense(characterIndex);
        return ConfigManager.EntityConfig.Get(type).quality;
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
