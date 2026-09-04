using Ros.Info;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 初始界面（策划案 3.1）：选择角色 / 阵营意向 / 查看角色信息与升级 / 进入匹配大厅。
/// 局外养成只保留角色解锁与角色升级（策划案 8.1）。
/// </summary>
public class HomePage : PageBase
{
    private const string CampAttack = "进攻方";
    private const string CampDefense = "防守方";

    private VisualElement characterList;
    private Label infoLabel;
    private Label levelUpLabel;
    private Button enterLobbyButton;
    private readonly Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.9f);
    private readonly Color normalColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);

    /// <summary>当前展示的阵营角色列表（0 进攻 / 1 防守）。</summary>
    public int CurrentListCamp { get; private set; }

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
        var subtitle = new Label("选择阵营意向与角色，开始匹配")
        {
            style = { color = new Color(0.7f, 0.7f, 0.75f, 1f), fontSize = 16, marginBottom = 18 }
        };
        container.Add(title);
        container.Add(subtitle);

        // 阵营意向
        var campRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 12 } };
        var attackToggle = new Toggle("进攻方") { value = ClientSelection.campIntention == 0 };
        var defenseToggle = new Toggle("防守方") { value = ClientSelection.campIntention == 1 };
        var anyToggle = new Toggle("任意") { value = ClientSelection.campIntention == 2 };
        attackToggle.style.marginRight = 20;
        defenseToggle.style.marginRight = 20;
        campRow.Add(attackToggle);
        campRow.Add(defenseToggle);
        campRow.Add(anyToggle);
        container.Add(campRow);

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

        // 底部按钮
        var bottomRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 16, justifyContent = Justify.SpaceBetween } };
        enterLobbyButton = new Button { text = "进入匹配大厅", style = { width = 200, height = 44, fontSize = 18 } };
        enterLobbyButton.clicked += () => Owner.ShowPage(UIManager.PageType.Lobby);
        bottomRow.Add(enterLobbyButton);
        container.Add(bottomRow);

        root.Add(container);

        // 阵营意向变化刷新列表
        attackToggle.RegisterValueChangedCallback(e =>
        {
            if (e.newValue)
            {
                defenseToggle.value = false;
                anyToggle.value = false;
                ClientSelection.campIntention = 0;
                RefreshList();
            }
        });
        defenseToggle.RegisterValueChangedCallback(e =>
        {
            if (e.newValue)
            {
                attackToggle.value = false;
                anyToggle.value = false;
                ClientSelection.campIntention = 1;
                RefreshList();
            }
        });
        anyToggle.RegisterValueChangedCallback(e =>
        {
            if (e.newValue)
            {
                attackToggle.value = false;
                defenseToggle.value = false;
                ClientSelection.campIntention = 2;
                RefreshList();
            }
        });
    }

    public override void OnEnable()
    {
        RefreshList();
    }

    /// <summary>刷新角色列表与信息面板。</summary>
    public void RefreshList()
    {
        if (characterList == null) return;
        characterList.Clear();

        // 根据阵营意向决定展示的角色池（进攻 18 / 防守 6）
        bool showAttack = ClientSelection.campIntention != 1;
        bool showDefense = ClientSelection.campIntention != 0;
        CurrentListCamp = showAttack ? 0 : 1;

        if (showAttack)
        {
            AddCharacterButtons("—— 进攻方角色 ——", 0, Config.attack_character_count, Tool.InfoManager != null ? Tool.InfoManager.AttackCharacterInfoList : null);
        }
        if (showDefense)
        {
            AddCharacterButtons("—— 防守方角色 ——", Config.attack_character_count, Config.defense_character_count, Tool.InfoManager != null ? Tool.InfoManager.DefenseCharacterInfoList : null);
        }
        RefreshInfo();
    }

    private void AddCharacterButtons(string groupTitle, int startIndex, int count, System.Collections.Generic.List<EntityAttributeInfo> infoList)
    {
        var group = new Label(groupTitle) { style = { color = new Color(0.85f, 0.7f, 0.3f, 1f), fontSize = 16, marginTop = 8, marginBottom = 4 } };
        characterList.Add(group);

        for (int i = 0; i < count; i++)
        {
            int index = startIndex + i;
            var info = infoList != null && i < infoList.Count ? infoList[i] : null;
            string name = info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : $"角色 {index}";
            int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(index) : 1;
            bool unlocked = Tool.SaveManager != null && Tool.SaveManager.IsCharacterUnlocked(index);

            var btn = new Button
            {
                text = $"{name}  Lv{level}  {(unlocked ? "" : "[未解锁]")}",
                style = { marginBottom = 4, unityTextAlign = TextAnchor.MiddleLeft, height = 36 }
            };
            btn.style.backgroundColor = index == ClientSelection.selectedCharacterIndex ? selectedColor : normalColor;
            int captured = index;
            btn.clicked += () =>
            {
                ClientSelection.selectedCharacterIndex = captured;
                RefreshList();
            };
            characterList.Add(btn);
        }
    }

    private void RefreshInfo()
    {
        if (infoLabel == null) return;
        int index = ClientSelection.selectedCharacterIndex;
        var info = GetSelectedAttributeInfo(index);
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(index) : 1;
        bool unlocked = Tool.SaveManager != null && Tool.SaveManager.IsCharacterUnlocked(index);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"当前选中：{(index < Config.attack_character_count ? "进攻方 " : "防守方 ")}角色 {index}  Lv{level}  {(unlocked ? "[已解锁]" : "[未解锁]")}");
        if (info != null)
        {
            var attr = info.GetAttribute(level);
            sb.AppendLine($"生命 {attr.maxHealth}  力量 {attr.strength}  魔法 {attr.magic}");
            sb.AppendLine($"移速 {attr.moveSpeed}  暴击 {attr.critRate}% / {attr.critDamage:F1}倍  击退抗性 {attr.knockbackResistance}");
            sb.AppendLine($"可见距离 {attr.viewDistance}m  武器槽位 {attr.weaponSlotCount}");
        }
        infoLabel.text = sb.ToString();

        if (levelUpLabel != null)
        {
            levelUpLabel.text = info != null && level < Config.max_entity_level
                ? $"下一等级（Lv{level + 1}）：{info.GetNextLevelGainDescription(level)}"
                : (level >= Config.max_entity_level ? "已达等级上限" : "角色未配置属性");
        }
    }

    private EntityAttributeInfo GetSelectedAttributeInfo(int index)
    {
        if (Tool.InfoManager == null) return null;
        if (index < Config.attack_character_count)
        {
            return index < Tool.InfoManager.AttackCharacterInfoList.Count ? Tool.InfoManager.AttackCharacterInfoList[index] : null;
        }
        int d = index - Config.attack_character_count;
        return d < Tool.InfoManager.DefenseCharacterInfoList.Count ? Tool.InfoManager.DefenseCharacterInfoList[d] : null;
    }
}
