using UnityEngine;
using UnityEngine.UI;

public partial class BattlePage : RosPage
{
    [Header("顶部信息条")]
    [SerializeField] private Text timeLabel;         // 对局剩余时间（本地推演 + SCScoreInfo 校准，"MM:SS"）
    [SerializeField] private Text phaseLabel;        // 昼夜状态（"白天 45%" / "晚上 20%"）
    [SerializeField] private Text attackScoreLabel;  // 进攻方分数（"拆塔 X"）
    [SerializeField] private Text defenseScoreLabel; // 防守方分数（"防守 X"）

    [Header("守护点血量面板（右侧：中心守护点固定单槽，外围守护点固定 3 项，下标 = 外围序号）")]
    [SerializeField] private BeaconBarItem centerBeaconInfo; // 中心守护点的信息
    [SerializeField] private RosList beaconInfoList;         // 周围守护点的信息（条目 = BeaconBarItem prefab）
    private RosListWrapper<BeaconBarItem> beaconInfoListWrapper;

    [Header("底部技能栏（槽位数随武器槽变化）")]
    [SerializeField] private RosList skillList;            // 技能槽列表（条目 = BattleSkillItem prefab）
    private RosListWrapper<BattleSkillItem> skillListWrapper;

    [Header("复活进度（仅本地玩家死亡时显示）")]
    [SerializeField] private GameObject revivePanel;       // 复活遮罩整体
    [SerializeField] private Image reviveFill;             // 进度条填充（Image Type=Filled，绿）
    [SerializeField] private Text reviveLabel;             // "复活中 X%（愈战愈勇 ×N）" / "可复活！"

    [Header("结算面板（对局结束显示，关闭后回组队大厅）")]
    [SerializeField] private SettlementPanel settlementPanel; // 结算遮罩整体：胜负标题/比分经验/关闭按钮都在该组件内

    public override void Construct()
    {
        beaconInfoListWrapper = new(beaconInfoList);
        skillListWrapper = new(skillList);
    }
}
