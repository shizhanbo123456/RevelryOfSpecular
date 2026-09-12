using Ros.Transport;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 技能槽 UI 单元（战斗 HUD 底部技能栏的一项）。
/// 展示：图标 / 技能名 / CD 遮罩 / 库存 / 武器经验。
/// </summary>
public class BattleSkillUnit
{
    /// <summary>根元素。</summary>
    public VisualElement Root { get; }

    private readonly Image iconImage;
    private readonly Label nameLabel;
    private readonly VisualElement cdFill;
    private readonly Label cdLabel;
    private readonly Label storeLabel;
    private readonly Label expLabel;

    /// <summary>已取过配置的技能 id（-1 = 未取）。技能配置查询是线性扫描，按 id 变化才重查，避免每帧扫表。</summary>
    private int cachedSkillId = -1;
    /// <summary>缓存的技能配置（可能为 null = 未配置该技能）。</summary>
    private SkillInfo cachedInfo;

    public BattleSkillUnit()
    {
        Root = new VisualElement
        {
            style = { width = 96, height = 112, marginRight = 8, alignItems = Align.Center, justifyContent = Justify.Center }
        };
        Root.style.backgroundColor = UITheme.SlotBg;
        UITheme.SetBorder(Root, 2f, UITheme.Border);
        UITheme.SetRadius(Root, UITheme.Radius);
        Root.style.overflow = Overflow.Hidden;

        // 技能图标（InfoManager 技能配置的 icon；未配置时不占位）
        iconImage = new Image
        {
            scaleMode = ScaleMode.ScaleToFit,
            style = { width = 42, height = 42, marginBottom = 2 },
        };
        iconImage.pickingMode = PickingMode.Ignore;
        Root.Add(iconImage);

        nameLabel = UITheme.Text("", UITheme.TextMain, UITheme.FontSmall, true);
        nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        Root.Add(nameLabel);

        storeLabel = UITheme.Text("", UITheme.TextDim, UITheme.FontTiny);
        storeLabel.style.marginTop = 4;
        Root.Add(storeLabel);

        expLabel = UITheme.Text("", UITheme.Green, UITheme.FontTiny);
        Root.Add(expLabel);

        // CD 遮罩（自上而下生长）
        cdFill = new VisualElement { style = { position = Position.Absolute, left = 0, right = 0, top = 0, height = 0 } };
        cdFill.style.backgroundColor = UITheme.CdMask;
        cdFill.pickingMode = PickingMode.Ignore;
        Root.Add(cdFill);

        cdLabel = UITheme.Text("", UITheme.TextMain, 20, true);
        cdLabel.style.position = Position.Absolute;
        cdLabel.style.left = 0;
        cdLabel.style.right = 0;
        cdLabel.style.top = 32;
        cdLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        cdLabel.pickingMode = PickingMode.Ignore;
        Root.Add(cdLabel);
    }

    /// <summary>刷新槽位数据（来源：实体表现摘要的技能槽列表）。</summary>
    public void Refresh(SCEntityDisplayInfo.SkillSlotRuntime slot, bool selected)
    {
        if (slot == null || slot.skillId < 0)
        {
            SetEmpty();
            return;
        }

        var info = GetInfo(slot.skillId);
        ApplyIcon(info);
        nameLabel.text = info != null && !string.IsNullOrEmpty(info.skillName) ? info.skillName : $"技能{slot.skillId}";
        storeLabel.text = slot.store >= 0 ? $"x{slot.store}" : "";
        expLabel.text = slot.exp > 0 ? $"+{slot.exp}" : "";

        // 选中高亮
        Root.style.backgroundColor = selected ? UITheme.SlotSelectedBg : UITheme.SlotBg;
        UITheme.SetBorder(Root, 2f, selected ? UITheme.Accent : UITheme.Border);

        // CD 遮罩（按剩余比例遮挡）
        if (slot.cdTotal > 0f && slot.cdRemain > 0f)
        {
            float ratio = Mathf.Clamp01(slot.cdRemain / slot.cdTotal);
            cdFill.style.height = Length.Percent(ratio * 100f);
            cdLabel.text = slot.cdRemain.ToString("0.0");
        }
        else
        {
            cdFill.style.height = 0;
            cdLabel.text = "";
        }
    }

    /// <summary>空槽位。</summary>
    public void SetEmpty()
    {
        iconImage.sprite = null;
        iconImage.style.display = DisplayStyle.None;
        nameLabel.text = "—";
        storeLabel.text = "";
        expLabel.text = "";
        cdFill.style.height = 0;
        cdLabel.text = "";
        Root.style.backgroundColor = UITheme.SlotBg;
        UITheme.SetBorder(Root, 2f, UITheme.Border);
    }

    /// <summary>按技能 id 取配置（带缓存，避免每帧线性扫描 SkillInfoList）。</summary>
    private SkillInfo GetInfo(int skillId)
    {
        if (skillId != cachedSkillId)
        {
            cachedSkillId = skillId;
            cachedInfo = Tool.InfoManager != null ? Tool.InfoManager.GetSkillInfo(skillId) : null;
        }
        return cachedInfo;
    }

    /// <summary>应用技能图标（SkillInfo.icon；未配置则隐藏该元素）。</summary>
    private void ApplyIcon(SkillInfo info)
    {
        var icon = info != null ? info.icon : null;
        if (iconImage.sprite != icon) iconImage.sprite = icon;
        iconImage.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
