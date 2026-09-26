using Ros.Info;
using UnityEngine;
using UnityEngine.UI;

public partial class HomePage : RosPage
{
    public override void Init()
    {
        connectButton.SetCallback(OnConnectClicked);
    }

    public override void Enter(ShowParam param)
    {
        RefreshList();
    }

    /// <summary>连接服务器（成功后自动加入房间并进入组队大厅）。</summary>
    private async void OnConnectClicked()
    {
        if (Tool.NetworkManager == null)
        {
            connectStatusLabel.text = "缺少 NetworkManager，请检查场景配置";
            return;
        }
        connectButton.SetInteractable(false);
        connectStatusLabel.text = "正在连接服务器...";
        var result = await Tool.NetworkManager.TryConnect(IpAddressInputField.text);
        if (result != NetworkManager.ConnectResult.Success)
        {
            connectButton.SetInteractable(true);
            connectStatusLabel.text = result == NetworkManager.ConnectResult.Failed ? "连接失败，请检查服务器地址" : "操作过于频繁，请稍候";
            return;
        }
        connectStatusLabel.text = "已连接";
        Tool.NetworkManager.EnterWorld();
        connectButton.SetInteractable(true);
        //进组队大厅由 UIManager 的 OnConnect 事件统一切页
    }

    /// <summary>刷新两列角色列表、信息卡、玩家等级与场景预览（攻/守选择各自独立）。</summary>
    private void RefreshList()
    {
        if (playerLevelLabel != null)
            playerLevelLabel.text = $"玩家等级 Lv{(Tool.SaveManager != null ? Tool.SaveManager.playerLevel : 1)}";

        var infoManager = Tool.InfoManager;
        var attackList = infoManager != null ? infoManager.AttackCharacterInfoList : null;
        var defenseList = infoManager != null ? infoManager.DefenseCharacterInfoList : null;

        attackerInfoListWrapper.SetItemCount(attackList?.Count ?? 0);
        for (int i = 0; i < attackerInfoListWrapper.Count; i++)
            RenderCharacterItem(attackerInfoListWrapper.GetItem(i), attackList[i], i, false);

        defenserInfoListWrapper.SetItemCount(defenseList?.Count ?? 0);
        for (int i = 0; i < defenserInfoListWrapper.Count; i++)
            RenderCharacterItem(defenserInfoListWrapper.GetItem(i), defenseList[i], i, true);

        RefreshInfoCard(false);
        RefreshInfoCard(true);
        //同步场景预览：当前选中的两角色复制到预览锚点
        Tool.ClientLogicManager?.HomePreview?.Refresh(ClientSelection.selectedAttackIndex, ClientSelection.selectedDefenseIndex);
    }

    private void RenderCharacterItem(HomeCharacterItem item, PlayerCharacterInfo info, int index, bool isDefense)
    {
        string name = info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : (isDefense ? $"防守角色 {index}" : $"进攻角色 {index}");
        int saveIndex = isDefense ? Config.attack_character_count + index : index;
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;
        bool unlocked = Tool.SaveManager == null || Tool.SaveManager.IsCharacterUnlocked(saveIndex);
        int selected = isDefense ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;

        item.NameText.text = name;
        item.LevelText.text = $"Lv{level}";
        //角色头像：图标列表未导入或下标越界时隐藏图标位
        var icons = Tool.AssetsManager == null ? null
            : isDefense ? Tool.AssetsManager.DefenseCharacterIcons : Tool.AssetsManager.AttackCharacterIcons;
        bool hasIcon = icons != null && index < icons.Count && icons[index] != null;
        if (item.Icon != null)
        {
            item.Icon.sprite = hasIcon ? icons[index] : null;
            item.Icon.gameObject.SetActive(hasIcon);
        }
        item.LockRoot.SetActive(!unlocked);
        item.SelectedMark.SetActive(index == selected);
        item.SelectButton.SetInteractable(unlocked);
        item.SelectButton.SetCallback(() =>
        {
            if (isDefense) ClientSelection.selectedDefenseIndex = index;
            else ClientSelection.selectedAttackIndex = index;
            RefreshList();
        });
    }

    /// <summary>刷新指定阵营的选中信息卡（名字+等级、8 个属性数据块、下一级提升）。</summary>
    private void RefreshInfoCard(bool isDefense)
    {
        int selectedIndex = isDefense ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;
        int saveIndex = isDefense ? Config.attack_character_count + selectedIndex : selectedIndex;
        var info = Tool.InfoManager != null
            ? Tool.InfoManager.GetPlayerCharacterInfo(saveIndex)
            : null;
        int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;

        var nameText = isDefense ? defenseSelectedNameText : attackSelectedNameText;
        if (nameText != null)
            nameText.text = $"{(info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : "未选择角色")}   Lv{level}";

        var chips = isDefense ? defenseStatChips : attackStatChips;
        if (chips != null && chips.Count > 0)
        {
            if (info == null)
            {
                foreach (var chip in chips) chip.ValueText.text = "-";
            }
            else
            {
                var attr = info.GetAttribute(level);
                FillChips(chips, $"{attr.health}", $"{attr.strength}", $"{attr.magic}",
                    $"{attr.critRate}%", $"{attr.critDamage:F1}x", $"{attr.knockbackResistance}",
                    $"{attr.viewDistance}m", $"{attr.weaponSlotCount}");
            }
        }

        var hint = isDefense ? defenseLevelUpHintText : attackLevelUpHintText;
        if (hint != null)
        {
            hint.text = info != null && level < Config.max_entity_level
                ? $"下一级：{info.GetNextLevelGainDescription(level)}"
                : (level >= Config.max_entity_level ? "已达等级上限" : "");
        }
    }

    #region//Local
    /// <summary>按固定顺序填属性块：生命/力量/魔法/暴击/暴伤/击退抗/视野/技能槽（与 View 注释的下标约定一致）。</summary>
    private static void FillChips(System.Collections.Generic.List<StatChipItem> chips,
        string health, string strength, string magic, string crit, string critDamage,
        string knockResist, string view, string slots)
    {
        string[] values = { health, strength, magic, crit, critDamage, knockResist, view, slots };
        for (int i = 0; i < chips.Count && i < values.Length; i++)
        {
            if (chips[i] != null && chips[i].ValueText != null) chips[i].ValueText.text = values[i];
        }
    }
    #endregion
}
