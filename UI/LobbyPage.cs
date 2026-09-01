using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 匹配大厅（策划案 3.2）：PVP/PVE 双队列切换 / 切换角色与阵营 / 开始匹配。
/// 开始匹配流程：连接服务器（本机或远程 IP）→ 进入房间上报选角 → 服务器分配阵营 → 战斗开始。
/// </summary>
public class LobbyPage : PageBase
{
    private Toggle pvpToggle;
    private Toggle pveToggle;
    private TextField ipField;
    private Label statusLabel;
    private Button matchButton;

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
        container.Add(new Label("匹配大厅")
        {
            style = { color = Color.white, fontSize = 32, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 16 }
        });

        // 队列选择
        var queueRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 12 } };
        pvpToggle = new Toggle("PVP（真人防守方）") { value = ClientSelection.pvpQueue, style = { marginRight = 24 } };
        pveToggle = new Toggle("PVE（AI 防守方）") { value = !ClientSelection.pvpQueue };
        queueRow.Add(pvpToggle);
        queueRow.Add(pveToggle);
        container.Add(queueRow);

        pvpToggle.RegisterValueChangedCallback(e =>
        {
            if (e.newValue) pveToggle.value = false;
            ClientSelection.pvpQueue = e.newValue || !pveToggle.value;
        });
        pveToggle.RegisterValueChangedCallback(e =>
        {
            if (e.newValue) pvpToggle.value = false;
            ClientSelection.pvpQueue = !e.newValue;
        });

        // 当前选择
        var selectionLabel = new Label { style = { color = new Color(0.8f, 0.8f, 0.85f, 1f), fontSize = 16, marginBottom = 8 } };
        selectionLabel.text = $"当前角色：{DescribeSelection()}";
        container.Add(selectionLabel);

        // 服务器地址
        var ipRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 12, alignItems = Align.Center } };
        ipRow.Add(new Label("服务器 IP（空=本机）") { style = { color = Color.white, marginRight = 10 } });
        ipField = new TextField { value = "", style = { flexGrow = 1, maxWidth = 300 } };
        ipRow.Add(ipField);
        container.Add(ipRow);

        statusLabel = new Label("")
        {
            style = { color = new Color(1f, 0.8f, 0.3f, 1f), fontSize = 15, minHeight = 22, marginBottom = 8, whiteSpace = WhiteSpace.Normal }
        };
        container.Add(statusLabel);

        // 底部按钮
        var bottomRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        var backButton = new Button("返回初始界面") { style = { width = 180, height = 44 } };
        backButton.clicked += () => Owner.ShowPage(UIManager.PageType.Home);
        matchButton = new Button("开始匹配") { style = { width = 200, height = 44, fontSize = 18 } };
        matchButton.clicked += OnMatchClicked;
        bottomRow.Add(backButton);
        bottomRow.Add(matchButton);
        container.Add(bottomRow);

        root.Add(container);
    }

    private string DescribeSelection()
    {
        var type = ClientSelection.SelectionToEntityType(ClientSelection.selectedCharacterIndex);
        string camp = ClientSelection.campIntention == 0 ? "进攻方" : (ClientSelection.campIntention == 1 ? "防守方" : "任意");
        return $"{camp} · {type}";
    }

    private async void OnMatchClicked()
    {
        if (Tool.NetworkManager == null)
        {
            SetStatus("缺少 NetworkManager，请检查场景配置");
            return;
        }
        matchButton.SetEnabled(false);
        SetStatus("正在连接服务器...");
        Owner.ShowLoading(true);

        var result = await Tool.NetworkManager.TryConnect(ipField.value);
        if (result != NetworkManager.ConnectResult.Success)
        {
            matchButton.SetEnabled(true);
            Owner.ShowLoading(false);
            SetStatus(result == NetworkManager.ConnectResult.Failed ? "连接失败，请检查服务器地址" : "操作过于频繁，请稍候");
            return;
        }

        SetStatus("已连接，进入对局...");
        Tool.NetworkManager.EnterWorld();
        // 战斗开始后 UIManager 自动切到战斗页；失败时 OnRestartGame 回初始界面
        matchButton.SetEnabled(true);
        Owner.ShowLoading(false);
    }

    private void SetStatus(string text)
    {
        if (statusLabel != null) statusLabel.text = text;
    }
}
