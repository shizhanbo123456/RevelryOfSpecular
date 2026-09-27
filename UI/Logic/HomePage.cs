using Ros.UI.Main;
using FairyGUI;
using Ros.Info;
using UnityEngine;

/// <summary>
/// 首页逻辑（FGUI）：m_page 控制器切换「玩家信息页(0)/角色列表页(1)」；
/// 角色列表按页签显示攻/守阵营（UI_RoleHead：锁定/等级/头像/名字）；
/// 属性列表 UBB 三段式（当前值+绿累计增益+橙下级增益）；连接服务器；场景预览联动。
/// </summary>
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
        panel.m_page.selectedIndex = 0; // 默认玩家信息页

        var charPanel = panel.m_characterPanel;
        charPanel.m_characterList.itemRenderer = RenderCharacter;
        charPanel.m_btn_attacker.onClick.Add(() => SwitchTab(false));
        charPanel.m_btn_defenser.onClick.Add(() => SwitchTab(true));
        charPanel.m_btn_finish.onClick.Add(() => panel.m_page.selectedIndex = 0); // 编辑完成 → 玩家信息页

        panel.m_playerInfo.m_btn_editSelectedCharacter.onClick.Add(() => panel.m_page.selectedIndex = 1);
        var nameInput = panel.m_playerInfo.m_input_playerName;
        nameInput.text = ClientSelection.playerName;
        nameInput.onChanged.Add(() => ClientSelection.playerName = nameInput.text);

        panel.m_connectPanel.m_btn_connect.onClick.Add(OnConnectClicked);
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        RefreshLists();
        RefreshPlayerInfo();
        RefreshAttrList();
        //同步场景预览：当前选中的两角色复制到预览锚点
        Tool.ClientLogicManager?.HomePreview?.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
    }

    #region//Local
    private void SwitchTab(bool defense)
    {
        defenseTab = defense;
        panel.m_characterPanel.m_btn_attacker.m_selected.selectedIndex = defense ? 0 : 1;
        panel.m_characterPanel.m_btn_defenser.m_selected.selectedIndex = defense ? 1 : 0;
        RefreshLists();
    }

    /// <summary>刷新两页签对应阵营的角色列表（GList 虚拟渲染）。</summary>
    private void RefreshLists()
    {
        var charPanel = panel.m_characterPanel;
        var infoList = Tool.InfoManager == null ? null
            : defenseTab ? Tool.InfoManager.DefenseCharacterInfoList : Tool.InfoManager.AttackCharacterInfoList;
        charPanel.m_characterList.numItems = infoList?.Count ?? 0;
    }

    private void RenderCharacter(int index, GObject obj)
    {
        var head = (UI_RoleHead)obj;
        var infoList = defenseTab ? Tool.InfoManager?.DefenseCharacterInfoList : Tool.InfoManager?.AttackCharacterInfoList;
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
            Tool.ClientLogicManager?.HomePreview?.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
        });
    }

    /// <summary>玩家信息页：名字/等级/两阵营选中角色 RoleHead。</summary>
    private void RefreshPlayerInfo()
    {
        var info = panel.m_playerInfo;
        if (info.m_label_level != null)
            info.m_label_level.text = $"玩家等级 Lv{(Tool.SaveManager != null ? Tool.SaveManager.playerLevel : 1)}";
        RenderHead(info.m_selectedAttacker, ClientSelection.selectedAttackIndex, false);
        RenderHead(info.m_selectedDefenser, ClientSelection.selectedDefenseIndex, true);
    }

    private void RenderHead(UI_RoleHead head, int index, bool isDefense)
    {
        var info = Tool.InfoManager == null ? null
            : isDefense ? (index < Tool.InfoManager.DefenseCharacterInfoList.Count ? Tool.InfoManager.DefenseCharacterInfoList[index] : null)
                        : (index < Tool.InfoManager.AttackCharacterInfoList.Count ? Tool.InfoManager.AttackCharacterInfoList[index] : null);
        int saveIndex = isDefense ? Config.attack_character_count + index : index;
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;
        head.m_roleName.text = info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : $"角色 {index}";
        head.m_level.selectedIndex = Mathf.Clamp(level - 1, 0, head.m_level.pageCount - 1);
        head.m_lock.selectedIndex = 0;
        var icons = Tool.AssetsManager == null ? null
            : isDefense ? Tool.AssetsManager.DefenseCharacterIcons : Tool.AssetsManager.AttackCharacterIcons;
        bool hasIcon = icons != null && index < icons.Count && icons[index] != null;
        head.m_headIcon.texture = hasIcon ? new NTexture(icons[index]) : null;
    }

    /// <summary>属性列表：UBB 三段式（当前值白 + 绿累计增益 + 橙下级增益）。</summary>
    private void RefreshAttrList()
    {
        int selectedIndex = defenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;
        int saveIndex = defenseTab ? Config.attack_character_count + selectedIndex : selectedIndex;
        var info = Tool.InfoManager == null ? null
            : Tool.InfoManager.GetPlayerCharacterInfo(saveIndex);
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;

        var attrList = panel.m_attributePanel.m_attrList;
        if (info == null)
        {
            attrList.itemRenderer = (i, obj) => ((UI_AttrItem)obj).m_attrValue.text = "-";
            attrList.numItems = StatKeys.Length;
            return;
        }
        var attr = info.GetAttribute(level);
        var baseAttr = info.GetAttribute(1);
        var nextAttr = level < Config.max_entity_level ? info.GetAttribute(level + 1) : null;
        string[] values =
        {
            StatPart(attr.health, baseAttr.health, nextAttr?.health),
            StatPart(attr.strength, baseAttr.strength, nextAttr?.strength),
            StatPart(attr.magic, baseAttr.magic, nextAttr?.magic),
            StatPart(attr.critRate, baseAttr.critRate, nextAttr?.critRate, "%"),
            StatPart(attr.critDamage, baseAttr.critDamage, nextAttr?.critDamage, "x"),
            StatPart(attr.knockbackResistance, baseAttr.knockbackResistance, nextAttr?.knockbackResistance),
            StatPart(attr.viewDistance, baseAttr.viewDistance, nextAttr?.viewDistance, "m"),
            StatPart(attr.weaponSlotCount, baseAttr.weaponSlotCount, nextAttr?.weaponSlotCount),
        };
        attrList.itemRenderer = (i, obj) =>
        {
            var item = (UI_AttrItem)obj;
            item.m_attrName.text = i < StatKeys.Length ? StatKeys[i] : "";
            item.m_attrValue.text = i < values.Length ? values[i] : "-";
        };
        attrList.numItems = StatKeys.Length;
    }

    /// <summary>组装单个属性的 UBB 显示串。</summary>
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

    /// <summary>连接服务器（成功后由 UIManager 的 OnConnect 事件统一切页）。</summary>
    private async void OnConnectClicked()
    {
        if (Tool.NetworkManager == null) return;
        var result = await Tool.NetworkManager.TryConnect(panel.m_connectPanel.m_input_ipaddress.m_content.text);
        if (result != NetworkManager.ConnectResult.Success) return;
        Tool.NetworkManager.EnterWorld();
    }
    #endregion
}
