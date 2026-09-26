using UnityEngine;
using UnityEngine.UI;

public partial class BattlePage : RosPage
{
    [Header("顶部信息条")]
    [SerializeField] private Text timeLabel;         // 对局剩余时间（本地推演 + SCScoreInfo 校准，"MM:SS"）
    [SerializeField] private Text phaseLabel;        // 昼夜状态（"白天 45%" / "晚上 20%"）
    [SerializeField] private Text attackScoreLabel;  // 进攻方分数（"拆塔 X"）
    [SerializeField] private Text defenseScoreLabel; // 防守方分数（"防守 X"）

    [Header("守护点血量面板（右侧，按 entityId 动态增减条目）")]
    [SerializeField] private RectTransform beaconPanel;    // 条目容器
    [SerializeField] private BeaconBarItem beaconTemplate; // 条目模板（首个条目复用模板本体）

    [Header("底部技能栏（槽位数随武器槽变化）")]
    [SerializeField] private RosList skillList;            // 技能槽列表（条目 = BattleSkillItem prefab）
    private RosListWrapper<BattleSkillItem> skillListWrapper;

    [Header("飘字区（中央偏上，事件提示文字，2.5s 自动消失）")]
    [SerializeField] private RectTransform floatingPanel;  // 飘字容器
    [SerializeField] private Text floatingTextTemplate;    // 飘字文字模板

    [Header("复活进度（仅本地玩家死亡时显示）")]
    [SerializeField] private GameObject revivePanel;       // 复活遮罩整体
    [SerializeField] private Image reviveFill;             // 进度条填充（Image Type=Filled，绿）
    [SerializeField] private Text reviveLabel;             // "复活中 X%（愈战愈勇 ×N）" / "可复活！"

    [Header("结算面板（对局结束显示，关闭后回组队大厅）")]
    [SerializeField] private GameObject settlePanel;       // 结算遮罩整体
    [SerializeField] private Text settleTitle;             // 胜负标题（进攻方胜利/防守方胜利/平局）
    [SerializeField] private Text settleDetail;            // 双方比分 + 击杀数 + 本局经验
    [SerializeField] private RosButton settleCloseButton;  // "回到组队大厅"按钮

    public override void Construct()
    {
        skillListWrapper = new(skillList);
    }
}
