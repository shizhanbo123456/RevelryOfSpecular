using Ros.Transport;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD 技能槽条目（底部技能栏的一项，旧 BattleSkillUnit 的 uGUI 版）。
/// 展示：图标 / 技能名 / 键位 / CD 遮罩与剩余秒 / 库存 / 武器经验 / 选中高亮。
/// </summary>
public class BattleSkillItem : MonoBehaviour
{
    /// <summary>键位提示（与服务器技能槽顺序约定一致：U I O L H）。</summary>
    private static readonly string[] SlotKeys = { "U", "I", "O", "L", "H" };

    [SerializeField] private Image icon;         // 技能图标（SkillInfo.icon，未配置则隐藏）
    [SerializeField] private Text nameText;      // 技能名（SkillInfo.skillName，未配置显示"技能 N"）
    [SerializeField] private Text keyText;       // 键位提示（U/I/O/L/H，超出范围的槽位不显示）
    [SerializeField] private Image cdMask;       // CD 遮罩（Image Type=Filled，fillAmount = 剩余比例，自上而下）
    [SerializeField] private Text cdText;        // CD 剩余秒数（"3.2"，无 CD 时不显示）
    [SerializeField] private Text storeText;     // 剩余库存（"x3"，无限(-1)时不显示）
    [SerializeField] private Text expText;       // 武器经验（"+35"，0 时不显示）
    [SerializeField] private GameObject selectedMark; // 当前选中技能的高亮框

    /// <summary>已取过配置的技能 id（-1 = 未取）。技能配置查询是线性扫描，按 id 变化才重查，避免每帧扫表。</summary>
    private int cachedSkillId = -1;
    private SkillInfo cachedInfo;

    /// <summary>刷新槽位数据（来源：实体表现摘要的技能槽列表）。</summary>
    public void Refresh(SCEntityDisplayInfo.SkillSlotRuntime slot, bool selected, int slotIndex)
    {
        if (slot == null || slot.skillId < 0)
        {
            SetEmpty(slotIndex);
            return;
        }

        var info = GetInfo(slot.skillId);
        ApplyIcon(info);
        if (nameText != null)
            nameText.text = info != null && !string.IsNullOrEmpty(info.skillName) ? info.skillName : $"技能{slot.skillId}";
        if (keyText != null)
            keyText.text = slotIndex < SlotKeys.Length ? SlotKeys[slotIndex] : "";
        if (storeText != null)
            storeText.text = slot.store >= 0 ? $"x{slot.store}" : "";
        if (expText != null)
            expText.text = slot.exp > 0 ? $"+{slot.exp}" : "";

        if (selectedMark != null) selectedMark.SetActive(selected);

        // CD 遮罩（按剩余比例填充）
        if (cdMask != null)
        {
            bool cooling = slot.cdTotal > 0f && slot.cdRemain > 0f;
            cdMask.gameObject.SetActive(cooling);
            if (cooling) cdMask.fillAmount = Mathf.Clamp01(slot.cdRemain / slot.cdTotal);
        }
        if (cdText != null)
            cdText.text = slot.cdTotal > 0f && slot.cdRemain > 0f ? slot.cdRemain.ToString("0.0") : "";
    }

    /// <summary>空槽位。</summary>
    public void SetEmpty(int slotIndex)
    {
        if (icon != null) icon.gameObject.SetActive(false);
        if (nameText != null) nameText.text = "—";
        if (keyText != null) keyText.text = slotIndex < SlotKeys.Length ? SlotKeys[slotIndex] : "";
        if (storeText != null) storeText.text = "";
        if (expText != null) expText.text = "";
        if (cdMask != null) cdMask.gameObject.SetActive(false);
        if (cdText != null) cdText.text = "";
        if (selectedMark != null) selectedMark.SetActive(false);
    }

    private SkillInfo GetInfo(int skillId)
    {
        if (skillId != cachedSkillId)
        {
            cachedSkillId = skillId;
            cachedInfo = Tool.InfoManager != null ? Tool.InfoManager.GetSkillInfo(skillId) : null;
        }
        return cachedInfo;
    }

    private void ApplyIcon(SkillInfo info)
    {
        if (icon == null) return;
        var sprite = info != null ? info.icon : null;
        if (icon.sprite != sprite) icon.sprite = sprite;
        icon.gameObject.SetActive(sprite != null);
    }
}
