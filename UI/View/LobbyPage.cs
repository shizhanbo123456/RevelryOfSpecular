using UnityEngine;
using UnityEngine.UI;

public partial class LobbyPage : RosPage
{
    [Header("我的队伍（两个互斥选择，回显以服务器 SCRoomInfo 为准）")]
    [SerializeField] private RosToggle attackToggle;   // 加入进攻方
    [SerializeField] private RosToggle defenseToggle;  // 加入防守方

    [Header("AI 玩家数量（任意玩家可编辑双方，数字文本框）")]
    [SerializeField] private InputField attackAIField;  // 进攻方 AI 数量
    [SerializeField] private InputField defenseAIField; // 防守方 AI 数量

    [Header("房间状态")]
    [SerializeField] private Text roomLabel;            // 双方人数（"进攻方：人类 X + AI Y"）与开局条件是否满足

    [Header("底部按钮")]
    [SerializeField] private RosButton backButton;      // 断开并返回：断开连接回初始界面
    [SerializeField] private RosButton startButton;     // 准备：发送开始请求（双阵营人数均 >0 才可点）
}
