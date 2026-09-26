using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 属性数据块：key 文字（生命/力量/魔法等）预置在 prefab 上不改，valueText 由代码按角色属性填。
/// 进攻/防守两组信息卡各自挂 8 个，下标顺序约定见 HomePage.View。
/// </summary>
public class StatChipItem : MonoBehaviour
{
    [SerializeField] private Text valueText;  // 属性数值（如"320"、"5%"、"1.5x"）

    public Text ValueText => valueText;
}
