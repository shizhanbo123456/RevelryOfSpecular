using Ros.Transport;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 技能槽 UI 单元（战斗 HUD 底部技能栏的一项）。
/// 展示：图标名 / 选中高亮 / CD 遮罩 / 库存 / 武器经验。
/// </summary>
public class BattleSkillUnit
{
    /// <summary>根元素。</summary>
    public VisualElement Root { get; }

    private readonly Label nameLabel;
    private readonly VisualElement cdFill;
    private readonly Label cdLabel;
    private readonly Label storeLabel;
    private readonly Label expLabel;

    private static readonly Color NormalBg = new Color(0.15f, 0.15f, 0.2f, 0.95f);
    private static readonly Color SelectedBg = new Color(0.3f, 0.6f, 1f, 0.95f);
    private static readonly Color CdMask = new Color(0f, 0f, 0f, 0.65f);

    public BattleSkillUnit()
    {
        Root = new VisualElement
        {
            style =
            {
                width = 96, height = 108,
                backgroundColor = NormalBg,
                borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                borderTopColor = Color.gray, borderBottomColor = Color.gray,
                borderLeftColor = Color.gray, borderRightColor = Color.gray,
                marginRight = 8,
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                position = Position.Relative,
            }
        };

        nameLabel = new Label { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } };
        Root.Add(nameLabel);

        storeLabel = new Label { style = { color = new Color(0.8f, 0.8f, 0.85f, 1f), fontSize = 12, marginTop = 4 } };
        Root.Add(storeLabel);

        expLabel = new Label { style = { color = new Color(0.4f, 0.9f, 0.5f, 1f), fontSize = 12 } };
        Root.Add(expLabel);

        // CD 遮罩（从底部生长）
        cdFill = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 0,
                height = 0,
                backgroundColor = CdMask,
            }
        };
        cdFill.pickingMode = PickingMode.Ignore;
        Root.Add(cdFill);

        cdLabel = new Label
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 30,
                color = Color.white, fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold,
                unityTextAlign = TextAnchor.MiddleCenter,
            }
        };
        cdLabel.pickingMode = PickingMode.Ignore;
        Root.Add(cdLabel);
    }

    /// <summary>刷新槽位数据。</summary>
    public void Refresh(SCSkillRuntimeInfo.SkillSlotRuntime slot, bool selected)
    {
        if (slot == null || slot.skillId < 0)
        {
            SetEmpty();
            return;
        }

        string skillName = GetSkillName(slot.skillId);
        nameLabel.text = skillName;
        storeLabel.text = slot.store >= 0 ? $"x{slot.store}" : "";
        expLabel.text = slot.exp > 0 ? $"+{slot.exp}" : "";

        // 选中高亮
        Root.style.backgroundColor = selected ? SelectedBg : NormalBg;
        Root.style.borderTopColor = selected ? Color.yellow : Color.gray;
        Root.style.borderBottomColor = selected ? Color.yellow : Color.gray;
        Root.style.borderLeftColor = selected ? Color.yellow : Color.gray;
        Root.style.borderRightColor = selected ? Color.yellow : Color.gray;

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
        nameLabel.text = "—";
        storeLabel.text = "";
        expLabel.text = "";
        cdFill.style.height = 0;
        cdLabel.text = "";
        Root.style.backgroundColor = NormalBg;
        Root.style.borderTopColor = Color.gray;
        Root.style.borderBottomColor = Color.gray;
        Root.style.borderLeftColor = Color.gray;
        Root.style.borderRightColor = Color.gray;
    }

    private static string GetSkillName(int skillId)
    {
        if (Tool.InfoManager != null)
        {
            var info = Tool.InfoManager.GetSkillInfo(skillId);
            if (info != null && !string.IsNullOrEmpty(info.skillName)) return info.skillName;
        }
        return $"技能{skillId}";
    }
}
