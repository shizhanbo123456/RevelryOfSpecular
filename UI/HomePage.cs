using Ros.Info;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 初始界面（策划案 3.1）：分别选择进攻方/防守方角色、连接服务器；
/// 连接成功后进入组队大厅（选队伍与 AI 数量在组队大厅完成）。
/// 局外养成只保留角色解锁与角色升级。
/// </summary>
public class HomePage : PageBase
{
    private VisualElement characterList;
    private Label infoLabel;
    private Label levelUpLabel;
    private TextField ipField;
    private Label connectStatusLabel;
    private Button connectButton;
    private readonly Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.9f);
    private readonly Color normalColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);

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
        var title = new Label("墓园狂欢 · 非对称攻防")
        {
            style = { color = Color.white, fontSize = 34, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 }
        };
        var subtitle = new Label("分别选择进攻方与防守方角色，连接服务器后进入组队大厅")
        {
            style = { color = new Color(0.7f, 0.7f, 0.75f, 1f), fontSize = 16, marginBottom = 18 }
        };
        container.Add(title);
        container.Add(subtitle);

        // 角色列表（左）与信息（右）
        var contentRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };

        characterList = new ScrollView { style = { flexGrow = 1, flexBasis = 480, marginRight = 20 } };
        contentRow.Add(characterList);

        var infoPanel = new VisualElement
        {
            style = { width = 420, backgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f), paddingLeft = 16, paddingRight = 16, paddingTop = 16, paddingBottom = 16 }
        };
        infoLabel = new Label("未选择角色") { style = { color = Color.white, fontSize = 16, whiteSpace = WhiteSpace.Normal } };
        levelUpLabel = new Label("") { style = { color = new Color(0.4f, 0.9f, 0.5f, 1f), fontSize = 15, whiteSpace = WhiteSpace.Normal, marginTop = 10 } };
        infoPanel.Add(infoLabel);
        infoPanel.Add(levelUpLabel);
        contentRow.Add(infoPanel);

        container.Add(contentRow);

        // 底部：连接服务器（成功后进入组队大厅）
        var bottomRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 16, alignItems = Align.Center } };
        bottomRow.Add(new Label("服务器 IP（空=本机）") { style = { color = Color.white, marginRight = 10 } });
        ipField = new TextField { value = "", style = { width = 260, marginRight = 16 } };
        bottomRow.Add(ipField);
        connectButton = new Button(OnConnectClicked) { text = "连接服务器", style = { width = 160, height = 44, fontSize = 18 } };
        bottomRow.Add(connectButton);
        connectStatusLabel = new Label("")
        {
            style = { color = new Color(1f, 0.8f, 0.3f, 1f), fontSize = 15, marginLeft = 16, whiteSpace = WhiteSpace.Normal, flexGrow = 1 }
        };
        bottomRow.Add(connectStatusLabel);
        container.Add(bottomRow);

        root.Add(container);
    }

    /// <summary>连接服务器（成功后自动加入房间并进入组队大厅）。</summary>
    private async void OnConnectClicked()
    {
        if (Tool.NetworkManager == null)
        {
            connectStatusLabel.text = "缺少 NetworkManager，请检查场景配置";
            return;
        }
        connectButton.SetEnabled(false);
        connectStatusLabel.text = "正在连接服务器...";
        var result = await Tool.NetworkManager.TryConnect(ipField.value);
        if (result != NetworkManager.ConnectResult.Success)
        {
            connectButton.SetEnabled(true);
            connectStatusLabel.text = result == NetworkManager.ConnectResult.Failed ? "连接失败，请检查服务器地址" : "操作过于频繁，请稍候";
            return;
        }
        connectStatusLabel.text = "已连接，正在进入组队大厅...";
        Tool.NetworkManager.EnterWorld();
        connectButton.SetEnabled(true);
        Owner.ShowPage(UIManager.PageType.Lobby);
    }

    public override void OnEnable()
    {
        RefreshList();
    }

    /// <summary>刷新角色列表与信息面板（两个阵营分组常驻展示，各自独立选择）。</summary>
    public void RefreshList()
    {
        if (characterList == null) return;
        characterList.Clear();

        AddCharacterButtons("—— 进攻方角色 ——", isDefense: false, Tool.InfoManager != null ? Tool.InfoManager.AttackCharacterInfoList : null);
        AddCharacterButtons("—— 防守方角色 ——", isDefense: true, Tool.InfoManager != null ? Tool.InfoManager.DefenseCharacterInfoList : null);
        RefreshInfo();
    }

    private void AddCharacterButtons(string groupTitle, bool isDefense, System.Collections.Generic.List<EntityAttributeInfo> infoList)
    {
        var group = new Label(groupTitle) { style = { color = new Color(0.85f, 0.7f, 0.3f, 1f), fontSize = 16, marginTop = 8, marginBottom = 4 } };
        characterList.Add(group);

        for (int i = 0; i < infoList?.Count; i++)
        {
            var info = infoList[i];
            string name = !string.IsNullOrEmpty(info.Name) ? info.Name : (isDefense ? $"防守角色 {i}" : $"进攻角色 {i}");
            int saveIndex = isDefense ? Config.attack_character_count + i : i;
            int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;
            bool unlocked = Tool.SaveManager != null && Tool.SaveManager.IsCharacterUnlocked(saveIndex);
            int selectedIndex = isDefense ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;

            var btn = new Button
            {
                text = $"{name}  Lv{level}  {(unlocked ? "" : "[未解锁]")}",
                style = { marginBottom = 4, unityTextAlign = TextAnchor.MiddleLeft, height = 36 }
            };
            btn.style.backgroundColor = i == selectedIndex ? selectedColor : normalColor;
            int captured = i;
            btn.clicked += () =>
            {
                if (isDefense) ClientSelection.selectedDefenseIndex = captured;
                else ClientSelection.selectedAttackIndex = captured;
                RefreshList();
            };
            characterList.Add(btn);
        }
    }

    private void RefreshInfo()
    {
        if (infoLabel == null) return;
        var sb = new System.Text.StringBuilder();
        AppendCharacter(sb, "进攻方", ClientSelection.selectedAttackIndex, isDefense: false);
        sb.AppendLine();
        AppendCharacter(sb, "防守方", ClientSelection.selectedDefenseIndex, isDefense: true);
        infoLabel.text = sb.ToString();

        // 升级提示：以进攻方当前选中角色为例展示
        var info = GetAttributeInfo(ClientSelection.selectedAttackIndex, isDefense: false);
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(ClientSelection.selectedAttackIndex) : 1;
        if (levelUpLabel != null)
        {
            levelUpLabel.text = info != null && level < Config.max_entity_level
                ? $"进攻方下一等级（Lv{level + 1}）：{info.GetNextLevelGainDescription(level)}"
                : (level >= Config.max_entity_level ? "已达等级上限" : "角色未配置属性");
        }
    }

    private void AppendCharacter(System.Text.StringBuilder sb, string campName, int index, bool isDefense)
    {
        var info = GetAttributeInfo(index, isDefense);
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(isDefense ? Config.attack_character_count + index : index) : 1;
        sb.AppendLine($"{campName}选中：{(info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : $"角色 {index}")}  Lv{level}");
        if (info != null)
        {
            var attr = info.GetAttribute(level);
            sb.AppendLine($"生命 {attr.maxHealth}  力量 {attr.strength}  魔法 {attr.magic}");
            sb.AppendLine($"暴击 {attr.critRate}% / {attr.critDamage:F1}倍  击退抗性 {attr.knockbackResistance}  可见距离 {attr.viewDistance}m  技能槽位 {attr.weaponSlotCount}");
        }
    }

    private EntityAttributeInfo GetAttributeInfo(int index, bool isDefense)
    {
        if (Tool.InfoManager == null) return null;
        if (!isDefense)
        {
            return index < Tool.InfoManager.AttackCharacterInfoList.Count ? Tool.InfoManager.AttackCharacterInfoList[index] : null;
        }
        return index < Tool.InfoManager.DefenseCharacterInfoList.Count ? Tool.InfoManager.DefenseCharacterInfoList[index] : null;
    }
}
