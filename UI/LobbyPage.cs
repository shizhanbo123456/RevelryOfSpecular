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
        var container = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                paddingLeft = 40, paddingRight = 40, paddingTop = 24, paddingBottom = 24,
                backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.97f),
            }
        };
        container.Add(new Label("组队大厅")
        {
            style = { color = Color.white, fontSize = 32, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 }
        });
        container.Add(new Label("已连接服务器 · 选择队伍并自由编辑双方 AI 数量（双方人数均 > 0 可开始）")
        {
            style = { color = new Color(0.7f, 0.7f, 0.75f, 1f), fontSize = 15, marginBottom = 16 }
        });

        // 队伍选择
        var campRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 12 } };
        attackToggle = new Toggle("加入进攻方") { style = { marginRight = 24 } };
        defenseToggle = new Toggle("加入防守方") { style = { marginRight = 24 } };
        campRow.Add(attackToggle);
        campRow.Add(defenseToggle);
        container.Add(campRow);

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

        // AI 数量（任意玩家可编辑双方数量）
        var aiRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 16, alignItems = Align.Center } };
        aiRow.Add(new Label("进攻方 AI 数量") { style = { color = Color.white, marginRight = 8 } });
        attackAIField = new IntegerField { value = 0, isReadOnly = false, style = { width = 80, marginRight = 24 } };
        aiRow.Add(attackAIField);
        aiRow.Add(new Label("防守方 AI 数量") { style = { color = Color.white, marginRight = 8 } });
        defenseAIField = new IntegerField { value = 0, isReadOnly = false, style = { width = 80 } };
        aiRow.Add(defenseAIField);
        container.Add(aiRow);

        attackAIField.RegisterValueChangedCallback(e => OnAICountChanged());
        defenseAIField.RegisterValueChangedCallback(e => OnAICountChanged());

        // 房间状态（服务器权威广播）
        roomLabel = new Label("等待房间状态...")
        {
            style = { color = new Color(0.8f, 0.8f, 0.85f, 1f), fontSize = 16, marginBottom = 12, whiteSpace = WhiteSpace.Normal }
        };
        container.Add(roomLabel);

        statusLabel = new Label("")
        {
            style = { color = new Color(1f, 0.8f, 0.3f, 1f), fontSize = 15, minHeight = 22, marginBottom = 8, whiteSpace = WhiteSpace.Normal }
        };
        container.Add(statusLabel);

        // 底部按钮
        var bottomRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        var backButton = new Button(OnBackClicked) { text = "断开并返回", style = { width = 180, height = 44 } };
        startButton = new Button(OnStartClicked) { text = "准备", style = { width = 200, height = 44, fontSize = 18 } };
        bottomRow.Add(backButton);
        bottomRow.Add(startButton);
        container.Add(bottomRow);

        root.Add(container);
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
        startButton.SetEnabled(canStart && !info.battleStarted);
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
