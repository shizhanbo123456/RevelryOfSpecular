using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class HomePage : RosPage
{
    [Header("顶栏（收起/展开两态均常驻）")]
    [SerializeField] private Text playerLevelLabel;          // 玩家等级（SaveManager.playerLevel，"玩家等级 Lv N"）
    [SerializeField] private RosButton connectButton;        // 连接服务器：连接成功后自动进组队大厅
    [SerializeField] private Text connectStatusLabel;        // 连接状态提示（正在连接.../连接失败/已连接）
    [SerializeField] private InputField IpAddressInputField; // 服务器 IP 输入框（留空 = 连本机）

    [Header("角色列表（进攻/防守两列并排，各显示本阵营全部角色，未解锁行灰显+锁标记）")]
    [SerializeField] private RosList attackerInfoList;       // 进攻方角色列表（条目 = HomeCharacterItem prefab）
    [SerializeField] private RosList defenserInfoList;       // 防守方角色列表（同上）
    private RosListWrapper<HomeCharacterItem> attackerInfoListWrapper;
    private RosListWrapper<HomeCharacterItem> defenserInfoListWrapper;

    [Header("选中信息卡 · 进攻方（显示进攻方当前选中的角色）")]
    [SerializeField] private Text attackSelectedNameText;    // 选中角色名 + 等级（"名字 Lv N"）
    [SerializeField] private List<StatChipItem> attackStatChips; // 属性数据块，下标固定顺序：0生命 1力量 2魔法 3暴击 4暴伤 5击退抗 6视野 7技能槽
    [SerializeField] private Text attackLevelUpHintText;     // 升级增益：选中角色下一级的属性提升描述

    [Header("选中信息卡 · 防守方（显示防守方当前选中的角色）")]
    [SerializeField] private Text defenseSelectedNameText;   // 选中角色名 + 等级（同上）
    [SerializeField] private List<StatChipItem> defenseStatChips; // 属性数据块，下标顺序同上
    [SerializeField] private Text defenseLevelUpHintText;    // 升级增益：防守方选中角色下一级的属性提升描述

    public override void Construct()
    {
        attackerInfoListWrapper = new(attackerInfoList);
        defenserInfoListWrapper = new(defenserInfoList);
    }
}
