using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyPage : RosPage
{
    [Header("我的队伍（两个互斥选择，回显以服务器 SCRoomInfo 为准）")]
    [SerializeField] private RosToggle attackToggle;   // 加入进攻方
    [SerializeField] private RosToggle defenseToggle;  // 加入防守方

    [Header("队伍成员列表（每行 = 玩家名 + 当前所选角色头像，AI 不在此列只计数量）")]
    [SerializeField] private RosList attackMemberList;  // 进攻方成员列表（条目 = LobbyMemberItem prefab）
    [SerializeField] private RosList defenseMemberList; // 防守方成员列表（同上）
    private RosListWrapper<LobbyMemberItem> attackMemberListWrapper;
    private RosListWrapper<LobbyMemberItem> defenseMemberListWrapper;

    [Header("进攻方 AI 数量（-/+ 按钮控制，房间共享，任意玩家可编辑）")]
    [SerializeField] private RosButton attackMinusButton; // 进攻方 AI -1
    [SerializeField] private RosButton attackPlusButton;  // 进攻方 AI +1
    [SerializeField] private Text attackAICountLabel;     // 进攻方当前 AI 数量

    [Header("防守方 AI 数量（-/+ 按钮控制）")]
    [SerializeField] private RosButton defenseMinusButton; // 防守方 AI -1
    [SerializeField] private RosButton defensePlusButton;  // 防守方 AI +1
    [SerializeField] private Text defenseAICountLabel;     // 防守方当前 AI 数量

    [Header("底部按钮")]
    [SerializeField] private RosButton backButton;      // 断开并返回：断开连接回初始界面
    [SerializeField] private RosButton startButton;     // 准备：发送开始请求（双阵营人数均 >0 才可点）

    public override void Construct()
    {
        attackMemberListWrapper = new(attackMemberList);
        defenseMemberListWrapper = new(defenseMemberList);
    }
}
