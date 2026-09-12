using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 战斗 HUD（策划案 3.2/10/13/14 章）：
/// 底部技能栏（键盘槽位 U I O L H 直触）/ 右侧守护点血量 / 顶部时间·昼夜·分数 / 复活进度 / 飘字提示。
/// </summary>
public class BattlePage : PageBase
{
    private const string DayName = "白天";
    private const string NightName = "晚上";

    private readonly List<BattleSkillUnit> skillSlots = new();
    private readonly Dictionary<ushort, BeaconBarUnit> beaconBars = new();
    private readonly List<(Label label, float time)> floatingLabels = new();

    private VisualElement skillBar;
    private VisualElement beaconPanel;
    private VisualElement floatingPanel;
    private Label timeLabel;
    private Label phaseLabel;
    private Label attackScoreLabel;
    private Label defenseScoreLabel;
    private VisualElement revivePanel;
    private VisualElement reviveFill;
    private Label reviveLabel;
    private float battleStartTime;
    private bool expSettled; // 对局经验只结算一次（防 SCScoreInfo 重复到达）
    private VisualElement settlePanel;
    private Label settleTitle;
    private Label settleDetail;

    protected override void Build(VisualElement root)
    {
        // 顶部信息条
        var topBar = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 0, height = 42,
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                justifyContent = Justify.Center,
            }
        };
        topBar.style.backgroundColor = UITheme.BarBg;
        topBar.style.borderBottomWidth = 1f;
        topBar.style.borderBottomColor = UITheme.Border;
        root.Add(topBar);

        timeLabel = UITheme.Text("15:00", UITheme.TextMain, 22, true);
        timeLabel.style.marginRight = 28;
        phaseLabel = UITheme.Text(DayName, UITheme.Gold, 18, true);
        phaseLabel.style.marginRight = 28;
        attackScoreLabel = UITheme.Text("拆塔 0", UITheme.Attack, 18, true);
        attackScoreLabel.style.marginRight = 20;
        defenseScoreLabel = UITheme.Text("防守 0", UITheme.Defense, 18, true);
        topBar.Add(timeLabel);
        topBar.Add(phaseLabel);
        topBar.Add(attackScoreLabel);
        topBar.Add(defenseScoreLabel);

        // 右侧守护点血量（常驻显示，防守方 HUD）
        beaconPanel = new VisualElement
        {
            style = { position = Position.Absolute, right = 16, top = 60, width = 240 }
        };
        var beaconTitle = UITheme.Section("守护点");
        beaconTitle.style.marginBottom = 6;
        beaconPanel.Add(beaconTitle);
        root.Add(beaconPanel);

        // 飘字区（中央偏上）
        floatingPanel = new VisualElement
        {
            style = { position = Position.Absolute, left = 0, right = 0, top = 96, alignItems = Align.Center }
        };
        root.Add(floatingPanel);

        // 复活进度（死亡时显示）
        revivePanel = UITheme.Overlay(0.45f);
        var reviveBox = UITheme.Card(320f, 18f);
        reviveLabel = UITheme.Text("复活中...", UITheme.TextMain, 18, true);
        reviveLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        reviveLabel.style.marginBottom = 10;
        var reviveBar = UITheme.BarTrack(12f);
        reviveFill = UITheme.BarFill(UITheme.Green, 12f);
        reviveFill.style.width = Length.Percent(0f);
        reviveBar.Add(reviveFill);
        reviveBox.Add(reviveLabel);
        reviveBox.Add(reviveBar);
        revivePanel.Add(reviveBox);
        root.Add(revivePanel);

        // 底部技能栏
        var bottomBar = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, bottom = 0,
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.Center,
                paddingTop = 12, paddingBottom = 12,
            }
        };
        bottomBar.style.backgroundColor = UITheme.BarBg;
        bottomBar.style.borderTopWidth = 1f;
        bottomBar.style.borderTopColor = UITheme.Border;
        skillBar = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        bottomBar.Add(skillBar);
        root.Add(bottomBar);

        // 初始化守护点槽位（4 个：3 外围 + 1 中心）
        for (int i = 0; i < Config.outer_beacon_count + Config.core_beacon_count; i++)
        {
            var unit = new BeaconBarUnit();
            beaconPanel.Add(unit.Root);
            beaconBars[(ushort)(i + 1)] = unit;
            unit.Root.style.display = DisplayStyle.None;
        }

        // 结算面板（对局结束显示；关闭后回组队大厅，准备开始下一轮）
        settlePanel = UITheme.Overlay(0.75f);
        var settleBox = UITheme.Card(440f, 22f);
        settleTitle = UITheme.Text("对局结束", UITheme.TextMain, 30, true);
        settleTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        settleTitle.style.marginBottom = 14;
        settleDetail = UITheme.Text("", UITheme.TextDim, 17);
        settleDetail.style.marginBottom = 20;
        var settleClose = UITheme.StyleButton(new Button(OnSettleClose) { text = "回到组队大厅" }, true, 0f, 44f);
        settleBox.Add(settleTitle);
        settleBox.Add(settleDetail);
        settleBox.Add(settleClose);
        settlePanel.Add(settleBox);
        root.Add(settlePanel);
    }

    public override void OnEnable()
    {
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.AddEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.AddEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);
        EventManager.AddEvent<int>(ClientEvent.OnDayNightChange, OnDayNightChange);

        // 开局信息立即应用（守护点/技能槽随实体表现摘要到达后刷新）
        battleStartTime = Time.time;
        expSettled = false;
        if (settlePanel != null) settlePanel.style.display = DisplayStyle.None; // 新对局隐藏结算面板
        RefreshDayNightLabel();
    }

    public override void OnDisable()
    {
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.RemoveEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.RemoveEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);
        EventManager.RemoveEvent<int>(ClientEvent.OnDayNightChange, OnDayNightChange);
    }

    public override void OnUpdate()
    {
        // 飘字自动消失
        for (int i = floatingLabels.Count - 1; i >= 0; i--)
        {
            var item = floatingLabels[i];
            if (Time.time - item.time > 2.5f)
            {
                if (item.label.parent != null) item.label.parent.Remove(item.label);
                floatingLabels.RemoveAt(i);
            }
        }
        // 剩余时间（本地估算，精确值以服务器 SCScoreInfo 为准）
        if (NetworkManager.battleInfo != null && timeLabel != null)
        {
            float remain = Config.battle_duration - (Time.time - battleStartTime);
            timeLabel.text = FormatTime(Mathf.Max(0f, remain));
        }
        // 昼夜显示（两态 + 归一化时间百分比）
        RefreshDayNightLabel();
    }

    #region 事件处理
    /// <summary>实体表现摘要：守护点 → 右侧 HUD；本地玩家 → 底部技能栏。</summary>
    private void OnEntityDisplayUpdate(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        if (info.type.category == EntityCategory.Beacon)
        {
            OnBeaconDisplay(info);
        }
        else if (NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            OnLocalSkillBarUpdate(info);
        }
    }

    private void OnLocalSkillBarUpdate(SCEntityDisplayInfo info)
    {
        if (skillBar == null) return;
        // 重建槽位（数量变化时）
        while (skillSlots.Count < info.skills.Count)
        {
            var unit = new BattleSkillUnit();
            skillSlots.Add(unit);
            skillBar.Add(unit.Root);
        }
        while (skillSlots.Count > info.skills.Count)
        {
            var last = skillSlots[skillSlots.Count - 1];
            skillSlots.RemoveAt(skillSlots.Count - 1);
            skillBar.Remove(last.Root);
        }
        for (int i = 0; i < info.skills.Count; i++)
        {
            skillSlots[i].Refresh(info.skills[i], i == info.selectedIndex);
        }
    }

    private void OnBeaconDisplay(SCEntityDisplayInfo info)
    {
        if (!beaconBars.TryGetValue(info.entityId, out var unit))
        {
            unit = new BeaconBarUnit();
            beaconPanel.Add(unit.Root);
            beaconBars[info.entityId] = unit;
        }
        unit.Root.style.display = DisplayStyle.Flex;
        if (info.health <= 0) unit.SetDestroyed();
        else unit.Refresh(info);
    }

    private void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null) return;
        attackScoreLabel.text = $"拆塔 {(int)info.attackScore}";
        defenseScoreLabel.text = $"防守 {(int)info.defenseScore}";
        timeLabel.text = FormatTime(Mathf.Max(0f, info.remainTime));
        if (info.gameState != 0)
        {
            ShowFloating(GetEndText(info.gameState), UITheme.Gold);
            TrySettleExp(info);
            ShowSettlement(info);
        }
    }

    /// <summary>显示结算面板（胜负/比分/经验；玩家关闭后回组队大厅准备下一轮）。</summary>
    private void ShowSettlement(SCScoreInfo info)
    {
        if (settlePanel == null) return;
        settleTitle.text = GetEndText(info.gameState);
        settleTitle.style.color = info.gameState == 1 ? UITheme.Attack
            : info.gameState == 2 ? UITheme.Defense : UITheme.TextMain;
        settleDetail.text = $"进攻方（拆塔）：{(int)info.attackScore}\n" +
                            $"防守方：{(int)info.defenseScore}（击杀 ×{info.killScore}）\n" +
                            $"本局获得经验：{info.expGain}";
        settlePanel.style.display = DisplayStyle.Flex;
    }

    /// <summary>关闭结算面板：清空表现残留，回组队大厅（组队状态保留，点"准备"开启下一轮）。</summary>
    private void OnSettleClose()
    {
        if (settlePanel != null) settlePanel.style.display = DisplayStyle.None;
        Tool.ClientDisplayManager?.ClearAll();
        Owner.ShowPage(UIManager.PageType.Lobby);
    }

    /// <summary>结算局外经验（策划案 17.3：获得经验 = 对水晶造成的伤害量，服务器随 SCScoreInfo 下发）。</summary>
    private void TrySettleExp(SCScoreInfo info)
    {
        if (expSettled || Tool.SaveManager == null || info.expGain <= 0) return;
        expSettled = true;
        Tool.SaveManager.AddPlayerExp(info.expGain);
        var battle = NetworkManager.battleInfo;
        if (battle != null)
        {
            // 全局角色索引：进攻 0~17 / 防守 18~23（SaveManager 双等级制）
            int index = battle.camp == EntityCamp.Attack
                ? battle.characterType.value
                : Config.attack_character_count + battle.characterType.value;
            Tool.SaveManager.AddCharacterExp(index, info.expGain);
        }
    }

    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        switch (e.type)
        {
            case SCBattleEvent.Type.Kill:
                ShowFloating("击杀！", UITheme.Attack);
                break;
            case SCBattleEvent.Type.BeaconDestroyed:
                ShowFloating("守护点被摧毁！", UITheme.Danger);
                break;
            case SCBattleEvent.Type.CrystalCollected:
                ShowFloating("采集水晶，获得收益", UITheme.Green);
                break;
            case SCBattleEvent.Type.PlagueTreeCaptured:
                ShowFloating("攻占瘟疫树！CD 加速", UITheme.Defense);
                break;
            case SCBattleEvent.Type.ShowText:
                Owner.ShowNotice(NoticeMessageMap.Get(e.value));
                break;
        }
    }

    private void OnReviveProgressUpdate(SCReviveInfo info)
    {
        if (info == null) return;
        bool isLocal = NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId;
        if (!isLocal) return;
        revivePanel.style.display = info.ready ? DisplayStyle.None : DisplayStyle.Flex;
        reviveFill.style.width = Length.Percent(Mathf.Clamp01(info.progress) * 100f);
        reviveLabel.text = info.ready
            ? "可复活！"
            : $"复活中 {Mathf.RoundToInt(info.progress * 100f)}%   （愈战愈勇 ×{info.yzStack}）";
    }

    private void OnRightClickBlocked(string msg)
    {
        ShowFloating(string.IsNullOrEmpty(msg) ? "该技能无法在此状态下使用" : msg, UITheme.Warn);
    }

    /// <summary>昼夜状态变化（EnvironmentManager 权威同步/推演触发；1 = 白天，0 = 晚上）。</summary>
    private void OnDayNightChange(int state)
    {
        RefreshDayNightLabel();
    }
    #endregion

    #region//Local
    /// <summary>刷新顶栏昼夜显示：「白天/晚上 + 归一化时间百分比」（t = 1 正午 / 0 午夜）。</summary>
    private void RefreshDayNightLabel()
    {
        if (phaseLabel == null) return;
        phaseLabel.text = $"{(EnvironmentManager.IsDay ? DayName : NightName)} {Mathf.RoundToInt(EnvironmentManager.Time01 * 100f)}%";
    }

    private void ShowFloating(string text, Color color)
    {
        if (floatingPanel == null) return;
        var label = UITheme.Text(text, color, 20, true);
        label.style.marginBottom = 4;
        floatingPanel.Add(label);
        floatingLabels.Add((label, Time.time));
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:D2}:{s % 60:D2}";
    }

    private static string GetEndText(int gameState)
    {
        switch (gameState)
        {
            case 1: return "进攻方胜利";
            case 2: return "防守方胜利";
            default: return "平局";
        }
    }
    #endregion
}
