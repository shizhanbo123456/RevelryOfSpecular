using System;
using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 角色选择浮层：进攻/防守页签、角色列表、属性表（三条进度条 + UBB 三段文本）
    public partial class UI_HomeCharacterList
    {
        private static readonly string[] StatKeys = { "生命", "攻击", "暴击", "暴伤", "视野", "技能槽" };
        private static readonly string[] AttrSuffixes = { "", "", "%", "%", "m", "" }; // 与 StatKeys 一一对应
        private const string GainGreen = "#6BD98C";    // 已取得的累计增益
        private const string NextGainOrange = "#FF9E47"; // 下一级将获得的增益

        private Action onSelectionChanged;

        public bool DefenseTab { get; private set; }

        // 构造期一次：标题、页签、列表渲染器、底框关闭按钮（点了收起浮层）
        public void Build(Action selectionChanged)
        {
            onSelectionChanged = selectionChanged;
            visible = false;
            if (m_panel != null)
            {
                m_panel.title = "角色选择";
                if (m_panel.m_close != null) m_panel.m_close.onClick.Set(() => visible = false);
            }
            if (m_characterList != null) m_characterList.itemRenderer = RenderCharacter;
            if (m_btn_attack != null)
            {
                m_btn_attack.title = "进攻";
                m_btn_attack.onClick.Add(() => SwitchTab(false));
            }
            if (m_btn_defense != null)
            {
                m_btn_defense.title = "防守";
                m_btn_defense.onClick.Add(() => SwitchTab(true));
            }
            ApplyTabMark();
        }

        // 打开：同步页签选中态（默认进攻页签，避免进入后无任何页签选中）
        public void Open()
        {
            visible = true;
            SwitchTab(DefenseTab);
        }

        public void Close() => visible = false;

        // 列表与属性表同面板，一起重画
        public void Refresh()
        {
            if (m_characterList != null)
                m_characterList.numItems = DefenseTab ? Config.defense_character_count : Config.attack_character_count;
            RefreshAttrList();
        }

        private void SwitchTab(bool defense)
        {
            DefenseTab = defense;
            ApplyTabMark();
            Refresh();
        }

        private void ApplyTabMark()
        {
            if (m_btn_attack != null) m_btn_attack.selected = !DefenseTab;
            if (m_btn_defense != null) m_btn_defense.selected = DefenseTab;
        }

        private void RenderCharacter(int index, GObject obj)
        {
            var head = (UI_RoleHead)obj;
            int saveIndex = DefenseTab ? Config.attack_character_count + index : index;
            ConfigManager.EntityConfig info = Tool.InfoManager != null ? Tool.InfoManager.GetPlayerCharacterInfo(saveIndex) : default;
            string name = !string.IsNullOrEmpty(info.name) ? info.name : (DefenseTab ? $"防守角色 {index}" : $"进攻角色 {index}");
            int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;
            bool unlocked = Tool.SaveManager == null || Tool.SaveManager.IsCharacterUnlocked(saveIndex);
            int selected = DefenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;

            head.m_roleName.text = name;
            head.m_level.selectedIndex = Mathf.Clamp(level - 1, 0, head.m_level.pageCount - 1);
            head.m_lock.selectedIndex = unlocked ? 0 : 1;
            head.m_quality.selectedIndex = info.quality;
            head.enabled = unlocked;

            // 头像：图标列表未导入或下标越界时置空
            var icons = Tool.AssetsManager == null ? null
                : DefenseTab ? Tool.AssetsManager.DefenseCharacterIcons : Tool.AssetsManager.AttackCharacterIcons;
            bool hasIcon = icons != null && index < icons.Count && icons[index] != null;
            head.m_headIcon.texture = hasIcon ? new NTexture(icons[index]) : null;

            // 点击选中（FGUI onClick 为追加制，先清后加以防列表复用叠加）
            head.onClick.Set(() => { });
            head.onClick.Add(() =>
            {
                if (DefenseTab) ClientSelection.selectedDefenseIndex = index;
                else ClientSelection.selectedAttackIndex = index;
                Refresh();
                onSelectionChanged?.Invoke();
            });
        }

        private void RefreshAttrList()
        {
            if (m_attrList == null) return;
            int selectedIndex = DefenseTab ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;
            int saveIndex = DefenseTab ? Config.attack_character_count + selectedIndex : selectedIndex;
            ConfigManager.EntityConfig info = Tool.InfoManager != null ? Tool.InfoManager.GetPlayerCharacterInfo(saveIndex) : default;
            int level = Tool.SaveManager != null ? Tool.SaveManager.GetCharacterLevel(saveIndex) : 1;

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

            m_attrList.itemRenderer = (i, obj) =>
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
            m_attrList.numItems = StatKeys.Length;
        }

        private static float[] ToValues(EntityAttribute attr)
        {
            return new[]
            {
                attr.health, attr.attack, attr.critRate,
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
    }
}
