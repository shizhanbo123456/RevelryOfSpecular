using Ros.Info;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 初始界面（策划案 3.1）：底部横条选进攻/防守角色（页签切换），上方场景展示两角色预览。
/// 面板默认收起成一条细栏（不遮挡场景中段），点「展开」或点页签展开列表；
/// 角色属性用紧凑数据块呈现，不放大段文字。连接成功后进入组队大厅。
/// </summary>
public class HomePage : PageBase
{
    private VisualElement pageRoot;
    private VisualElement tabBar;
    private VisualElement characterList;
    private VisualElement bodyRow;
    private Label infoNameLabel;
    private VisualElement statsRow;
    private Label levelUpLabel;
    private Label playerLevelLabel;
    private TextField ipField;
    private Label connectStatusLabel;
    private Button connectButton;
    private Button toggleButton;
    private bool defenseTab;
    private bool expanded;

    private const float HeightCollapsed = 62f;
    private const float HeightExpanded = 380f;

    protected override void Build(VisualElement root)
    {
        var page = UITheme.Page();
        pageRoot = page;

        //底部条：贴底横条（高度随收起/展开切换），让出上方场景
        page.style.position = Position.Absolute;
        page.style.left = 0;
        page.style.right = 0;
        page.style.bottom = 0;
        page.style.height = HeightCollapsed;
        page.style.paddingLeft = 20;
        page.style.paddingRight = 20;
        page.style.paddingTop = 10;
        page.style.paddingBottom = 10;
        page.style.backgroundColor = new Color(UITheme.PageBg.r, UITheme.PageBg.g, UITheme.PageBg.b, 0.9f);

        //顶行：收起/展开 + 页签 + 玩家等级 + 连接区（收起态也全部可用）
        var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        page.Add(headerRow);

        toggleButton = UITheme.StyleButton(new Button(ToggleExpanded) { text = "展开" }, false, 84f, 38f);
        headerRow.Add(toggleButton);

        tabBar = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginLeft = 10 } };
        headerRow.Add(tabBar);

        playerLevelLabel = UITheme.Text("", UITheme.Gold, UITheme.FontBody);
        playerLevelLabel.style.marginLeft = 12;
        playerLevelLabel.style.marginRight = 10;
        headerRow.Add(playerLevelLabel);

        ipField = UITheme.StyleField(new TextField { value = "" }, 190f);
        headerRow.Add(ipField);

        connectButton = UITheme.StyleButton(new Button(OnConnectClicked) { text = "连接服务器" }, true, 140f, 40f);
        connectButton.style.marginLeft = 8;
        headerRow.Add(connectButton);

        connectStatusLabel = UITheme.Text("", UITheme.Warn, UITheme.FontSmall);
        connectStatusLabel.style.marginLeft = 10;
        headerRow.Add(connectStatusLabel);

        //展开区：左侧角色列表 + 右侧紧凑属性（收起态隐藏）
        bodyRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginTop = 10 } };
        page.Add(bodyRow);

        var listCard = UITheme.Card();
        listCard.style.flexGrow = 1;
        listCard.style.marginRight = 12;
        characterList = new ScrollView { style = { flexGrow = 1 } };
        listCard.Add(characterList);
        bodyRow.Add(listCard);

        var infoCard = UITheme.Card(430f);
        infoNameLabel = UITheme.Text("", UITheme.TextMain, UITheme.FontHeading, true);
        infoNameLabel.style.marginBottom = 8;
        infoCard.Add(infoNameLabel);
        statsRow = new VisualElement
        {
            style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, flexGrow = 1 }
        };
        infoCard.Add(statsRow);
        levelUpLabel = UITheme.Text("", UITheme.Green, UITheme.FontSmall);
        levelUpLabel.style.marginTop = 8;
        infoCard.Add(levelUpLabel);
        bodyRow.Add(infoCard);

        root.Add(page);
        ApplyExpanded();
    }

    public override void OnEnable()
    {
        RefreshList();
    }

    public override void OnDisable()
    {
        //离开首页即清理预览模型（进组队大厅/战斗）
        Tool.ClientLogicManager?.HomePreview?.Hide();
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

    private void ToggleExpanded()
    {
        expanded = !expanded;
        ApplyExpanded();
    }

    private void ApplyExpanded()
    {
        if (pageRoot == null || bodyRow == null || toggleButton == null) return;
        pageRoot.style.height = expanded ? HeightExpanded : HeightCollapsed;
        bodyRow.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
        toggleButton.text = expanded ? "收起" : "展开";
    }

    /// <summary>刷新页签、角色列表、信息面板与场景预览（页签决定列表显示哪个阵营，两阵营各自独立选择）。</summary>
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
        //同步场景预览：当前选中的两角色复制到预览锚点
        Tool.ClientLogicManager?.HomePreview?.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
    }

    /// <summary>构建进攻/防守两个页签（当前页签用主按钮色高亮，页签上带当前选中角色名）。</summary>
    private void BuildTabs()
    {
        tabBar.Clear();
        tabBar.Add(MakeTab("进攻方", false, ClientSelection.selectedAttackIndex));
        var defense = MakeTab("防守方", true, ClientSelection.selectedDefenseIndex);
        defense.style.marginLeft = 8;
        tabBar.Add(defense);
    }

    private Button MakeTab(string title, bool isDefense, int selectedIndex)
    {
        var info = GetAttributeInfo(selectedIndex, isDefense);
        string name = info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : $"角色 {selectedIndex}";
        var btn = UITheme.StyleButton(new Button(() =>
        {
            bool wasCollapsed = !expanded;
            defenseTab = isDefense;
            expanded = true;
            if (wasCollapsed) ApplyExpanded();
            RefreshList();
        }) { text = $"{title} · {name}" }, defenseTab == isDefense, 0f, 38f);
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
        if (infoNameLabel == null) return;
        if (playerLevelLabel != null)
            playerLevelLabel.text = $"玩家等级 Lv{(Tool.SaveManager != null ? Tool.SaveManager.playerLevel : 1)}";

        int selectedIndex = defenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;
        int saveIndex = defenseTab ? Config.attack_character_count + selectedIndex : selectedIndex;
        var info = GetAttributeInfo(selectedIndex, defenseTab);
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;

        infoNameLabel.text = $"{(info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : "未选择角色")}   Lv{level}";
        BuildStatChips(info, level);

        if (levelUpLabel != null)
        {
            levelUpLabel.text = info != null && level < Config.max_entity_level
                ? $"下一级：{info.GetNextLevelGainDescription(level)}"
                : (level >= Config.max_entity_level ? "已达等级上限" : "");
        }
    }

    #region//Local
    /// <summary>把属性铺成紧凑数据块（键值小片，两行换行排布），替代大段文字。</summary>
    private void BuildStatChips(EntityAttributeInfo info, int level)
    {
        statsRow.Clear();
        if (info == null)
        {
            statsRow.Add(UITheme.Text("该角色未配置属性", UITheme.TextFaint, UITheme.FontSmall));
            return;
        }
        var attr = info.GetAttribute(level);
        statsRow.Add(StatChip("生命", $"{attr.health}"));
        statsRow.Add(StatChip("力量", $"{attr.strength}"));
        statsRow.Add(StatChip("魔法", $"{attr.magic}"));
        statsRow.Add(StatChip("暴击", $"{attr.critRate}%"));
        statsRow.Add(StatChip("暴伤", $"{attr.critDamage:F1}x"));
        statsRow.Add(StatChip("击退抗", $"{attr.knockbackResistance}"));
        statsRow.Add(StatChip("视野", $"{attr.viewDistance}m"));
        statsRow.Add(StatChip("技能槽", $"{attr.weaponSlotCount}"));
    }

    private static VisualElement StatChip(string key, string value)
    {
        var chip = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
        chip.style.backgroundColor = UITheme.SunkenBg;
        chip.style.marginRight = 8;
        chip.style.marginBottom = 8;
        UITheme.SetRadius(chip, UITheme.RadiusSmall);
        UITheme.SetPadding(chip, 6f);
        chip.Add(UITheme.Text(key, UITheme.TextFaint, UITheme.FontTiny));
        var valueLabel = UITheme.Text(value, UITheme.TextMain, UITheme.FontBody);
        valueLabel.style.marginLeft = 6;
        chip.Add(valueLabel);
        return chip;
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
    #endregion
}
