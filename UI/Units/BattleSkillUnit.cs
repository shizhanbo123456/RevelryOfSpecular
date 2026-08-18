using UnityEngine;
using UnityEngine.UI;

public class BattleSkillUnit : MonoBehaviour
{
    public Image Icon;
    public Image CooldownFill;

    public Text CountLabel;

    public GameObject Selected;

    public void RefreshBasic(int skillId)
    {
        gameObject.SetActive(skillId >= 0);
        if (skillId < 0) return;

        if (Icon != null &&
            Tool.InfoManager != null &&
            skillId < Tool.InfoManager.SkillInfoList.Count)
        {
            Icon.sprite = Tool.InfoManager.SkillInfoList[skillId].sprite;
        }
    }
    public void RefreshRuntime(int count, float cooldownRate, bool selected)
    {
        CountLabel.text = count.ToString();
        CooldownFill.fillAmount = Mathf.Clamp01(cooldownRate);
        Selected.SetActive(selected);
    }
}
