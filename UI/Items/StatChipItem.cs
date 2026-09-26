using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 属性数据块（RosList 条目，攻/守信息卡共用同一模板）：key 与 value 都由代码填。
/// 进攻/防守两组信息卡各固定 8 项，下标顺序约定见 HomePage.Logic 的 StatKeys。
/// </summary>
public class StatChipItem : MonoBehaviour
{
    [SerializeField] private Text keyText;    // 属性名（"生命"/"力量"/"魔法"/"暴击"/"暴伤"/"击退抗"/"视野"/"技能槽"）
    [SerializeField] private Text valueText;  // 属性数值（如"320"、"5%"、"1.5x"）

    public Text KeyText => keyText;
    public Text ValueText => valueText;
}
