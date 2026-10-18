using Ros.UI.Main;
using FairyGUI;
using Ros.Info;
using UnityEngine;

public class HomePage : PageBase
{
    private readonly UI_HomePanel panel;
    private bool defenseTab;

    private static readonly string[] StatKeys = { "生命", "力量", "魔法", "暴击", "暴伤", "击退抗", "视野", "技能槽" };
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
        charPanel.m_btn_attacker.onClick.Add(() => SwitchTab(false));
        charPanel.m_btn_defenser.onClick.Add(() => SwitchTab(true));
        // 确认按钮在 FGUI 里没有命名，生成类里没有字段，只能按下标取
        var confirmBtn = charPanel.GetChildAt(2) as GButton;
        if (confirmBtn == null) Debug.LogError("[HomePage] 角色选择面板第 3 个子节点不是按钮，确认按钮接不上");
        else confirmBtn.onClick.Add(() => charPanel.visible = false);

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
        RefreshLists();
        RefreshPlayerInfo();
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
        charPanel.m_btn_attacker.m_selected.selectedIndex = defense ? 0 : 1;
        charPanel.m_btn_defenser.m_selected.selectedIndex = defense ? 1 : 0;
        RefreshLists();
        RefreshAttrList(); // 属性列表与角色列表同面板，切阵营后一起刷新
    }

    private void SetTitles()
    {
        // 子面板标题：UI_panel_2 是 GLabel，标题走 title
        panel.m_characterList.m_panel.title = "角色选择";
        // 按钮标题（GButton.title）
        panel.m_connectPanel.m_btn_editSelection.title = "选择角色"; // 打开角色选择面板
        panel.m_characterList.m_btn_attacker.title = "进攻";
        panel.m_characterList.m_btn_defenser.title = "防守";
        panel.m_connectPanel.m_btn_connect.title = "连接";
    }

    private void RefreshLists()
    {
        var charPanel = panel.m_characterList;
        var infoList = Tool.InfoManager == null ? null
            : defenseTab ? Tool.InfoManager.DefenseCharacterInfoList : Tool.InfoManager.AttackCharacterInfoList;
        charPanel.m_characterList.numItems = infoList != null ? infoList.Count : 0;
    }

    private void RenderCharacter(int index, GObject obj)
    {
        var head = (UI_RoleHead)obj;
        var infoList = Tool.InfoManager != null
            ? (defenseTab ? Tool.InfoManager.DefenseCharacterInfoList : Tool.InfoManager.AttackCharacterInfoList)
            : null;
        var info = infoList != null && index < infoList.Count ? infoList[index] : null;
        string name = info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : (defenseTab ? $"防守角色 {index}" : $"进攻角色 {index}");
        int saveIndex = defenseTab ? Config.attack_character_count + index : index;
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;
        bool unlocked = Tool.SaveManager == null || Tool.SaveManager.IsCharacterUnlocked(saveIndex);
        int selected = defenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;

        head.m_roleName.text = name;
        head.m_level.selectedIndex = Mathf.Clamp(level - 1, 0, head.m_level.pageCount - 1);
        head.m_lock.selectedIndex = unlocked ? 0 : 1;
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

    private void RefreshPlayerInfo()
    {
        var info = panel.m_playerInfo;
        if (info.m_label_level != null)
            info.m_label_level.text = $"玩家等级 Lv{(Tool.SaveManager != null ? Tool.SaveManager.playerLevel : 1)}";
    }

    private void RefreshAttrList()
    {
        int selectedIndex = defenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;
        int saveIndex = defenseTab ? Config.attack_character_count + selectedIndex : selectedIndex;
        var info = Tool.InfoManager == null ? null
            : Tool.InfoManager.GetPlayerCharacterInfo(saveIndex);
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;

        var attrList = panel.m_characterList.m_attrList;
        if (info == null)
        {
            attrList.itemRenderer = (i, obj) => ((UI_AttrItem)obj).m_attrValue.text = "-";
            attrList.numItems = StatKeys.Length;
            return;
        }
        var attr = info.GetAttribute(level);
        var baseAttr = info.GetAttribute(1);
        var nextAttr = level < Config.save_max_level ? info.GetAttribute(level + 1) : null;
        float? nextHealth = nextAttr != null ? nextAttr.health : (float?)null;
        float? nextStrength = nextAttr != null ? nextAttr.strength : (float?)null;
        float? nextMagic = nextAttr != null ? nextAttr.magic : (float?)null;
        float? nextCritRate = nextAttr != null ? nextAttr.critRate : (float?)null;
        float? nextCritDamage = nextAttr != null ? nextAttr.critDamage : (float?)null;
        float? nextKnockbackResistance = nextAttr != null ? nextAttr.knockbackResistance : (float?)null;
        float? nextViewDistance = nextAttr != null ? nextAttr.viewDistance : (float?)null;
        float? nextWeaponSlotCount = nextAttr != null ? nextAttr.weaponSlotCount : (float?)null;
        string[] values =
        {
            StatPart(attr.health, baseAttr.health, nextHealth),
            StatPart(attr.strength, baseAttr.strength, nextStrength),
            StatPart(attr.magic, baseAttr.magic, nextMagic),
            StatPart(attr.critRate, baseAttr.critRate, nextCritRate, "%"),
            StatPart(attr.critDamage, baseAttr.critDamage, nextCritDamage, "x"),
            StatPart(attr.knockbackResistance, baseAttr.knockbackResistance, nextKnockbackResistance),
            StatPart(attr.viewDistance, baseAttr.viewDistance, nextViewDistance, "m"),
            StatPart(attr.weaponSlotCount, baseAttr.weaponSlotCount, nextWeaponSlotCount),
        };
        attrList.itemRenderer = (i, obj) =>
        {
            var item = (UI_AttrItem)obj;
            item.m_attrName.text = i < StatKeys.Length ? StatKeys[i] : "";
            item.m_attrValue.text = i < values.Length ? values[i] : "-";
        };
        attrList.numItems = StatKeys.Length;
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
