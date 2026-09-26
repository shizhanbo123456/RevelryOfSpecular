using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 首页角色列表的单行条目（进攻/防守两个列表共用同一套 prefab）。
/// 未解锁：lockRoot 激活 + 整行不可点击；选中：selectedMark 激活。
/// </summary>
public class HomeCharacterItem : MonoBehaviour
{
    [SerializeField] private RosButton selectButton;   // 整行点击按钮：点击选中该角色（未解锁时 SetInteractable(false)）
    [SerializeField] private Image icon;               // 角色头像（AssetsManager.Attack/DefenseCharacterIcons，按角色序号取）
    [SerializeField] private Text nameText;            // 角色名（Info 未配置时显示"角色 N"）
    [SerializeField] private Text levelText;           // 角色等级（"Lv N"，取自存档）
    [SerializeField] private GameObject lockRoot;      // 未解锁标记（锁图标+"未解锁"文字整体），未解锁时激活、已解锁隐藏
    [SerializeField] private GameObject selectedMark;  // 选中高亮框，当前选中该角色时激活

    public RosButton SelectButton => selectButton;
    public Image Icon => icon;
    public Text NameText => nameText;
    public Text LevelText => levelText;
    public GameObject LockRoot => lockRoot;
    public GameObject SelectedMark => selectedMark;
}
