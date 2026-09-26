using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 组队大厅队伍成员条目：显示玩家名 + 当前所选角色的头像（人形成员；AI 只以数量展示，不在此列）。
/// </summary>
public class LobbyMemberItem : MonoBehaviour
{
    [SerializeField] private Text nameText;  // 玩家名（"玩家 N"，与头顶名字同一规则）
    [SerializeField] private Image icon;     // 所选角色头像（AssetsManager.Attack/DefenseCharacterIcons 按角色下标取；未选/未导入时隐藏）

    public Text NameText => nameText;
    public Image Icon => icon;
}
