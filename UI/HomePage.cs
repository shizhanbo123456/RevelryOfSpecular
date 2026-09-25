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
    private VisualElement tabBar;
    private bool defenseTab;
    private Label infoLabel;
    private Label playerLevelLabel;
    private Label levelUpLabel;
    private TextField ipField;
    private Label connectStatusLabel;
    private Button connectButton;

    protected override void Build(VisualElement root)
    {
        var page = UITheme.Page();

        page.Add(UITheme.Title("墓园狂欢 · 非对称攻防"));
        var subtitle = UITheme.Subtitle("分别选择进攻方与防守方角色，连接服务器后进入组队大厅");
        subtitle.style.marginBottom = 18;
        page.Add(subtitle);

        // 角色列表（左，两页签切换阵营）与选中信息（右）
        var contentRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
        page.Add(contentRow);

        var listCard = UITheme.Card();
        listCard.style.flexGrow = 1;
        listCard.style.marginRight = 16;
        tabBar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 10 } };
        listCard.Add(tabBar);
        characterList = new ScrollView { style = { flexGrow = 1 } };
        listCard.Add(characterList);
        contentRow.Add(listCard);

        var infoCard = UITheme.Card(400f);
        infoCard.Add(UITheme.Section("角色信息"));
        playerLevelLabel = UITheme.Text("", UITheme.Gold, UITheme.FontBody);
        playerLevelLabel.style.marginBottom = 8;
        infoLabel = UITheme.Text("未选择角色", UITheme.TextMain, UITheme.FontBody);
        infoLabel.style.marginBottom = 8;
        levelUpLabel = UITheme.Text("", UITheme.Green, UITheme.FontSmall);
        infoCard.Add(playerLevelLabel);
        infoCard.Add(infoLabel);
        infoCard.Add(levelUpLabel);
        contentRow.Add(infoCard);

        // 底部：连接服务器（成功后进入组队大厅）
        var bottomRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        bottomRow.style.marginTop = 16;
        page.Add(bottomRow);

        var ipTip = UITheme.Text("服务器 IP（空=本机）", UITheme.TextDim);
        ipTip.style.marginRight = 10;
        bottomRow.Add(ipTip);

        ipField = UITheme.StyleField(new TextField { value = "" }, 260f);
        ipField.style.marginRight = 16;
        bottomRow.Add(ipField);

        connectButton = UITheme.StyleButton(new Button(OnConnectClicked) { text = "连接服务器" }, true, 170f, 40f);
        bottomRow.Add(connectButton);

        connectStatusLabel = UITheme.Text("", UITheme.Warn, UITheme.FontSmall);
        connectStatusLabel.style.marginLeft = 16;
        connectStatusLabel.style.flexGrow = 1;
        bottomRow.Add(connectStatusLabel);

        root.Add(page);
    }

    /// <summary>连接服务器（成功后自动加入房间并进入组队大厅）。</summary>
    private async void OnConnectClicked()
    {
        if (Tool.NetworkManager == null)
        {
            connectStatusLabel.text = "缺少 NetworkManager，请检查场景配置";
            return;
        }
        UITheme.SetButtonEnabled(connectButton, false);
        connectStatusLabel.text = "正在连接服务器...";
        Owner.ShowLoading(true, "正在连接服务器..."); // 连接自旋最长 5+5 秒，期间遮盖
        var result = await Tool.NetworkManager.TryConnect(ipField.value);
        Owner.ShowLoading(false); // 成功与失败都要收起
        if (result != NetworkManager.ConnectResult.Success)
        {
            UITheme.SetButtonEnabled(connectButton, true);
            connectStatusLabel.text = result == NetworkManager.ConnectResult.Failed ? "连接失败，请检查服务器地址" : "操作过于频繁，请稍候";
            return;
        }
        connectStatusLabel.text = "已连接，正在进入组队大厅...";
        Tool.NetworkManager.EnterWorld();
        UITheme.SetButtonEnabled(connectButton, true);
        Owner.ShowPage(UIManager.PageType.Lobby);
    }

    public override void OnEnable()
    {
        RefreshList();
    }

    /// <summary>刷新页签、角色列表与信息面板（页签决定列表显示哪个阵营，两阵营各自独立选择）。</summary>
    public void RefreshList()
    {
        if (characterList == null) return;
        characterList.Clear();
        BuildTabs();
        var infoManager = Tool.InfoManager;
        if (defenseTab)
            AddCharacterButtons(true, infoManager != null ? infoManager.DefenseCharacterInfoList : null);
        else
            AddCharacterButtons(false, infoManager != null ? infoManager.AttackCharacterInfoList : null);
        RefreshInfo();
    }

    /// <summary>构建进攻/防守两个页签（当前页签用主按钮色高亮）。</summary>
    private void BuildTabs()
    {
        tabBar.Clear();
        tabBar.Add(MakeTab("选择进攻方", false));
        var defense = MakeTab("选择防守方", true);
        defense.style.marginLeft = 8;
        tabBar.Add(defense);
    }

    private Button MakeTab(string title, bool isDefense)
    {
        var btn = UITheme.StyleButton(new Button(() =>
        {
            if (defenseTab == isDefense) return;
            defenseTab = isDefense;
            RefreshList();
        }) { text = title }, defenseTab == isDefense, 0f, 34f);
        btn.style.flexGrow = 1;
        return btn;
    }

    private void AddCharacterButtons(bool isDefense, System.Collections.Generic.List<PlayerCharacterInfo> infoList)
    {
        for (int i = 0; i < infoList?.Count; i++)
        {
            var info = infoList[i];
            string name = !string.IsNullOrEmpty(info.Name) ? info.Name : (isDefense ? $"防守角色 {i}" : $"进攻角色 {i}");
            int saveIndex = isDefense ? Config.attack_character_count + i : i;
            int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;
            bool unlocked = Tool.SaveManager == null || Tool.SaveManager.IsCharacterUnlocked(saveIndex);
            int selectedIndex = isDefense ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;

            var btn = UITheme.StyleListItem(new Button(), i == selectedIndex);
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            if (unlocked)
            {
                row.Add(UITheme.Text($"{name}   Lv{level}", UITheme.TextMain, UITheme.FontBody));
            }
            else
            {
                //锁图标必须用元素绘制（黑体无锁字形），锁定角色不可点击
                row.Add(UITheme.LockIcon());
                var lockText = UITheme.Text($"{name}   Lv{level}   未解锁", UITheme.TextFaint, UITheme.FontBody);
                lockText.style.marginLeft = 6;
                row.Add(lockText);
                btn.SetEnabled(false);
            }
            btn.Add(row);
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
        if (playerLevelLabel != null)
            playerLevelLabel.text = $"玩家等级 Lv{(Tool.SaveManager != null ? Tool.SaveManager.playerLevel : 1)}";
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
        sb.AppendLine($"{campName}选中：{(info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : $"角色 {index}")}   Lv{level}");
        if (info != null)
        {
            var attr = info.GetAttribute(level);
            sb.AppendLine($"生命 {attr.health}   力量 {attr.strength}   魔法 {attr.magic}");
            sb.AppendLine($"暴击 {attr.critRate}% / {attr.critDamage:F1}倍   击退抗性 {attr.knockbackResistance}");
            sb.AppendLine($"可见距离 {attr.viewDistance}m   技能槽位 {attr.weaponSlotCount}");
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
