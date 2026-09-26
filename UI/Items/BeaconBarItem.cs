using Ros.Transport;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 守护点血量条目（战斗 HUD 右侧面板的一项，旧 BeaconBarUnit 的 uGUI 版）。
/// 展示：守护点名 / 血量数字 / 血条 / 减伤叠层；被摧毁置灰。
/// </summary>
public class BeaconBarItem : MonoBehaviour
{
    [SerializeField] private Text nameLabel;      // 守护点名（"中心守护点"/"外围守护点 N"）
    [SerializeField] private Text healthText;     // 血量数字（"5000/5000"，摧毁后显示"已摧毁"）
    [SerializeField] private Image healthFill;    // 血条填充（Image Type=Filled，红）
    [SerializeField] private Text shieldText;     // 减伤叠层（"减伤叠层 ×N"，无则不显示）
    [SerializeField] private GameObject destroyedMark; // 被摧毁标记（可选：血条置灰说明等）

    /// <summary>刷新守护点数据（来源：实体表现摘要；减伤叠层按 Buff 判断）。</summary>
    public void Refresh(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        if (nameLabel != null)
            nameLabel.text = info.type == EntityType.CoreBeacon ? "中心守护点" : $"外围守护点 {info.type.value}";
        if (healthText != null)
        {
            healthText.text = $"{info.health}/{info.maxHealth}";
            healthText.color = Color.white;
        }
        if (healthFill != null)
            healthFill.fillAmount = info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f;
        if (destroyedMark != null) destroyedMark.SetActive(false);

        // 减伤叠层 = 「守护点减伤」Buff 的等级（守护点数量分层机制/教皇守护）
        int shieldLayer = 0;
        foreach (var buff in info.buffs)
        {
            if (buff != null && buff.type == (int)EffectType.BeaconReduce)
            {
                shieldLayer = Mathf.Max(shieldLayer, buff.level);
            }
        }
        if (shieldText != null)
        {
            shieldText.text = shieldLayer > 0 ? $"减伤叠层 ×{shieldLayer}" : "";
            shieldText.gameObject.SetActive(shieldLayer > 0);
        }
    }

    /// <summary>守护点被摧毁时置灰。</summary>
    public void SetDestroyed()
    {
        if (healthFill != null) healthFill.fillAmount = 0f;
        if (healthText != null)
        {
            healthText.text = "已摧毁";
            healthText.color = new Color(0.48f, 0.48f, 0.56f);
        }
        if (shieldText != null) shieldText.text = "";
        if (destroyedMark != null) destroyedMark.SetActive(true);
    }
}
