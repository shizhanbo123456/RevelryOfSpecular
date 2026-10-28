using Ros.UI.Main;
using FairyGUI;
using UnityEngine;

public class HomePage : PageBase
{
    private readonly UI_HomePanel panel;
    private bool defenseTab;

    private static readonly string[] StatKeys = { "生命", "力量", "魔法", "暴击", "暴伤", "视野", "技能槽" };
    private static readonly string[] AttrSuffixes = { "", "", "", "%", "%", "m", "" }; // 与 StatKeys 一一对应
    private const string GainGreen = "#6BD98C";  // 已取得的累计增益
    private const string NextGainOrange = "#FF9E47"; // 下一级将获得的增益

    public HomePage(UI_HomePanel panel) : base(panel)
    {
        this.panel = panel;
    }

    public override void Construct()
    {
        var charPanel = panel.m_characterList;
        charPanel.visible = false; // 单页布局：角色选择面板默认隐藏，「选择角色」打开
        charPanel.m_characterList.itemRenderer = RenderCharacter;
        charPanel.m_btn_attack.onClick.Add(() => SwitchTab(false));
        charPanel.m_btn_defense.onClick.Add(() => SwitchTab(true));
        BindFrameClose(charPanel.m_panel, charPanel); // 底框自带的关闭按钮 → 收起角色选择面板

        panel.m_connectPanel.m_btn_editSelection.onClick.Add(() => // 打开角色选择面板的按钮在连接面板上
        {
            charPanel.visible = true;
            SwitchTab(defenseTab); // 同步页签选中态：默认进攻页签，避免进入后无任何页签选中
        });
        var nameInput = panel.m_playerInfo.m_input_playerName;
        nameInput.text = ClientSelection.playerName;
        nameInput.onChanged.Add(() =>
        {
            ClientSelection.playerName = nameInput.text;
            if (Tool.SaveManager != null)
            {
                Tool.SaveManager.playerName = nameInput.text;
                Tool.SaveManager.Save();
            }
        });

        panel.m_connectPanel.m_btn_connect.onClick.Add(OnConnectClicked);
        SetTitles(); // 集中设置子面板标题与各按钮标题
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        panel.m_playerInfo.m_input_playerName.text = ClientSelection.playerName;
        RefreshPlayerExp();
        RefreshLists();
        RefreshAttrList();
        panel.m_characterList.visible = false; // 每次进入首页收起角色选择面板
        //同步场景预览：当前选中的两角色复制到预览锚点
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
            Tool.ClientLogicManager.HomePreview.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
    }

    #region//Local
    private void SwitchTab(bool defense)
    {
        defenseTab = defense;
        var charPanel = panel.m_characterList;
        charPanel.m_btn_attack.selected = !defense;
        charPanel.m_btn_defense.selected = defense;
        RefreshLists();
        RefreshAttrList(); // 属性列表与角色列表同面板，切阵营后一起刷新
    }

    private void SetTitles()
    {
        // 子面板标题：UI_panel_2 是 GLabel，标题走 title
        panel.m_characterList.m_panel.title = "角色选择";
        // 按钮标题（GButton.title）
        panel.m_connectPanel.m_btn_editSelection.title = "选择角色"; // 打开角色选择面板
        panel.m_characterList.m_btn_attack.title = "进攻";
        panel.m_characterList.m_btn_defense.title = "防守";
        panel.m_connectPanel.m_btn_connect.title = "连接";
    }

    private void RefreshLists()
    {
        var charPanel = panel.m_characterList;
        charPanel.m_characterList.numItems = defenseTab ? Config.defense_character_count : Config.attack_character_count;
    }

    private void RenderCharacter(int index, GObject obj)
    {
        var head = (UI_RoleHead)obj;
        int saveIndex = defenseTab ? Config.attack_character_count + index : index;
        ConfigManager.EntityConfig info = Tool.InfoManager != null ? Tool.InfoManager.GetPlayerCharacterInfo(saveIndex) : default;
        string name = !string.IsNullOrEmpty(info.name) ? info.name : (defenseTab ? $"防守角色 {index}" : $"进攻角色 {index}");
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;
        bool unlocked = Tool.SaveManager == null || Tool.SaveManager.IsCharacterUnlocked(saveIndex);
        int selected = defenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;

        head.m_roleName.text = name;
        head.m_level.selectedIndex = Mathf.Clamp(level - 1, 0, head.m_level.pageCount - 1);
        head.m_lock.selectedIndex = unlocked ? 0 : 1;
        head.m_quality.selectedIndex = info.quality;
        head.enabled = unlocked;

        //头像：图标列表未导入或下标越界时置空
        var icons = Tool.AssetsManager == null ? null
            : defenseTab ? Tool.AssetsManager.DefenseCharacterIcons : Tool.AssetsManager.AttackCharacterIcons;
        bool hasIcon = icons != null && index < icons.Count && icons[index] != null;
        head.m_headIcon.texture = hasIcon ? new NTexture(icons[index]) : null;

        //点击选中（FGUI onClick 为追加制，先清后加以防列表复用叠加）
        head.onClick.Set(() => { });
        head.onClick.Add(() =>
        {
            if (defenseTab) ClientSelection.selectedDefenseIndex = index;
            else ClientSelection.selectedAttackIndex = index;
            RefreshLists();
            RefreshAttrList();
            if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
                Tool.ClientLogicManager.HomePreview.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
        });
    }

    // 玩家经验条：进度 = 当前级经验 / 升到下一级所需（经验与等级都在存档里，满级拉满）
    private void RefreshPlayerExp()
    {
        var info = panel.m_playerInfo;
        int level = Tool.SaveManager != null ? Tool.SaveManager.playerLevel : 1;
        int exp = Tool.SaveManager != null ? Tool.SaveManager.playerExp : 0;
        bool maxed = level >= Config.player_max_level;
        int need = maxed ? 0 : Config.GetPlayerLevelUpExp(level);
        info.m_bar_fill_energy.fillAmount = need > 0 ? Mathf.Clamp01((float)exp / need) : 1f;
        info.m_label_exp.text = need > 0 ? $"{exp}/{need}" : "MAX";
    }

    private void RefreshAttrList()
    {
        int selectedIndex = defenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;
        int saveIndex = defenseTab ? Config.attack_character_count + selectedIndex : selectedIndex;
        ConfigManager.EntityConfig info = Tool.InfoManager != null ? Tool.InfoManager.GetPlayerCharacterInfo(saveIndex) : default;
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;

        var attrList = panel.m_characterList.m_attrList;
        float[] baseValues = ToValues(info.GetAttribute(1));    // 基础：1 级属性
        float[] curValues = ToValues(info.GetAttribute(level)); // 当前等级加成后
        var nextAttr = level < Config.save_max_level ? info.GetAttribute(level + 1) : null;
        float[] nextValues = nextAttr != null ? ToValues(nextAttr) : curValues; // 满级没有下一级：条与当前一致

        string[] values = new string[StatKeys.Length];
        for (int i = 0; i < StatKeys.Length; i++)
        {
            float? next = nextAttr != null ? nextValues[i] : (float?)null;
            values[i] = StatPart(curValues[i], baseValues[i], next, AttrSuffixes[i]);
        }

        attrList.itemRenderer = (i, obj) =>
        {
            var item = (UI_AttrItem)obj;
            var range = ConfigManager.AttrRangeConfig.Get(i); // 属性条比例：最小值=空、最大值=满
            item.m_show_bg.selectedIndex = (i + 1) % 2;        // 条目底色：隔行交替
            item.m_attrName.text = StatKeys[i];
            item.m_attrValue.text = values[i];
            item.m_base.fillAmount = range.Ratio(baseValues[i]);
            item.m_growth_fill.fillAmount = range.Ratio(curValues[i]);
            item.m_next_level_fill.fillAmount = range.Ratio(nextValues[i]);
        };
        attrList.numItems = StatKeys.Length;
    }

    private static float[] ToValues(EntityAttribute attr)
    {
        return new[]
        {
            attr.health, attr.strength, attr.magic, attr.critRate,
            attr.critDamage, attr.viewDistance, attr.weaponSlotCount,
        };
    }

    private static string StatPart(float current, float baseVal, float? nextVal, string suffix = "")
    {
        string s = Num(current) + suffix;
        float gain = current - baseVal;
        if (!Mathf.Approximately(gain, 0f))
            s += $"[color={GainGreen}]+{Num(gain)}{suffix}[/color]";
        if (nextVal.HasValue)
        {
            float next = nextVal.Value - current;
            if (!Mathf.Approximately(next, 0f))
                s += $"[color={NextGainOrange}]（+{Num(next)}{suffix}）[/color]";
        }
        return s;
    }

    private static string Num(float v) => v.ToString("0.#");

    private async void OnConnectClicked()
    {
        // 连接前校验玩家名字长度，不合法则飘字提示并拦截连接
        var name = ClientSelection.playerName;
        if (string.IsNullOrEmpty(name) || name.Length < 2 || name.Length > 8)
        {
            if (Tool.UIManager != null) Tool.UIManager.ShowFlyText("玩家名字必须为2-8个字符");
            return;
        }
        if (Tool.NetworkManager == null) return;
        var result = await Tool.NetworkManager.TryConnect(panel.m_connectPanel.m_input_ipaddress.m_content.text);
        if (result != NetworkManager.ConnectResult.Success) return;
        Tool.NetworkManager.EnterWorld();
    }
    #endregion
}
