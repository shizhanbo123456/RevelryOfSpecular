using System.Collections.Generic;
using Ros.Transport;
using Ros.UI.Main;

public class LobbyPage : PageBase
{
    private readonly UI_LobbyPanel panel;
    private readonly List<UI_LobbyMemberList.Member> roomMembers = new(); // 回显缓冲，避免每次广播重新分配

    public LobbyPage(UI_LobbyPanel panel) : base(panel)
    {
        this.panel = panel;
    }

    //结算在大厅之上播放期间隐藏/恢复面板
    public void SetPanelVisible(bool visible) => Root.visible = visible;

    public override void Construct()
    {
        // 主视图自带三阵营块与准备/退出按钮：名字解析、状态上报、断开连接都留在页面这边
        panel.m_mainView.Build(NetworkManager.GetLobbyMemberName, SendRoomState, DoExitWorld);
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        EventManager.AddEvent<SCLobbyInfo>(ClientEvent.OnLobbyInfoUpdate, OnLobbyInfoUpdate);
        // 有缓存立即回显（服务器只在事件时广播，错过就没了）；无缓存清空展示，不留 FGUI 默认内容
        ApplyLobbyInfo(NetworkManager.LatestLobbyInfo);
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
    private void OnLobbyInfoUpdate(SCLobbyInfo info) => ApplyLobbyInfo(info);

    // 协议包 → 主视图需要的成员表（负 clientId 为 AI，勾选与名额判定各自处理）
    private void ApplyLobbyInfo(SCLobbyInfo info)
    {
        roomMembers.Clear();
        if (info?.members != null)
        {
            for (int i = 0; i < info.members.Count; i++)
            {
                var member = info.members[i];
                if (member == null) continue;
                roomMembers.Add(new UI_LobbyMemberList.Member(member.clientId, member.camp, member.characterIndex, member.ready));
            }
        }
        panel.m_mainView.Apply(roomMembers,
            info != null ? info.attackAICount : 0,
            info != null ? info.defenseAICount : 0,
            NetworkManager.LocalToken);
    }

    private void SendRoomState(int camp, bool ready, int attackAI, int defenseAI)
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendLobbyUpdate(new CSLobbyUpdate()
        {
            camp = camp,
            ready = ready,
            attackAICount = attackAI,
            defenseAICount = defenseAI,
        });
    }

    private static void DoExitWorld()
    {
        if (Tool.NetworkManager != null) Tool.NetworkManager.ExitWorld();
    }
    #endregion
}
