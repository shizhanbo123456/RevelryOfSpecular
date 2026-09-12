using System.Linq;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 组队大厅（策划案 3.2：组队阶段，选角与组队同时进行）。
/// 只做两件事：①选择自己加入的队伍（进攻方/防守方）②编辑双方 AI 玩家数量（任意玩家可编辑，数量不限）。
/// 双方人数（人类 + AI）均 &gt; 0 时可开始对局；无匹配、无队列。
/// 房间状态由服务器 SCRoomInfo 权威广播。
/// </summary>
public class LobbyPage : PageBase
{
    private Toggle attackToggle;
    private Toggle defenseToggle;
    private IntegerField attackAIField;
    private IntegerField defenseAIField;
    private Label roomLabel;
    private Label statusLabel;
    private Button startButton;

    /// <summary>本地当前选择（-1 未选 / 0 进攻 / 1 防守）。</summary>
    private int myCamp = -1;
    private int attackAICount;
    private int defenseAICount;
    private bool syncingFromServer;

    protected override void Build(VisualElement root)
    {
        var page = UITheme.Page();

        page.Add(UITheme.Title("组队大厅"));
        var subtitle = UITheme.Subtitle("已连接服务器 · 选择队伍并自由编辑双方 AI 数量（双方人数均 > 0 可开始）");
        subtitle.style.marginBottom = 18;
        page.Add(subtitle);

        // 队伍选择
        var campCard = UITheme.Card();
        campCard.style.marginBottom = 12;
        campCard.Add(UITheme.Section("我的队伍"));
        var campRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        campRow.style.marginTop = 6;
        attackToggle = UITheme.StyleToggle(new Toggle("加入进攻方"));
        attackToggle.style.marginRight = 28;
        defenseToggle = UITheme.StyleToggle(new Toggle("加入防守方"));
        campRow.Add(attackToggle);
        campRow.Add(defenseToggle);
        campCard.Add(campRow);
        page.Add(campCard);

        // AI 数量（任意玩家可编辑双方数量）
        var aiCard = UITheme.Card();
        aiCard.style.marginBottom = 12;
        aiCard.Add(UITheme.Section("AI 玩家数量"));
        var aiRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        aiRow.style.marginTop = 6;
        var attackTag = UITheme.Text("进攻方", UITheme.Attack, UITheme.FontBody, true);
        attackTag.style.marginRight = 8;
        aiRow.Add(attackTag);
        attackAIField = UITheme.StyleField(new IntegerField { value = 0, isReadOnly = false }, 80f);
        attackAIField.style.marginRight = 28;
        aiRow.Add(attackAIField);
        var defenseTag = UITheme.Text("防守方", UITheme.Defense, UITheme.FontBody, true);
        defenseTag.style.marginRight = 8;
        aiRow.Add(defenseTag);
        defenseAIField = UITheme.StyleField(new IntegerField { value = 0, isReadOnly = false }, 80f);
        aiRow.Add(defenseAIField);
        aiCard.Add(aiRow);
        page.Add(aiCard);

        attackToggle.RegisterValueChangedCallback(e =>
        {
            if (syncingFromServer) return;
            if (e.newValue)
            {
                defenseToggle.SetValueWithoutNotify(false);
                myCamp = 0;
            }
            else if (myCamp == 0)
            {
                myCamp = -1;
            }
            SendRoomState();
        });
        defenseToggle.RegisterValueChangedCallback(e =>
        {
            if (syncingFromServer) return;
            if (e.newValue)
            {
                attackToggle.SetValueWithoutNotify(false);
                myCamp = 1;
            }
            else if (myCamp == 1)
            {
                myCamp = -1;
            }
            SendRoomState();
        });

        attackAIField.RegisterValueChangedCallback(e => OnAICountChanged());
        defenseAIField.RegisterValueChangedCallback(e => OnAICountChanged());

        // 房间状态（服务器权威广播）
        var roomCard = UITheme.Card();
        roomCard.style.marginBottom = 12;
        roomCard.Add(UITheme.Section("房间状态"));
        roomLabel = UITheme.Text("等待房间状态...", UITheme.TextDim, UITheme.FontBody);
        roomLabel.style.marginTop = 6;
        roomCard.Add(roomLabel);
        page.Add(roomCard);

        statusLabel = UITheme.Text("", UITheme.Warn, UITheme.FontSmall);
        statusLabel.style.marginBottom = 8;
        page.Add(statusLabel);

        // 底部按钮
        var bottomRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        page.Add(bottomRow);
        bottomRow.Add(UITheme.StyleButton(new Button(OnBackClicked) { text = "断开并返回" }, false, 180f, 44f));
        startButton = UITheme.StyleButton(new Button(OnStartClicked) { text = "准备" }, true, 200f, 44f);
        bottomRow.Add(startButton);

        root.Add(page);
    }

    public override void OnEnable()
    {
        EventManager.AddEvent<SCRoomInfo>(ClientEvent.OnRoomInfoUpdate, OnRoomInfoUpdate);
    }

    public override void OnDisable()
    {
        EventManager.RemoveEvent<SCRoomInfo>(ClientEvent.OnRoomInfoUpdate, OnRoomInfoUpdate);
    }

    #region//Local
    /// <summary>AI 数量输入变化 → 上报房间状态。</summary>
    private void OnAICountChanged()
    {
        if (syncingFromServer) return;
        attackAICount = Mathf.Max(0, attackAIField.value);
        defenseAICount = Mathf.Max(0, defenseAIField.value);
        SendRoomState();
    }

    /// <summary>上报本客户端的大厅选择（选队 + AI 数量，服务器取最新值）。</summary>
    private void SendRoomState()
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendRoomUpdate(new CSRoomUpdate()
        {
            camp = myCamp,
            attackAICount = Mathf.Max(0, attackAIField.value),
            defenseAICount = Mathf.Max(0, defenseAIField.value),
        });
    }

    /// <summary>服务器广播的房间状态 → 刷新 UI。</summary>
    private void OnRoomInfoUpdate(SCRoomInfo info)
    {
        if (info == null) return;
        syncingFromServer = true;
        attackAICount = info.attackAICount;
        defenseAICount = info.defenseAICount;
        attackAIField.SetValueWithoutNotify(info.attackAICount);
        defenseAIField.SetValueWithoutNotify(info.defenseAICount);

        // 回显自己的队伍选择
        var me = info.members.Find(m => m != null && m.clientId == EnsInstance.LocalClientId);
        int myServerCamp = me?.camp ?? -1;
        attackToggle.SetValueWithoutNotify(myServerCamp == 0);
        defenseToggle.SetValueWithoutNotify(myServerCamp == 1);
        myCamp = myServerCamp;

        int attackHumans = info.members.Count(m => m != null && m.camp == 0);
        int defenseHumans = info.members.Count(m => m != null && m.camp == 1);
        bool canStart = attackHumans + info.attackAICount > 0 && defenseHumans + info.defenseAICount > 0;
        roomLabel.text = $"进攻方：人类 {attackHumans} + AI {info.attackAICount}\n" +
                         $"防守方：人类 {defenseHumans} + AI {info.defenseAICount}\n" +
                         (canStart ? "满足开局条件（双方人数均 > 0）" : "双方人数均需 > 0 才能开始");
        UITheme.SetButtonEnabled(startButton, canStart && !info.battleStarted);
        syncingFromServer = false;
    }

    private void OnStartClicked()
    {
        Tool.NetworkManager?.SendStartRequest();
        SetStatus("已发送开始请求...");
    }

    private void OnBackClicked()
    {
        Tool.NetworkManager?.ExitWorld();
        Owner.ShowPage(UIManager.PageType.Home);
    }

    private void SetStatus(string text)
    {
        if (statusLabel != null) statusLabel.text = text;
    }
    #endregion
}
