using System.Collections.Generic;
using FairyGUI;
using Ros.Transport;
using Ros.UI.Main;
using UnityEngine;

//全局聊天面板：联网后常驻显示，大厅与战斗共用；离开房间清空并隐藏
public class ChatPlugin
{
    public const int ChannelAll = 0;
    public const int ChannelCamp = 1;

    // GM 开关「敌方对话」：开启后阵营频道额外显示敌方阵营的消息
    public static bool ShowEnemyDialog;

    private const int MaxEntries = 50;      // 只保留最近 50 条
    private const float SendInterval = 1f;  // 最快 1s 一条
    private const int MaxLength = 50;       // 消息长度 1~50
    private const float BottomMargin = 50f; // 距屏幕底部（屏幕像素）

    private class ChatEntry
    {
        public bool isSelf;
        public short senderClientId;
        public int camp;           // 发送方阵营：0 进攻 / 1 防守
        public int characterIndex; // 发送方角色下标（-1 无头像）
        public string name;
        public string content;
    }

    private readonly UI_ChatPanel panel;
    private readonly List<ChatEntry> entries = new();
    private int sendChannel = ChannelAll;
    private float lastSendTime = -999f;
    private float uiScale = 1f;

    public ChatPlugin(UI_ChatPanel panel)
    {
        this.panel = panel;
        GRoot.inst.AddChild(panel);
        panel.visible = false;
    }

    public void Construct()
    {
        panel.m_content.itemRenderer = RenderItem;
        panel.m_btn_send.onClick.Add(TrySend);
        panel.m_input.onSubmit.Add(TrySend);
        panel.m_btn_allChannel.onClick.Add(() => SelectChannel(ChannelAll));
        panel.m_btn_campChannel.onClick.Add(() => SelectChannel(ChannelCamp));
        panel.m_btn_hide.onClick.Add(() => SetFolded(true));
        panel.m_btn_show.onClick.Add(() => SetFolded(false));
        panel.m_fold.selectedIndex = 0; // 默认展开

        EventManager.AddEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.AddEvent(ClientEvent.OnExitWorld, OnLeaveRoom);
        EventManager.AddEvent(ClientEvent.OnRestartGame, OnLeaveRoom);
    }

    public void Shutdown()
    {
        //EventManager 是静态的，订阅必须与解绑成对
        EventManager.RemoveEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.RemoveEvent(ClientEvent.OnExitWorld, OnLeaveRoom);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, OnLeaveRoom);
    }

    //与页面同规则的整体缩放（设计高度 1080）；贴屏幕左下角（紧贴左侧、距底部 BottomMargin）
    public void OnResize(float uiScale)
    {
        this.uiScale = uiScale;
        ApplyTransform();
    }

    #region//Local
    // 折叠后面板尺寸会变，位置要按新尺寸重新贴左下角
    private void SetFolded(bool folded)
    {
        panel.m_fold.selectedIndex = folded ? 1 : 0;
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        panel.scale = new Vector2(uiScale, uiScale);
        panel.SetXY(0f, GRoot.inst.height - panel.height * uiScale - BottomMargin);
    }

    private void OnConnect()
    {
        SelectChannel(ChannelAll);
        panel.m_show_channel_menu.selectedIndex = 0; // 组队界面不显示频道页签
        ClearMessages(); // 显示前先清空，避免残留 FGUI 设计期的测试条目
        panel.visible = true;
    }

    private void OnBattleStart(int camp)
    {
        SelectChannel(ChannelAll);
        panel.m_show_channel_menu.selectedIndex = 1; // 战斗时显示频道页签
    }

    private void OnLeaveRoom()
    {
        ClearMessages();
        SelectChannel(ChannelAll);
        SetFolded(false); // 断开后下次进房重新以展开态出现
        panel.visible = false;
    }

    private void ClearMessages()
    {
        entries.Clear();
        panel.m_content.numItems = 0;
        panel.m_input.text = string.Empty;
    }

    private void SelectChannel(int channel)
    {
        sendChannel = channel;
        panel.m_btn_allChannel.selected = channel == ChannelAll;
        panel.m_btn_campChannel.selected = channel == ChannelCamp;
    }

    private void TrySend()
    {
        string content = panel.m_input.text;
        if (string.IsNullOrWhiteSpace(content)) return; // 空消息不发送
        content = content.Trim();
        if (content.Length > MaxLength)
        {
            if (Tool.UIManager != null) Tool.UIManager.ShowFlyText($"消息过长，最多{MaxLength}字");
            return;
        }
        if (Time.time - lastSendTime < SendInterval) return; // 1s 冷却
        lastSendTime = Time.time;

        var self = GetLocalMember();
        entries.Add(new ChatEntry()
        {
            isSelf = true,
            senderClientId = EnsInstance.LocalClientId,
            camp = GetMyCamp(),
            characterIndex = self != null ? self.characterIndex : -1,
            name = GetMyName(),
            content = content,
        });
        TrimAndRefresh();
        panel.m_input.text = string.Empty;

        if (Tool.NetworkManager != null)
            Tool.NetworkManager.SendChat(new CSChat() { channel = sendChannel, content = content });
    }

    private void OnChatMessage(SCChat chat)
    {
        if (chat == null) return;
        if (chat.senderClientId == EnsInstance.LocalClientId) return; // 自己的已本地回显
        // 阵营频道客户端过滤；GM「敌方对话」开启时放行敌方阵营消息
        if (chat.channel == ChannelCamp && chat.camp != GetMyCamp() && !ShowEnemyDialog) return;
        entries.Add(new ChatEntry()
        {
            isSelf = false,
            senderClientId = chat.senderClientId,
            camp = chat.camp,
            characterIndex = chat.characterIndex,
            name = chat.name,
            content = chat.content,
        });
        TrimAndRefresh();
    }

    private void RenderItem(int index, GObject obj)
    {
        if (obj is not UI_ChatItem item || index >= entries.Count) return;
        var e = entries[index];
        item.m_side.selectedIndex = e.isSelf ? 1 : 0;
        item.m_camp.selectedIndex = e.camp; // 0 进攻 / 1 防守 / 2 观战 / 3 中立（协议值直接当档位）
        item.m_player_name.text = string.IsNullOrEmpty(e.name) ? $"玩家{e.senderClientId}" : e.name;
        item.m_content.text = e.content;
        //自己的消息文本靠右，他人的靠左
        item.m_content.align = e.isSelf ? AlignType.Right : AlignType.Left;
        item.m_player_name.align = e.isSelf ? AlignType.Right : AlignType.Left;

        var icons = Tool.AssetsManager == null ? null
            : e.camp == 1 ? Tool.AssetsManager.DefenseCharacterIcons : Tool.AssetsManager.AttackCharacterIcons;
        bool hasIcon = e.characterIndex >= 0 && icons != null
            && e.characterIndex < icons.Count && icons[e.characterIndex] != null;
        item.m_headIcon.texture = hasIcon ? new NTexture(icons[e.characterIndex]) : null;
    }

    private void TrimAndRefresh()
    {
        while (entries.Count > MaxEntries) entries.RemoveAt(0);
        panel.m_content.numItems = entries.Count;
        if (entries.Count > 0) panel.m_content.ScrollToView(entries.Count - 1);
    }

    private SCLobbyInfo.LobbyMemberInfo GetLocalMember()
    {
        var info = NetworkManager.LatestLobbyInfo;
        if (info == null || info.members == null) return null;
        return info.members.Find(m => m != null && m.clientId == EnsInstance.LocalClientId);
    }

    private int GetMyCamp()
    {
        var loading = ClientLoadingManager.Current;
        if (NetworkManager.BattleRunning && loading != null && loading.HasSession)
        {
            if (loading.Camp == EntityCamp.Attack) return 0;
            if (loading.Camp == EntityCamp.Defense) return 1;
            return 2; // 观战方玩家（无战斗实体）
        }
        var member = GetLocalMember();
        return member != null ? member.camp : 2;
    }

    private string GetMyName()
    {
        var loading = ClientLoadingManager.Current;
        if (NetworkManager.BattleRunning && loading != null && loading.HasSession) return loading.GetName(EnsInstance.LocalClientId);
        if (NetworkManager.TryGetLobbyPlayerName(EnsInstance.LocalClientId, out var lobbyName)) return lobbyName;
        return !string.IsNullOrEmpty(ClientSelection.playerName) ? ClientSelection.playerName : $"玩家{EnsInstance.LocalClientId}";
    }
    #endregion
}
