using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 战斗 HUD（策划案 3.2/10/13/14 章）：
/// 底部技能栏（滚轮循环选中）/ 右侧守护点血量 / 顶部时间·昼夜·分数 / 复活进度 / 飘字提示。
/// </summary>
public class BattlePage : PageBase
{
    private static readonly string[] PhaseNames = { "白天", "黄昏", "夜晚", "黎明" };

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

    protected override void Build(VisualElement root)
    {
        // 顶部信息
        var topBar = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.Center,
                paddingTop = 8,
                backgroundColor = new Color(0f, 0f, 0f, 0.55f),
            }
        };
        timeLabel = new Label("15:00") { style = { color = Color.white, fontSize = 22, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 24 } };
        phaseLabel = new Label("白天") { style = { color = new Color(1f, 0.9f, 0.4f, 1f), fontSize = 18, marginRight = 24 } };
        attackScoreLabel = new Label("拆塔 0") { style = { color = new Color(1f, 0.5f, 0.4f, 1f), fontSize = 18, marginRight = 16 } };
        defenseScoreLabel = new Label("防守 0") { style = { color = new Color(0.4f, 0.8f, 1f, 1f), fontSize = 18 } };
        topBar.Add(timeLabel);
        topBar.Add(phaseLabel);
        topBar.Add(attackScoreLabel);
        topBar.Add(defenseScoreLabel);
        root.Add(topBar);

        // 右侧守护点血量（常驻显示，防守方 HUD）
        beaconPanel = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                right = 12, top = 60,
                width = 260,
            }
        };
        root.Add(beaconPanel);

        // 飘字区（中央偏上）
        floatingPanel = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 90,
                alignItems = Align.Center,
            }
        };
        root.Add(floatingPanel);

        // 复活进度（死亡时显示）
        revivePanel = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 0, bottom = 0,
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                display = DisplayStyle.None,
            }
        };
        var reviveBox = new VisualElement
        {
            style = { width = 300, backgroundColor = new Color(0f, 0f, 0f, 0.75f), padding = 16 }
        };
        reviveLabel = new Label("复活中...") { style = { color = Color.white, fontSize = 18, textAlignment = TextAnchor.MiddleCenter, marginBottom = 8 } };
        var reviveBar = new VisualElement { style = { height = 14, backgroundColor = new Color(0.25f, 0.25f, 0.3f, 1f) } };
        reviveFill = new VisualElement { style = { width = Length.Percent(0f), height = 14, backgroundColor = new Color(0.3f, 0.9f, 0.4f, 1f) } };
        reviveFill.pickingMode = PickingMode.Ignore;
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
                paddingBottom = 10, paddingTop = 10,
                backgroundColor = new Color(0f, 0f, 0f, 0.5f),
            }
        };
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
    }

    public override void OnEnable()
    {
        EventManager.AddEvent(ClientEvent.OnSkillRuntimeUpdate, OnSkillRuntimeUpdate);
        EventManager.AddEvent(ClientEvent.OnBeaconHealthUpdate, OnBeaconHealthUpdate);
        EventManager.AddEvent(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.AddEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);

        // 开局信息立即应用
        battleStartTime = Time.time;
        if (NetworkManager.battleInfo != null)
        {
            OnSkillRuntimeUpdate(new SCSkillRuntimeInfo() { selectedIndex = -1 });
            foreach (var beacon in NetworkManager.battleInfo.beacons)
            {
                OnBeaconHealthUpdate(beacon);
            }
            phaseLabel.text = PhaseNames[Mathf.Clamp(NetworkManager.battleInfo.dayNightPhase, 0, PhaseNames.Length - 1)];
        }
    }

    public override void OnDisable()
    {
        EventManager.RemoveEvent(ClientEvent.OnSkillRuntimeUpdate, OnSkillRuntimeUpdate);
        EventManager.RemoveEvent(ClientEvent.OnBeaconHealthUpdate, OnBeaconHealthUpdate);
        EventManager.RemoveEvent(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.RemoveEvent(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.RemoveEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);
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
    }

    #region 事件处理
    private void OnSkillRuntimeUpdate(SCSkillRuntimeInfo info)
    {
        if (info == null || skillBar == null) return;
        // 重建槽位（数量变化时）
        while (skillSlots.Count < info.slots.Count)
        {
            var unit = new BattleSkillUnit();
            skillSlots.Add(unit);
            skillBar.Add(unit.Root);
        }
        while (skillSlots.Count > info.slots.Count)
        {
            var last = skillSlots[skillSlots.Count - 1];
            skillSlots.RemoveAt(skillSlots.Count - 1);
            skillBar.Remove(last.Root);
        }
        for (int i = 0; i < info.slots.Count; i++)
        {
            skillSlots[i].Refresh(info.slots[i], i == info.selectedIndex);
        }
    }

    private void OnBeaconHealthUpdate(SCBeaconInfo info)
    {
        if (info == null) return;
        if (!beaconBars.TryGetValue(info.entityId, out var unit))
        {
            unit = new BeaconBarUnit();
            beaconPanel.Add(unit.Root);
            beaconBars[info.entityId] = unit;
        }
        unit.Root.style.display = DisplayStyle.Flex;
        if (info.destroyed) unit.SetDestroyed();
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
            ShowFloating(GetEndText(info.gameState), new Color(1f, 0.9f, 0.3f, 1f));
        }
    }

    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        switch (e.type)
        {
            case SCBattleEvent.Type.DayNight:
                phaseLabel.text = PhaseNames[Mathf.Clamp(e.value, 0, PhaseNames.Length - 1)];
                break;
            case SCBattleEvent.Type.Kill:
                ShowFloating("击杀！", new Color(1f, 0.5f, 0.3f, 1f));
                break;
            case SCBattleEvent.Type.BeaconDestroyed:
                ShowFloating("守护点被摧毁！", new Color(1f, 0.3f, 0.2f, 1f));
                break;
            case SCBattleEvent.Type.CrystalCollected:
                ShowFloating("采集水晶，获得收益", new Color(0.5f, 0.9f, 0.6f, 1f));
                break;
            case SCBattleEvent.Type.PlagueTreeCaptured:
                ShowFloating("攻占瘟疫树！CD 加速", new Color(0.6f, 0.8f, 1f, 1f));
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
            : $"复活中 {Mathf.RoundToInt(info.progress * 100f)}%  （愈战愈勇 ×{info.yzStack}）";
    }

    private void OnRightClickBlocked(string msg)
    {
        ShowFloating(string.IsNullOrEmpty(msg) ? Config.right_click_blocked_notice : msg, new Color(1f, 0.6f, 0.2f, 1f));
    }
    #endregion

    #region//Local
    private void ShowFloating(string text, Color color)
    {
        if (floatingPanel == null) return;
        var label = new Label(text) { style = { color = color, fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
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
