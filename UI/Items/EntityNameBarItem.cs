using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗页实体名牌（屏幕空间 uGUI，跟随实体头顶）：玩家名 + 血条填充。
/// 条目由 BattlePage 按实体 id 管理，颜色随阵营（攻红/守蓝）。
/// </summary>
public class EntityNameBarItem : MonoBehaviour
{
    [SerializeField] private Text nameText;   // 玩家名（"玩家 N"，攻红/守蓝）
    [SerializeField] private Image healthFill; // 血条填充（Image Type=Filled，颜色随阵营）

    public Text NameText => nameText;
    public Image HealthFill => healthFill;
}
