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
    public const int TabSystem = 2;

    // GM 开关「敌方对话」：开启后阵营频道额外显示敌方阵营的消息
    public static bool ShowEnemyDialog;

    private const int MaxEntries = 50;      // 聊天只保留最近 50 条
    private const float SendInterval = 1f;  // 最快 1s 一条
    private const int MaxLength = 50;       // 消息长度 1~50

    private class ChatEntry
    {
        public int channel;        // 0 全频道 / 1 阵营频道，页签按它过滤
        public bool isSelf;
        public int senderClientId;
        public int camp;           // 发送方阵营：0 进攻 / 1 防守
        public int characterIndex; // 发送方角色下标（-1 无头像）
        public string name;
        public string content;
    }

    // 系统消息条目：field 直接对应 EventItem 的版式控制器与 EventIcon 档位
    private class SystemEntry
    {
        public int type;     // 0 纯文字 / 1 图标+文字 / 2 文字+图标+文字
        public string text;  // type 0/1 的文本
        public int icon;     // type 1/2 的 EventIcon 档位
        public string left;  // type 2 左侧文本（击杀者）
        public string right; // type 2 右侧文本（受害者）
    }

    private readonly UI_ChatPanel panel;
    private readonly List<ChatEntry> entries = new();
    private readonly List<ChatEntry> visibleChat = new(); // 当前聊天页签下可见的条目（渲染器按索引读取）
    private readonly List<SystemEntry> systemEntries = new(); // 系统消息，整局常驻，战斗结束后清空
    private int viewTab = ChannelAll;
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
        panel.m_content.itemProvider = ItemUrlAt;
        panel.m_content.itemRenderer = RenderItem;
        panel.m_btn_send.onClick.Add(TrySend);
        panel.m_input.onSubmit.Add(TrySend);
        panel.m_btn_allChannel.onClick.Add(() => SelectTab(ChannelAll));
        panel.m_btn_campChannel.onClick.Add(() => SelectTab(ChannelCamp));
        panel.m_btn_systemInfo.onClick.Add(() => SelectTab(TabSystem));
        panel.m_btn_hide.onClick.Add(() => SetFolded(true));
        panel.m_btn_show.onClick.Add(() => SetFolded(false));
        panel.m_fold.selectedIndex = 0; // 默认展开

        EventManager.AddEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.AddEvent<int>(ClientEvent.OnBattleEnd, OnBattleEnd);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<SCChat>(ClientEvent.OnChatMessage, OnChatMessage);
        EventManager.AddEvent(ClientEvent.OnExitWorld, OnLeaveRoom);
        EventManager.AddEvent(ClientEvent.OnRestartGame, OnLeaveRoom);
    }

    public void Shutdown()
    {
        //EventManager 是静态的，订阅必须与解绑成对
        EventManager.RemoveEvent(ClientEvent.OnConnect, OnConnect);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleEnd, OnBattleEnd);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<SCChat>(ClientEvent.OnChatMessage, OnChatMessage);
        EventManager.RemoveEvent(ClientEvent.OnExitWorld, OnLeaveRoom);
        EventManager.RemoveEvent(ClientEvent.OnRestartGame, OnLeaveRoom);
    }

    //与页面同规则的整体缩放（设计高度 1080）；左上角贴屏幕左侧中点
    public void OnResize(float uiScale)
    {
        this.uiScale = uiScale;
        ApplyTransform();
    }

    #region//Local
    // 折叠后面板尺寸会变，位置按新尺寸重新贴屏幕左侧中点
    private void SetFolded(bool folded)
    {
        panel.m_fold.selectedIndex = folded ? 1 : 0;
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        panel.scale = new Vector2(uiScale, uiScale);
        panel.SetXY(0f, GRoot.inst.height * 0.5f);
    }

    private void OnConnect()
    {
        ClearMessages(); // 显示前先清空，避免残留 FGUI 设计期的测试条目
        SelectTab(ChannelAll);
        panel.m_show_channel_menu.selectedIndex = 0; // 组队界面不显示频道页签
        panel.visible = true;
    }

    private void OnBattleStart(int camp)
    {
        SelectTab(ChannelAll);
        panel.m_show_channel_menu.selectedIndex = 1; // 战斗时显示频道页签
    }

    private void OnBattleEnd(int gameState)
    {
        if (systemEntries.Count == 0) return;
        systemEntries.Clear(); // 系统消息只属于本局
        if (viewTab == TabSystem) RefreshList();
    }

    private void OnLeaveRoom()
    {
        ClearMessages();
        SelectTab(ChannelAll);
        SetFolded(false); // 断开后下次进房重新以展开态出现
        panel.visible = false;
    }

    private void ClearMessages()
    {
        entries.Clear();
        systemEntries.Clear();
        RefreshList();
        panel.m_input.text = string.Empty;
    }

    // 页签即频道（0 全频道 / 1 阵营频道），系统消息页签只管看，不参与发送频道
    private void SelectTab(int tab)
    {
        viewTab = tab;
        if (tab != TabSystem) sendChannel = tab;
        panel.m_btn_allChannel.selected = tab == ChannelAll;
        panel.m_btn_campChannel.selected = tab == ChannelCamp;
        panel.m_btn_systemInfo.selected = tab == TabSystem;
        RefreshList();
    }

    private string ItemUrlAt(int index)
    {
        return viewTab == TabSystem ? UI_EventItem.URL : UI_ChatItem.URL;
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
            channel = sendChannel,
            isSelf = true,
            senderClientId = NetworkManager.LocalToken,
            camp = GetMyCamp(),
            characterIndex = self != null ? self.characterIndex : -1,
            name = GetMyName(),
            content = content,
        });
        RefreshList();
        panel.m_input.text = string.Empty;

        if (Tool.NetworkManager != null)
            Tool.NetworkManager.SendChat(new CSChat() { channel = sendChannel, content = content });
    }

    private void OnChatMessage(SCChat chat)
    {
        if (chat == null) return;
        if (chat.senderClientId == NetworkManager.LocalToken) return; // 自己的已本地回显
        // 阵营频道客户端过滤；GM「敌方对话」开启时放行敌方阵营消息
        if (chat.channel == ChannelCamp && chat.camp != GetMyCamp() && !ShowEnemyDialog) return;
        entries.Add(new ChatEntry()
        {
            channel = chat.channel,
            isSelf = false,
            senderClientId = chat.senderClientId,
            camp = chat.camp,
            characterIndex = chat.characterIndex,
            name = chat.name,
            content = chat.content,
        });
        RefreshList();
    }

    // 事件类型 → (EventIcon 档位, 条目版式)：FGUI 表现映射，协议里不传表现字段
    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        byte icon;
        byte layout;
        switch (e.type)
        {
            case SCBattleEvent.Type.PlayerKill: icon = 6; layout = 2; break;           // 文字+击败图标+文字
            case SCBattleEvent.Type.DeathUnattributed: icon = 0; layout = 1; break;    // 无源死亡
            case SCBattleEvent.Type.PlayerRespawn: icon = 2; layout = 1; break;        // 玩家复活
            case SCBattleEvent.Type.NeutralStatueCaptured: icon = 1; layout = 1; break;   // 中立雕像被击败
            case SCBattleEvent.Type.NeutralStatueRespawn: icon = 3; layout = 1; break;    // 中立雕像刷新
            case SCBattleEvent.Type.Nightfall: icon = 4; layout = 1; break;            // 天黑
            case SCBattleEvent.Type.Daybreak: icon = 5; layout = 1; break;             // 天亮
            case SCBattleEvent.Type.StatueDestroyed: icon = 7; layout = 1; break;      // 其它（雕像）
            case SCBattleEvent.Type.CrystalStatueDestroyed: icon = 7; layout = 1; break;       // 其它（水晶雕像）
            case SCBattleEvent.Type.Notice: icon = 0; layout = 0; break;               // 纯文字
            case SCBattleEvent.Type.SkillBroadcast: return;   // 技能播报只进重要事件 banner，不进聊天列表
            default:
                Debug.LogError($"[ChatPlugin] 未处理的战斗事件类型：{e.type}");
                return;
        }

        var entry = new SystemEntry
        {
            type = layout,
            icon = icon,
            text = ResolveEventText(e.textId),
        };
        if (layout == 2) // 文字+图标+文字
        {
            entry.left = entry.text;
            entry.right = ResolveEventText(e.textId2);
        }
        systemEntries.Add(entry);
        if (viewTab == TabSystem) RefreshList();
    }

    // 事件文本统一按数字 id 解析：>=PlayerNameBase 映射为玩家名，否则查 notice.csv
    private static string ResolveEventText(int id)
    {
        if (id >= SCBattleEvent.PlayerNameBase) return GetUnitName(id - SCBattleEvent.PlayerNameBase);
        return ConfigManager.NoticeConfig.Get(id).text;
    }

    private static string GetUnitName(int clientId)
    {
        var loading = ClientLoadingManager.Current;
        return loading != null && loading.HasSession ? loading.GetName(clientId) : $"玩家{clientId}";
    }

    private void RefreshList()
    {
        RebuildVisibleChat();
        int count = viewTab == TabSystem ? systemEntries.Count : visibleChat.Count;
        // 页签切换会换 item 资源，先回收现有条目，再按 itemProvider 重建
        panel.m_content.RemoveChildrenToPool();
        panel.m_content.numItems = count;
        if (count > 0) panel.m_content.ScrollToView(count - 1);
    }

    private void RebuildVisibleChat()
    {
        visibleChat.Clear();
        if (viewTab == TabSystem) return;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].channel == viewTab) visibleChat.Add(entries[i]);
        }
    }

    private void RenderItem(int index, GObject obj)
    {
        if (viewTab == TabSystem) RenderSystemItem(index, obj);
        else RenderChatItem(index, obj);
    }

    private void RenderChatItem(int index, GObject obj)
    {
        if (obj is not UI_ChatItem item || index >= visibleChat.Count) return;
        var e = visibleChat[index];
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

    private void RenderSystemItem(int index, GObject obj)
    {
        if (obj is not UI_EventItem item || index >= systemEntries.Count) return;
        SystemEntry entry = systemEntries[index];
        item.m_type.selectedIndex = entry.type;
        switch (entry.type)
        {
            case 0:
                item.m_type0_label.text = entry.text;
                break;
            case 1:
                item.m_type1_loader.m_type.selectedIndex = entry.icon;
                item.m_type1_label.text = entry.text;
                break;
            default:
                item.m_type2_loader.m_type.selectedIndex = entry.icon;
                item.m_type2_label1.text = entry.left;
                item.m_type2_label2.text = entry.right;
                break;
        }
    }

    private SCLobbyInfo.LobbyMemberInfo GetLocalMember()
    {
        var info = NetworkManager.LatestLobbyInfo;
        if (info == null || info.members == null) return null;
        return info.members.Find(m => m != null && m.clientId == NetworkManager.LocalToken);
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
        if (NetworkManager.BattleRunning && loading != null && loading.HasSession) return loading.GetName(NetworkManager.LocalToken);
        if (NetworkManager.TryGetLobbyPlayerName(NetworkManager.LocalToken, out var lobbyName)) return lobbyName;
        return !string.IsNullOrEmpty(ClientSelection.playerName) ? ClientSelection.playerName : $"玩家{NetworkManager.LocalToken}";
    }
    #endregion
}
